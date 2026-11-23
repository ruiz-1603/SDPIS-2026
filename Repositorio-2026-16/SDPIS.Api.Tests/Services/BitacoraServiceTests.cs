using SDPIS.Api.Models.DTOs;
using SDPIS.Api.Repositories;
using SDPIS.Api.Services;
using SDPIS.Api.Services.Validation;
using Xunit;

namespace SDPIS.Api.Tests.Services;

public class BitacoraServiceTests
{
    // HU-015: BitacoraService ahora calcula el alcance con IPermisosService.
    // El fake de permisos se arma a partir del funcionario que cada prueba ya
    // define en repository.FuncionarioARetornar (rol, area y estado activo).
    private static BitacoraService CrearServicio(FakeBitacoraRepository repository)
        => new(repository, FakePermisosService.Desde(repository.FuncionarioARetornar));

    // AC #1, #2, #5: cualquier accion sobre la denuncia (asignacion, cambio
    // de estado, etc.) debe quedar en la bitacora sin intervencion manual.
    // FuncionarioId null representa una accion automatica del sistema (sin
    // que nadie la haya disparado a mano) y aun asi debe registrarse.
    [Fact]
    public async Task RegistrarAsync_ConFuncionarioNull_RegistraComoAccionAutomaticaDelSistema()
    {
        var repository = new FakeBitacoraRepository();
        var service = CrearServicio(repository);

        await service.RegistrarAsync(9001, funcionarioId: null, areaOrigenId: 5,
            "asignacion", "Denuncia asignada automaticamente", CancellationToken.None);

        Assert.Single(repository.RegistrosInsertados);
        var registro = repository.RegistrosInsertados[0];
        Assert.Null(registro.FuncionarioId);
        Assert.Equal(9001, registro.DenunciaId);
        Assert.Equal("asignacion", registro.TipoRegistro);
    }

    // AC #4: cada registro debe permitir identificar tipo, usuario
    // responsable, area de origen y observaciones (contenido).
    [Fact]
    public async Task RegistrarAsync_ConDatosValidos_GuardaTodosLosCamposCorrectamente()
    {
        var repository = new FakeBitacoraRepository();
        var service = CrearServicio(repository);

        await service.RegistrarAsync(9001, funcionarioId: 42, areaOrigenId: 7,
            "asignacion", "Validador asignado a la denuncia", CancellationToken.None);

        var registro = Assert.Single(repository.RegistrosInsertados);
        Assert.Equal(9001, registro.DenunciaId);
        Assert.Equal(42, registro.FuncionarioId);
        Assert.Equal(7, registro.AreaOrigenId);
        Assert.Equal("asignacion", registro.TipoRegistro);
        Assert.Equal("Validador asignado a la denuncia", registro.Contenido);
    }

    [Fact]
    public async Task RegistrarAsync_RecortaEspaciosEnElContenido()
    {
        var repository = new FakeBitacoraRepository();
        var service = CrearServicio(repository);

        await service.RegistrarAsync(9001, funcionarioId: 42, areaOrigenId: null,
            "asignacion", "  Contenido con espacios  ", CancellationToken.None);

        Assert.Equal("Contenido con espacios", repository.RegistrosInsertados[0].Contenido);
    }

    [Fact]
    public async Task RegistrarAsync_ConTipoRegistroInvalido_LanzaExcepcionYNoRegistraNada()
    {
        var repository = new FakeBitacoraRepository();
        var service = CrearServicio(repository);

        await Assert.ThrowsAsync<DenunciaValidationException>(() =>
            service.RegistrarAsync(9001, 42, 7, "tipo_inexistente", "Contenido valido", CancellationToken.None));

        Assert.Empty(repository.RegistrosInsertados);
    }

