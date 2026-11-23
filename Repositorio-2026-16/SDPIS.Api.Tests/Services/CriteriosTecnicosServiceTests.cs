using SDPIS.Api.Models.DTOs;
using SDPIS.Api.Repositories;
using SDPIS.Api.Services;
using SDPIS.Api.Services.Autorizacion;
using SDPIS.Api.Services.GestionUsuarios;
using SDPIS.Api.Services.Validation;
using Xunit;

namespace SDPIS.Api.Tests.Services;

public class CriteriosTecnicosServiceTests
{
    // HU-010/011 + HU-015: el servicio toma al usuario del token y su rol de
    // IPermisosService; el rol que venga en el request se ignora. Por defecto
    // el autenticado es el funcionario 100 (validador, asignado a la denuncia
    // 9001 segun FakeVisibilidadDenunciasService). Las pruebas que modifican
    // criterios ya registrados piden Roles.Superusuario.
    private static CriteriosTecnicosService CrearServicio(
        FakeCriteriosTecnicosRepository repository,
        FakeEvaluacionCriticidadService evaluacionCriticidadService,
        string rol = Roles.Validador)
    {
        var info = new FuncionarioPermisosInfo(100, rol, true, 5, false, []);
        return new CriteriosTecnicosService(
            repository,
            evaluacionCriticidadService,
            new FakeUsuarioActual(100),
            new FakePermisosService(info),
            new FakeVisibilidadDenunciasService());
    }

    private const string SlugDanoPotencialSalud = "dano-potencial-salud";
    private const string SlugCriterioEspecializadoUvc = "criterio-especializado-uvc";

    private static RegistrarCriteriosTecnicosRequest RequestRegistrar(string vinculadoComercioElectronico = "S") => new(
        FuncionarioValidadorId: 100,
        VinculadoComercioElectronico: vinculadoComercioElectronico,
        DanoPotencialSalud: "Alto",
        RequiereCriterioEspecializado: "Si",
        ObservacionesTecnicas: null
    );

    // Justificacion con un texto valido por defecto: la mayoria de estas
    // pruebas simula una edicion real (el fake repository devuelve un DTO
    // completo por defecto -- ver FakeCriteriosTecnicosRepository), asi que
    // sin esto fallarian por la nueva regla de justificacion obligatoria.
    private static ActualizarCriterioTecnicoRequest RequestActualizarCriterio(
        string rol, string? justificacion = "Se corrige tras revisar nuevamente el expediente.") => new(
        RolFuncionarioSimulado: rol,
        FuncionarioId: 1,
        ValorSeleccionado: "Medio",
        Justificacion: justificacion
    );

    private static ActualizarMetadataCriteriosTecnicosRequest RequestActualizarMetadata(
        string rol, string vinculadoComercioElectronico = "N", string? justificacion = null) => new(
        RolFuncionarioSimulado: rol,
        FuncionarioId: 1,
        VinculadoComercioElectronico: vinculadoComercioElectronico,
        ObservacionesTecnicas: null,
        Justificacion: justificacion
    );

    // DTO "completo" (ambos criterios respondidos) para simular el estado
    // que devuelve ObtenerAsync despues de editar un criterio.
    private static CriteriosTecnicosDto DtoCompleto(string? nivelCriticidad = null) => new(
        IdEvaluacion: 1,
        DenunciaId: 9001,
        FuncionarioValidadorId: 100,
        FechaEvaluacion: DateTime.Now,
        PuntajeTotal: 6,
        VinculadoComercioElectronico: "N",
        DanoPotencialSalud: "Alto",
        RequiereCriterioEspecializado: "Si",
        ObservacionesTecnicas: null,
        NivelCriticidad: nivelCriticidad,
        FechaLimite: null
    );

    // DTO "incompleto" (falta el criterio especializado), como queda tras
    // eliminar ese criterio (evaluaciones anteriores a retirar esa opcion).
    private static CriteriosTecnicosDto DtoIncompleto() => DtoCompleto() with { RequiereCriterioEspecializado = null };

