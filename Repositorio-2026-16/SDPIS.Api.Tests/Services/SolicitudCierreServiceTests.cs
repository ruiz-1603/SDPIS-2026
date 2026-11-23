using SDPIS.Api.Models.DTOs;
using SDPIS.Api.Repositories;
using SDPIS.Api.Services;
using SDPIS.Api.Services.Autorizacion;
using SDPIS.Api.Services.GestionUsuarios;
using SDPIS.Api.Services.Validation;
using Xunit;

namespace SDPIS.Api.Tests.Services;

// HU-018 Solicitar cierre de denuncia. Los permisos se calculan con la
// logica REAL de CatalogoPermisos (FakePermisosService), asi que un
// "validador" o un "jefe_area" se comportan como en el sistema.
public class SolicitudCierreServiceTests
{
    private const long DenunciaId = 9001;
    private const long Responsable = 100;
    private const long AreaDenuncia = 5;

    private static readonly SolicitarCierreRequest RequestValido = new(
        "Se verifico el retiro del producto.",
        "El establecimiento corrigio la situacion.",
        "Inspeccion en sitio y verificacion documental.");

    private static SolicitudCierreService CrearServicio(
        FakeSolicitudCierreRepository repository, long funcionarioId, string rol,
        long areaId = AreaDenuncia, bool activo = true, IReadOnlyList<string>? delegados = null)
        => new(repository,
            new FakeUsuarioActual(funcionarioId),
            new FakePermisosService(new FuncionarioPermisosInfo(
                funcionarioId, rol, activo, areaId, false, delegados ?? [])),
            new FakeVisibilidadDenunciasService());

    // ---------- Solicitud valida (caso de prueba #194) ----------

    // CA #1 y #2: el responsable asignado solicita el cierre; el SP (que
    // cambia el estado a EN_REVISION, registra la bitacora y notifica) se
    // llama con el funcionario del token y los textos normalizados.
    [Fact]
    public async Task SolicitarAsync_ResponsableAsignado_RegistraLaSolicitud()
    {
        var repository = new FakeSolicitudCierreRepository();
        var service = CrearServicio(repository, Responsable, Roles.Validador);
        var request = new SolicitarCierreRequest("  Resolucion  ", " Observaciones ", " Acciones ");

        var solicitud = await service.SolicitarAsync(DenunciaId, request, CancellationToken.None);

        var (denunciaId, funcionarioId, enviado) = Assert.Single(repository.Solicitudes);
        Assert.Equal(DenunciaId, denunciaId);
        Assert.Equal(Responsable, funcionarioId);
        Assert.Equal(new SolicitarCierreRequest("Resolucion", "Observaciones", "Acciones"), enviado);
        Assert.Equal("PENDIENTE", solicitud.Estado);
        Assert.Equal(AreaDenuncia, solicitud.AreaId);
    }

    [Theory]
    [InlineData("EN_TIEMPO")]
    [InlineData("PRONTA_VENCER")]
    [InlineData("ATRASADA")]
    public async Task SolicitarAsync_DenunciaEnCurso_PermiteSolicitar(string estado)
    {
        var repository = new FakeSolicitudCierreRepository
        {
            DenunciaARetornar = new DenunciaCierreInfo(DenunciaId, estado, AreaDenuncia, Responsable),
        };
        var service = CrearServicio(repository, Responsable, Roles.Validador);

        await service.SolicitarAsync(DenunciaId, RequestValido, CancellationToken.None);

        Assert.Single(repository.Solicitudes);
    }

    // ---------- Funcionario no responsable (caso de prueba #194) ----------

    // CA #1: otro funcionario del area no puede solicitar el cierre.
    [Fact]
    public async Task SolicitarAsync_FuncionarioNoResponsable_LanzaPermisoDenegadoYNoRegistra()
    {
        var repository = new FakeSolicitudCierreRepository();
        var service = CrearServicio(repository, funcionarioId: 200, Roles.Validador);

        var ex = await Assert.ThrowsAsync<PermisoDenegadoException>(() =>
            service.SolicitarAsync(DenunciaId, RequestValido, CancellationToken.None));

        Assert.Contains("responsable asignado", ex.Message);
        Assert.Empty(repository.Solicitudes);
    }