    // Los tipos de la prueba inicial ya no existen: todo se agrupa por tipo de accion.
    [Theory]
    [InlineData("accion_obligatoria")]
    [InlineData("accion_opcional")]
    [InlineData("consulta")]
    public async Task RegistrarAsync_ConTipoAnterior_LanzaExcepcion(string tipo)
    {
        var repository = new FakeBitacoraRepository();
        var service = CrearServicio(repository);

        await Assert.ThrowsAsync<DenunciaValidationException>(() =>
            service.RegistrarAsync(9001, 42, 7, tipo, "Contenido valido", CancellationToken.None));

        Assert.Empty(repository.RegistrosInsertados);
    }

    [Fact]
    public async Task RegistrarAsync_AceptaTodosLosTiposDeBitacora()
    {
        var repository = new FakeBitacoraRepository();
        var service = CrearServicio(repository);

        foreach (var tipo in TiposBitacora.Todos)
            await service.RegistrarAsync(9001, null, null, tipo, "Contenido valido", CancellationToken.None);

        Assert.Equal(TiposBitacora.Todos.Count, repository.RegistrosInsertados.Count);
    }

    [Fact]
    public async Task RegistrarAsync_ConContenidoVacio_LanzaExcepcionYNoRegistraNada()
    {
        var repository = new FakeBitacoraRepository();
        var service = CrearServicio(repository);

        await Assert.ThrowsAsync<DenunciaValidationException>(() =>
            service.RegistrarAsync(9001, 42, 7, "asignacion", "   ", CancellationToken.None));

        Assert.Empty(repository.RegistrosInsertados);
    }

    // AC #6: debe permitirse agregar comentarios manuales adicionales. Se
    // guardan siempre como "comentario" (TiposBitacora.Comentario).
    [Fact]
    public async Task AgregarComentarioAsync_ConDatosValidos_GuardaComoComentario()
    {
        var repository = new FakeBitacoraRepository
        {
            FuncionarioARetornar = new FuncionarioAccesoInfo(1, "jefe_area", AreaId: 5, Activo: true),
            DenunciaARetornar = new DenunciaAccesoInfo(9001, AreaActualId: 5, FuncionarioAsignadoId: null),
        };
        var service = CrearServicio(repository);
        var request = new AgregarComentarioBitacoraRequest(FuncionarioId: 1, Contenido: "Seguimiento telefonico");

        await service.AgregarComentarioAsync(9001, request, CancellationToken.None);

        var registro = Assert.Single(repository.RegistrosInsertados);
        Assert.Equal("comentario", registro.TipoRegistro);
        Assert.Equal(1, registro.FuncionarioId);
        Assert.Equal(5, registro.AreaOrigenId);
        Assert.Equal("Seguimiento telefonico", registro.Contenido);
    }

    [Fact]
    public async Task AgregarComentarioAsync_ConContenidoVacio_LanzaExcepcionYNoRegistraNada()
    {
        var repository = new FakeBitacoraRepository
        {
            FuncionarioARetornar = new FuncionarioAccesoInfo(1, "jefe_area", AreaId: 5, Activo: true),
            DenunciaARetornar = new DenunciaAccesoInfo(9001, AreaActualId: 5, FuncionarioAsignadoId: null),
        };
        var service = CrearServicio(repository);
        var request = new AgregarComentarioBitacoraRequest(FuncionarioId: 1, Contenido: "   ");

        await Assert.ThrowsAsync<DenunciaValidationException>(() =>
            service.AgregarComentarioAsync(9001, request, CancellationToken.None));

        Assert.Empty(repository.RegistrosInsertados);
    }