    [Fact]
    public async Task RegistrarAsync_ConDatosValidos_LlamaAlRepositorioYRetornaResultado()
    {
        var repository = new FakeCriteriosTecnicosRepository { ResultadoARetornar = new(5, 6) };
        var service = CrearServicio(repository, new FakeEvaluacionCriticidadService());

        var respuesta = await service.RegistrarAsync(9001, RequestRegistrar(), CancellationToken.None);

        Assert.True(repository.SeLlamoRegistrar);
        Assert.Equal(9001, repository.UltimoDenunciaId);
        Assert.Equal(5, respuesta.IdEvaluacion);
        Assert.Equal(6, respuesta.PuntajeTotal);
    }

    [Fact]
    public async Task RegistrarAsync_ConDatosInvalidos_NoLlamaAlRepositorio()
    {
        var repository = new FakeCriteriosTecnicosRepository();
        var evaluacionCriticidadService = new FakeEvaluacionCriticidadService();
        var service = CrearServicio(repository, evaluacionCriticidadService);
        var request = RequestRegistrar() with { DanoPotencialSalud = "" };

        await Assert.ThrowsAsync<DenunciaValidationException>(
            () => service.RegistrarAsync(9001, request, CancellationToken.None));

        Assert.False(repository.SeLlamoRegistrar);
        Assert.False(evaluacionCriticidadService.SeLlamoEvaluar);
    }

    // HU-011 (#62), AC #1/#2/#6/#7: registrar criterios tecnicos dispara
    // automaticamente la evaluacion de criticidad (nivel anterior null: es
    // una evaluacion nueva) y el chequeo de comercio electronico (AC #5).
    [Fact]
    public async Task RegistrarAsync_ConDatosValidos_DisparaEvaluacionDeCriticidadAutomaticamente()
    {
        var repository = new FakeCriteriosTecnicosRepository { ResultadoARetornar = new(5, 6) };
        var evaluacionCriticidadService = new FakeEvaluacionCriticidadService();
        var service = CrearServicio(repository, evaluacionCriticidadService);

        await service.RegistrarAsync(9001, RequestRegistrar("N"), CancellationToken.None);

        Assert.True(evaluacionCriticidadService.SeLlamoEvaluar);
        Assert.Equal(9001, evaluacionCriticidadService.UltimoDenunciaId);
        Assert.Equal(6, evaluacionCriticidadService.UltimoPuntajeTotal);
        Assert.Null(evaluacionCriticidadService.UltimoNivelAnterior);

        Assert.True(evaluacionCriticidadService.SeLlamoEvaluarComercioElectronico);
        Assert.Equal("N", evaluacionCriticidadService.UltimoVinculadoComercioElectronico);
        Assert.Null(evaluacionCriticidadService.UltimoVinculadoComercioElectronicoAnterior);
    }

    // HU-011 (#62): el nivel calculado se incluye en la respuesta, como
    // insumo para priorizacion/asignacion (AC #3).
    [Fact]
    public async Task RegistrarAsync_IncluyeElNivelCriticidadCalculadoEnLaRespuesta()
    {
        var repository = new FakeCriteriosTecnicosRepository();
        var evaluacionCriticidadService = new FakeEvaluacionCriticidadService
        {
            ResultadoARetornar = new("ALTO", 0)
        };
        var service = CrearServicio(repository, evaluacionCriticidadService);

        var respuesta = await service.RegistrarAsync(9001, RequestRegistrar(), CancellationToken.None);

        Assert.Equal("ALTO", respuesta.NivelCriticidad);
    }

    // Criterio de aceptacion #4 + correccion de alcance: solo el superusuario
    // puede modificar un criterio individual, y no debe tocar el repositorio
    // ni la evaluacion de criticidad si no lo es.
    [Fact]
    public async Task ActualizarCriterioAsync_SinRolSuperusuario_LanzaExcepcionYNoLlamaAlRepositorio()
    {
        var repository = new FakeCriteriosTecnicosRepository();
        var evaluacionCriticidadService = new FakeEvaluacionCriticidadService();
        var service = CrearServicio(repository, evaluacionCriticidadService);

        await Assert.ThrowsAsync<PermisoDenegadoException>(
            () => service.ActualizarCriterioAsync(
                9001, SlugDanoPotencialSalud, RequestActualizarCriterio("validador"), CancellationToken.None));

        Assert.False(repository.SeLlamoActualizarCriterio);
        Assert.False(evaluacionCriticidadService.SeLlamoEvaluar);
    }