    // El jefe de area no tiene SOLICITAR_CIERRE (aprueba, no solicita).
    [Fact]
    public async Task SolicitarAsync_JefeDeArea_LanzaPermisoDenegadoYNoRegistra()
    {
        var repository = new FakeSolicitudCierreRepository
        {
            DenunciaARetornar = new DenunciaCierreInfo(DenunciaId, "EN_TIEMPO", AreaDenuncia, FuncionarioAsignadoId: 1),
        };
        var service = CrearServicio(repository, funcionarioId: 1, Roles.JefeArea);

        await Assert.ThrowsAsync<PermisoDenegadoException>(() =>
            service.SolicitarAsync(DenunciaId, RequestValido, CancellationToken.None));

        Assert.Empty(repository.Solicitudes);
    }

    [Fact]
    public async Task SolicitarAsync_ResponsableInactivo_LanzaPermisoDenegado()
    {
        var repository = new FakeSolicitudCierreRepository();
        var service = CrearServicio(repository, Responsable, Roles.Validador, activo: false);

        await Assert.ThrowsAsync<PermisoDenegadoException>(() =>
            service.SolicitarAsync(DenunciaId, RequestValido, CancellationToken.None));

        Assert.Empty(repository.Solicitudes);
    }

    // ---------- Estado de la denuncia ----------

    // CA #4: la solicitud queda pendiente; no se puede enviar otra.
    [Fact]
    public async Task SolicitarAsync_DenunciaYaEnRevision_LanzaReglaNegocioYNoRegistra()
    {
        var repository = new FakeSolicitudCierreRepository
        {
            DenunciaARetornar = new DenunciaCierreInfo(DenunciaId, "EN_REVISION", AreaDenuncia, Responsable),
        };
        var service = CrearServicio(repository, Responsable, Roles.Validador);

        var ex = await Assert.ThrowsAsync<ReglaNegocioException>(() =>
            service.SolicitarAsync(DenunciaId, RequestValido, CancellationToken.None));

        Assert.Contains("pendiente", ex.Message);
        Assert.Empty(repository.Solicitudes);
    }

    [Theory]
    [InlineData("PENDIENTE_ASIGNACION")]
    [InlineData("FINALIZADA")]
    public async Task SolicitarAsync_DenunciaNoEnCurso_LanzaReglaNegocioYNoRegistra(string estado)
    {
        var repository = new FakeSolicitudCierreRepository
        {
            DenunciaARetornar = new DenunciaCierreInfo(DenunciaId, estado, AreaDenuncia, Responsable),
        };
        var service = CrearServicio(repository, Responsable, Roles.Validador);

        await Assert.ThrowsAsync<ReglaNegocioException>(() =>
            service.SolicitarAsync(DenunciaId, RequestValido, CancellationToken.None));

        Assert.Empty(repository.Solicitudes);
    }

    [Fact]
    public async Task SolicitarAsync_DenunciaInexistente_LanzaRecursoNoEncontrado()
    {
        var repository = new FakeSolicitudCierreRepository { DenunciaARetornar = null };
        var service = CrearServicio(repository, Responsable, Roles.Validador);

        await Assert.ThrowsAsync<RecursoNoEncontradoException>(() =>
            service.SolicitarAsync(DenunciaId, RequestValido, CancellationToken.None));
    }

    // ---------- Entradas obligatorias ----------

    [Fact]
    public async Task SolicitarAsync_SinResolucionObservacionesNiAcciones_LanzaValidacionYNoRegistra()
    {
        var repository = new FakeSolicitudCierreRepository();
        var service = CrearServicio(repository, Responsable, Roles.Validador);

        var ex = await Assert.ThrowsAsync<DenunciaValidationException>(() =>
            service.SolicitarAsync(DenunciaId, new SolicitarCierreRequest(" ", "", "   "), CancellationToken.None));

        Assert.Equal(3, ex.Errores.Count);
        Assert.Empty(repository.Solicitudes);
    }

    // ---------- Estado para la bitacora (boton) ----------