    // AC #10: solo los usuarios autorizados pueden consultar/comentar la
    // bitacora de una denuncia (jefe_area: solo su area; validador: solo lo
    // asignado a el). Un jefe de area distinta no debe poder comentar.
    [Fact]
    public async Task AgregarComentarioAsync_ConFuncionarioSinPermiso_LanzaExcepcionYNoRegistraNada()
    {
        var repository = new FakeBitacoraRepository
        {
            FuncionarioARetornar = new FuncionarioAccesoInfo(2, "jefe_area", AreaId: 5, Activo: true),
            DenunciaARetornar = new DenunciaAccesoInfo(9001, AreaActualId: 8, FuncionarioAsignadoId: null),
        };
        var service = CrearServicio(repository);
        var request = new AgregarComentarioBitacoraRequest(FuncionarioId: 2, Contenido: "Intento no autorizado");

        await Assert.ThrowsAsync<BitacoraAccesoDenegadoException>(() =>
            service.AgregarComentarioAsync(9001, request, CancellationToken.None));

        Assert.Empty(repository.RegistrosInsertados);
    }

    // AC #10: un jefe de area consultando la bitacora de una denuncia de
    // otra area debe ser rechazado.
    [Fact]
    public async Task ConsultarAsync_ConJefeDeAreaDeOtraArea_LanzaExcepcionAccesoDenegado()
    {
        var repository = new FakeBitacoraRepository
        {
            FuncionarioARetornar = new FuncionarioAccesoInfo(1, "jefe_area", AreaId: 5, Activo: true),
            DenunciaARetornar = new DenunciaAccesoInfo(9001, AreaActualId: 8, FuncionarioAsignadoId: null),
        };
        var service = CrearServicio(repository);

        await Assert.ThrowsAsync<BitacoraAccesoDenegadoException>(() =>
            service.ConsultarAsync(9001, 1, null, 1, 20, CancellationToken.None));
    }

    // AC #10: el validador solo puede consultar la bitacora de denuncias que
    // tiene asignadas.
    [Fact]
    public async Task ConsultarAsync_ConValidadorNoAsignado_LanzaExcepcionAccesoDenegado()
    {
        var repository = new FakeBitacoraRepository
        {
            FuncionarioARetornar = new FuncionarioAccesoInfo(100, "validador", AreaId: 5, Activo: true),
            DenunciaARetornar = new DenunciaAccesoInfo(9001, AreaActualId: 5, FuncionarioAsignadoId: 101),
        };
        var service = CrearServicio(repository);

        await Assert.ThrowsAsync<BitacoraAccesoDenegadoException>(() =>
            service.ConsultarAsync(9001, 100, null, 1, 20, CancellationToken.None));
    }

    // AC #9: la bitacora debe estar disponible para consulta y auditoria; el
    // superusuario puede consultar cualquier denuncia, sin restriccion de
    // area o asignacion.
    [Fact]
    public async Task ConsultarAsync_ConSuperusuario_DevuelveLaPaginaDelRepositorio()
    {
        var entradas = new List<BitacoraEntradaDto>
        {
            new(1, 9001, 1, "Jefe Prueba", 5, "Area 1", "2026-01-01T08:00:00.000", "asignacion", "Primer evento"),
            new(2, 9001, null, null, 5, "Area 1", "2026-01-01T09:00:00.000", "comentario", "Comentario manual"),
        };
        var repository = new FakeBitacoraRepository
        {
            FuncionarioARetornar = new FuncionarioAccesoInfo(201, "superusuario", AreaId: 1, Activo: true),
            DenunciaARetornar = new DenunciaAccesoInfo(9001, AreaActualId: 8, FuncionarioAsignadoId: null),
            ResultadoListarARetornar = (entradas, entradas.Count),
        };
        var service = CrearServicio(repository);

        var resultado = await service.ConsultarAsync(9001, 201, null, 1, 20, CancellationToken.None);

        Assert.Equal(2, resultado.Total);
        Assert.Equal(entradas, resultado.Items);
        Assert.Equal(1, resultado.Pagina);
        Assert.Equal(20, resultado.TamanoPagina);
    }