    [Fact]
    public async Task ActualizarCriterioAsync_ConRolSuperusuario_LlamaAlRepositorioConLaClaveCorrecta()
    {
        var repository = new FakeCriteriosTecnicosRepository { ResultadoARetornar = new(5, 4) };
        var service = CrearServicio(repository, new FakeEvaluacionCriticidadService(), Roles.Superusuario);

        var respuesta = await service.ActualizarCriterioAsync(
            9001, SlugDanoPotencialSalud, RequestActualizarCriterio("superusuario"), CancellationToken.None);

        Assert.True(repository.SeLlamoActualizarCriterio);
        Assert.Equal("DANO_POTENCIAL_SALUD", repository.UltimaClaveCriterio);
        Assert.Equal(4, respuesta.PuntajeTotal);
        Assert.Equal(SlugDanoPotencialSalud, respuesta.Criterio);
    }

    [Fact]
    public async Task ActualizarCriterioAsync_MapeaElOtroCriterioALaClaveCorrecta()
    {
        var repository = new FakeCriteriosTecnicosRepository();
        var service = CrearServicio(repository, new FakeEvaluacionCriticidadService(), Roles.Superusuario);

        await service.ActualizarCriterioAsync(
            9001, SlugCriterioEspecializadoUvc, RequestActualizarCriterio("superusuario"), CancellationToken.None);

        Assert.Equal("CRITERIO_ESPECIALIZADO_UVC", repository.UltimaClaveCriterio);
    }

    // HU-011 (#62): tras actualizar un criterio, si la evaluacion sigue
    // completa se recalcula el nivel (con el nivel anterior leido de
    // ObtenerAsync, para que EvaluacionCriticidadService pueda decidir si
    // notifica a UVC solo en la transicion).
    [Fact]
    public async Task ActualizarCriterioAsync_ConEvaluacionCompleta_RecalculaCriticidad()
    {
        var repository = new FakeCriteriosTecnicosRepository
        {
            ResultadoARetornar = new(5, 4),
            DtoARetornar = DtoCompleto(nivelCriticidad: "MEDIO")
        };
        var evaluacionCriticidadService = new FakeEvaluacionCriticidadService();
        var service = CrearServicio(repository, evaluacionCriticidadService, Roles.Superusuario);

        await service.ActualizarCriterioAsync(
            9001, SlugDanoPotencialSalud, RequestActualizarCriterio("superusuario"), CancellationToken.None);

        Assert.True(evaluacionCriticidadService.SeLlamoEvaluar);
        Assert.Equal(4, evaluacionCriticidadService.UltimoPuntajeTotal);
        Assert.Equal("MEDIO", evaluacionCriticidadService.UltimoNivelAnterior);
        Assert.False(evaluacionCriticidadService.SeLlamoLimpiar);
    }

    // HU-011 (#62): si tras actualizar un criterio la evaluacion queda
    // incompleta (raro para actualizar, pero posible si el otro criterio ya
    // se habia eliminado antes), se limpia el nivel en vez de calcularlo.
    [Fact]
    public async Task ActualizarCriterioAsync_ConEvaluacionIncompleta_LimpiaCriticidad()
    {
        var repository = new FakeCriteriosTecnicosRepository
        {
            ResultadoARetornar = new(5, 3),
            DtoARetornar = DtoIncompleto()
        };
        var evaluacionCriticidadService = new FakeEvaluacionCriticidadService();
        var service = CrearServicio(repository, evaluacionCriticidadService, Roles.Superusuario);

        await service.ActualizarCriterioAsync(
            9001, SlugDanoPotencialSalud, RequestActualizarCriterio("superusuario"), CancellationToken.None);

        Assert.True(evaluacionCriticidadService.SeLlamoLimpiar);
        Assert.False(evaluacionCriticidadService.SeLlamoEvaluar);
    }