    [Fact]
    public async Task ObtenerEstadoAsync_Responsable_PuedeSolicitar()
    {
        var service = CrearServicio(new FakeSolicitudCierreRepository(), Responsable, Roles.Validador);

        var estado = await service.ObtenerEstadoAsync(DenunciaId, CancellationToken.None);

        Assert.True(estado.PuedeSolicitar);
        Assert.Null(estado.Motivo);
        Assert.Null(estado.SolicitudPendiente);
    }

    [Fact]
    public async Task ObtenerEstadoAsync_JefeDeArea_NoPuedeSolicitarPeroVeLaPendiente()
    {
        var pendiente = new SolicitudCierreDto(1, DenunciaId, "ABAN-2026-000001", Responsable, "Funcionario",
            AreaDenuncia, "Area", "r", "o", "a", "2026-10-08T10:00:00.000", "PENDIENTE");
        var repository = new FakeSolicitudCierreRepository
        {
            DenunciaARetornar = new DenunciaCierreInfo(DenunciaId, "EN_REVISION", AreaDenuncia, Responsable),
            PendienteARetornar = pendiente,
        };
        var service = CrearServicio(repository, funcionarioId: 1, Roles.JefeArea);

        var estado = await service.ObtenerEstadoAsync(DenunciaId, CancellationToken.None);

        Assert.False(estado.PuedeSolicitar);
        Assert.NotNull(estado.Motivo);
        Assert.Equal(pendiente, estado.SolicitudPendiente);
    }

    [Fact]
    public async Task ObtenerEstadoAsync_DevuelveLasCarpetasDelExpediente()
    {
        var repository = new FakeSolicitudCierreRepository
        {
            DenunciaARetornar = new DenunciaCierreInfo(DenunciaId, "EN_TIEMPO", AreaDenuncia, Responsable,
                "https://carpetas/confidencial", null),
        };
        var service = CrearServicio(repository, Responsable, Roles.Validador);

        var estado = await service.ObtenerEstadoAsync(DenunciaId, CancellationToken.None);

        Assert.Equal("https://carpetas/confidencial", estado.CarpetaConfidencialUrl);
        Assert.Null(estado.CarpetaNoConfidencialUrl);
    }

    // ---------- Bandeja de la jefatura ----------

    [Fact]
    public async Task ListarPendientesAsync_JefeDeArea_ListaSoloSuArea()
    {
        var repository = new FakeSolicitudCierreRepository();
        var service = CrearServicio(repository, funcionarioId: 1, Roles.JefeArea, areaId: 7);

        await service.ListarPendientesAsync(CancellationToken.None);

        Assert.True(repository.SeListaronPendientes);
        Assert.Equal(7, repository.AreaListadaRecibida);
    }

    [Fact]
    public async Task ListarPendientesAsync_Superusuario_ListaTodasLasAreas()
    {
        var repository = new FakeSolicitudCierreRepository();
        var service = CrearServicio(repository, funcionarioId: 2, Roles.Superusuario, areaId: 30);

        await service.ListarPendientesAsync(CancellationToken.None);

        Assert.True(repository.SeListaronPendientes);
        Assert.Null(repository.AreaListadaRecibida);
    }

    [Fact]
    public async Task ListarPendientesAsync_Funcionario_LanzaPermisoDenegado()
    {
        var repository = new FakeSolicitudCierreRepository();
        var service = CrearServicio(repository, Responsable, Roles.Validador);

        await Assert.ThrowsAsync<PermisoDenegadoException>(() =>
            service.ListarPendientesAsync(CancellationToken.None));

        Assert.False(repository.SeListaronPendientes);
    }

    // Un validador con "Ver denuncias del area" delegado NO es jefatura:
    // APROBAR_CIERRE no es delegable.
    [Fact]
    public async Task ListarPendientesAsync_FuncionarioConVerAreaDelegado_LanzaPermisoDenegado()
    {
        var repository = new FakeSolicitudCierreRepository();
        var service = CrearServicio(repository, Responsable, Roles.Validador,
            delegados: [Permisos.VerDenunciasArea]);

        await Assert.ThrowsAsync<PermisoDenegadoException>(() =>
            service.ListarPendientesAsync(CancellationToken.None));
    }
}