    // Bug reportado: el listado del panel de busqueda de Bitacora dejaba de
    // incluir una denuncia en cuanto se le asignaba un funcionario encargado
    // (se armaba combinando endpoints de HU-009/HU-010 que excluian ese
    // caso). El nuevo listado propio de HU-016 no debe filtrar por estado.
    [Fact]
    public async Task ListarDenunciasVisiblesAsync_ConSuperusuario_NoFiltraPorAreaNiFuncionario()
    {
        var repository = new FakeBitacoraRepository
        {
            FuncionarioARetornar = new FuncionarioAccesoInfo(201, "superusuario", AreaId: 1, Activo: true),
        };
        var service = CrearServicio(repository);

        await service.ListarDenunciasVisiblesAsync(201, CancellationToken.None);

        Assert.Equal((null, null), repository.FiltroVisiblesRecibido);
    }

    [Fact]
    public async Task ListarDenunciasVisiblesAsync_ConJefeDeArea_FiltraPorSuArea()
    {
        var repository = new FakeBitacoraRepository
        {
            FuncionarioARetornar = new FuncionarioAccesoInfo(1, "jefe_area", AreaId: 5, Activo: true),
        };
        var service = CrearServicio(repository);

        await service.ListarDenunciasVisiblesAsync(1, CancellationToken.None);

        Assert.Equal(((long?)5, (long?)null), repository.FiltroVisiblesRecibido);
    }

    // El validador solo ve lo que tiene asignado actualmente (misma regla
    // que ConsultarAsync usa para autorizar el detalle): asi la lista nunca
    // muestra una denuncia que despues, al abrirla, resulte con acceso
    // denegado.
    [Fact]
    public async Task ListarDenunciasVisiblesAsync_ConValidador_FiltraPorFuncionarioAsignado()
    {
        var repository = new FakeBitacoraRepository
        {
            FuncionarioARetornar = new FuncionarioAccesoInfo(100, "validador", AreaId: 5, Activo: true),
        };
        var service = CrearServicio(repository);

        await service.ListarDenunciasVisiblesAsync(100, CancellationToken.None);

        Assert.Equal(((long?)null, (long?)100), repository.FiltroVisiblesRecibido);
    }

    [Fact]
    public async Task ListarDenunciasVisiblesAsync_ConFuncionarioInactivo_LanzaExcepcionAccesoDenegado()
    {
        var repository = new FakeBitacoraRepository
        {
            FuncionarioARetornar = new FuncionarioAccesoInfo(100, "validador", AreaId: 5, Activo: false),
        };
        var service = CrearServicio(repository);

        await Assert.ThrowsAsync<BitacoraAccesoDenegadoException>(() =>
            service.ListarDenunciasVisiblesAsync(100, CancellationToken.None));
    }

    // AC #7, #13: la bitacora no debe permitir eliminar ni alterar registros
    // ya almacenados. A nivel de base de datos esto lo garantiza el trigger
    // trg_bitacora_inmutable (BEFORE UPDATE OR DELETE, ver SDPIS.sql); a
    // nivel de aplicacion, ninguna de las dos interfaces del servicio de
    // bitacora debe siquiera exponer una operacion de actualizar/eliminar.
    [Fact]
    public void IBitacoraRepository_NoExponeOperacionesDeActualizarOEliminar()
    {
        var nombresDeMetodos = typeof(IBitacoraRepository).GetMethods().Select(m => m.Name).ToList();

        Assert.DoesNotContain(nombresDeMetodos, n => n.Contains("Actualizar", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(nombresDeMetodos, n => n.Contains("Eliminar", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(nombresDeMetodos, n => n.Contains("Borrar", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(nombresDeMetodos, n => n.Contains("Modificar", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void IBitacoraService_NoExponeOperacionesDeActualizarOEliminar()
    {
        var nombresDeMetodos = typeof(IBitacoraService).GetMethods().Select(m => m.Name).ToList();

        Assert.DoesNotContain(nombresDeMetodos, n => n.Contains("Actualizar", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(nombresDeMetodos, n => n.Contains("Eliminar", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(nombresDeMetodos, n => n.Contains("Borrar", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(nombresDeMetodos, n => n.Contains("Modificar", StringComparison.OrdinalIgnoreCase));
    }
}