    // Reinicio de plazo (acordado con el PO): solo una modificacion real de
    // un criterio reinicia el plazo. Guardar el mismo valor que ya tenia no
    // debe tocar la BD, recalcular la criticidad ni dejar rastro en bitacora.
    [Fact]
    public async Task ActualizarCriterioAsync_ConElMismoValor_NoModificaNiRecalculaCriticidad()
    {
        var repository = new FakeCriteriosTecnicosRepository
        {
            DtoARetornar = DtoCompleto(nivelCriticidad: "ALTO") with { DanoPotencialSalud = "Medio", PuntajeTotal = 4 }
        };
        var evaluacionCriticidadService = new FakeEvaluacionCriticidadService();
        var service = CrearServicio(repository, evaluacionCriticidadService, Roles.Superusuario);

        var respuesta = await service.ActualizarCriterioAsync(
            9001, SlugDanoPotencialSalud, RequestActualizarCriterio("superusuario"), CancellationToken.None);

        Assert.False(repository.SeLlamoActualizarCriterio);
        Assert.False(evaluacionCriticidadService.SeLlamoEvaluar);
        Assert.False(evaluacionCriticidadService.SeLlamoLimpiar);
        Assert.Equal(4, respuesta.PuntajeTotal);
        Assert.Equal("Medio", respuesta.ValorSeleccionado);
    }

    [Fact]
    public async Task ActualizarCriterioAsync_ConValorDistinto_ModificaYRecalculaCriticidad()
    {
        var repository = new FakeCriteriosTecnicosRepository
        {
            ResultadoARetornar = new(1, 4),
            DtoARetornar = DtoCompleto(nivelCriticidad: "ALTO") with { DanoPotencialSalud = "Alto" }
        };
        var evaluacionCriticidadService = new FakeEvaluacionCriticidadService();
        var service = CrearServicio(repository, evaluacionCriticidadService, Roles.Superusuario);

        await service.ActualizarCriterioAsync(
            9001, SlugDanoPotencialSalud, RequestActualizarCriterio("superusuario"), CancellationToken.None);

        Assert.True(repository.SeLlamoActualizarCriterio);
        Assert.True(evaluacionCriticidadService.SeLlamoEvaluar);
    }

    // Panel de justificacion obligatorio: editar un criterio que ya tenia
    // valor ("Alto" -> "Medio", DtoARetornar por defecto) sin justificacion
    // debe rechazarse antes de tocar el repositorio.
    [Fact]
    public async Task ActualizarCriterioAsync_EditandoValorExistenteSinJustificacion_LanzaExcepcionYNoLlamaAlRepositorio()
    {
        var repository = new FakeCriteriosTecnicosRepository();
        var evaluacionCriticidadService = new FakeEvaluacionCriticidadService();
        var service = CrearServicio(repository, evaluacionCriticidadService, Roles.Superusuario);

        await Assert.ThrowsAsync<DenunciaValidationException>(
            () => service.ActualizarCriterioAsync(
                9001, SlugDanoPotencialSalud, RequestActualizarCriterio("superusuario", justificacion: null), CancellationToken.None));

        Assert.False(repository.SeLlamoActualizarCriterio);
        Assert.False(evaluacionCriticidadService.SeLlamoEvaluar);
    }

    // Misma regla: una justificacion demasiado corta tampoco es valida.
    [Fact]
    public async Task ActualizarCriterioAsync_EditandoValorExistenteConJustificacionMuyCorta_LanzaExcepcion()
    {
        var repository = new FakeCriteriosTecnicosRepository();
        var service = CrearServicio(repository, new FakeEvaluacionCriticidadService(), Roles.Superusuario);

        await Assert.ThrowsAsync<DenunciaValidationException>(
            () => service.ActualizarCriterioAsync(
                9001, SlugDanoPotencialSalud, RequestActualizarCriterio("superusuario", justificacion: "ok"), CancellationToken.None));

        Assert.False(repository.SeLlamoActualizarCriterio);
    }

    // Boton "Agregar" (el criterio no tenia valor previo): no es una
    // edicion, asi que la justificacion no aplica y puede omitirse.
    [Fact]
    public async Task ActualizarCriterioAsync_AgregandoCriterioSinValorPrevio_NoExigeJustificacion()
    {
        var repository = new FakeCriteriosTecnicosRepository
        {
            ResultadoARetornar = new(1, 4),
            DtoARetornar = DtoCompleto() with { DanoPotencialSalud = null }
        };
        var service = CrearServicio(repository, new FakeEvaluacionCriticidadService(), Roles.Superusuario);

        var respuesta = await service.ActualizarCriterioAsync(
            9001, SlugDanoPotencialSalud, RequestActualizarCriterio("superusuario", justificacion: null), CancellationToken.None);

        Assert.True(repository.SeLlamoActualizarCriterio);
        Assert.Equal(4, respuesta.PuntajeTotal);
    }

    [Fact]
    public async Task ActualizarMetadataAsync_SinRolSuperusuario_LanzaExcepcionYNoLlamaAlRepositorio()
    {
        var repository = new FakeCriteriosTecnicosRepository();
        var evaluacionCriticidadService = new FakeEvaluacionCriticidadService();
        var service = CrearServicio(repository, evaluacionCriticidadService);

        await Assert.ThrowsAsync<PermisoDenegadoException>(
            () => service.ActualizarMetadataAsync(9001, RequestActualizarMetadata("validador"), CancellationToken.None));

        Assert.False(repository.SeLlamoActualizarMetadata);
        Assert.False(evaluacionCriticidadService.SeLlamoEvaluarComercioElectronico);
    }

    [Fact]
    public async Task ActualizarMetadataAsync_ConRolSuperusuario_LlamaAlRepositorio()
    {
        var repository = new FakeCriteriosTecnicosRepository { IdEvaluacionMetadataARetornar = 7 };
        var service = CrearServicio(repository, new FakeEvaluacionCriticidadService(), Roles.Superusuario);

        var respuesta = await service.ActualizarMetadataAsync(
            9001,
            RequestActualizarMetadata("superusuario", "S", "Se corrige tras revisar nuevamente el expediente."),
            CancellationToken.None);

        Assert.True(repository.SeLlamoActualizarMetadata);
        Assert.Equal(7, respuesta.IdEvaluacion);
    }

    // HU-011 (#62), AC #5: ActualizarMetadataAsync es el unico lugar donde
    // el comercio electronico cambia despues del registro -- se lee el
    // valor anterior ANTES de actualizar (el SP lo sobreescribe) y se pasa
    // junto con el nuevo a EvaluarComercioElectronicoAsync, que decide si
    // dispara el stub de reasignacion (esa decision se prueba en
    // EvaluacionCriticidadServiceTests, no aqui).
    [Fact]
    public async Task ActualizarMetadataAsync_InformaElComercioElectronicoAnteriorYNuevo()
    {
        var repository = new FakeCriteriosTecnicosRepository
        {
            DtoARetornar = DtoCompleto() with { VinculadoComercioElectronico = "N" }
        };
        var evaluacionCriticidadService = new FakeEvaluacionCriticidadService();
        var service = CrearServicio(repository, evaluacionCriticidadService, Roles.Superusuario);

        await service.ActualizarMetadataAsync(
            9001,
            RequestActualizarMetadata("superusuario", "S", "Se corrige tras revisar nuevamente el expediente."),
            CancellationToken.None);

        Assert.True(evaluacionCriticidadService.SeLlamoEvaluarComercioElectronico);
        Assert.Equal("S", evaluacionCriticidadService.UltimoVinculadoComercioElectronico);
        Assert.Equal("N", evaluacionCriticidadService.UltimoVinculadoComercioElectronicoAnterior);
    }
}