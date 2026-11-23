using SDPIS.Api.Models.DTOs;
using SDPIS.Api.Repositories;
using SDPIS.Api.Services;
using SDPIS.Api.Services.Autorizacion;
using SDPIS.Api.Services.GestionUsuarios;
using SDPIS.Api.Services.Validation;
using Xunit;

namespace SDPIS.Api.Tests.Services;

// HU-017 Gestionar aprobacion de resolucion de denuncias. Los permisos se
// calculan con la logica REAL de CatalogoPermisos (FakePermisosService), asi
// que un "jefe_area", "validador" o "superusuario" se comportan como en el
// sistema. El cambio de estado lo hace el SP; el fake lo simula.
public class ResolucionCierreServiceTests
{
    private const long SolicitudId = 7001;
    private const long DenunciaId = 9001;
    private const long Jefe = 1;
    private const long AreaSolicitud = 5;

    private static ResolucionCierreService CrearServicio(
        FakeResolucionCierreRepository repository, long funcionarioId = Jefe, string rol = Roles.JefeArea,
        long areaId = AreaSolicitud, bool activo = true, IVisibilidadDenunciasService? visibilidad = null)
        => new(repository,
            new FakeUsuarioActual(funcionarioId),
            new FakePermisosService(new FuncionarioPermisosInfo(funcionarioId, rol, activo, areaId, false, [])),
            visibilidad ?? new FakeVisibilidadDenunciasService());

    // ---------- Aprobacion (caso de prueba #190) ----------

    // CA #3 y #6: la jefatura del area aprueba; el SP se llama con el jefe
    // del token y la denuncia queda FINALIZADA.
    [Fact]
    public async Task AprobarAsync_JefeDelArea_RegistraLaAprobacionYFinaliza()
    {
        var repository = new FakeResolucionCierreRepository();
        var service = CrearServicio(repository);

        var resultado = await service.AprobarAsync(SolicitudId, new ResolverCierreRequest("  Conforme.  "), CancellationToken.None);

        var (solicitudId, jefeId, decision, observaciones) = Assert.Single(repository.Resoluciones);
        Assert.Equal(SolicitudId, solicitudId);
        Assert.Equal(Jefe, jefeId);
        Assert.Equal("APROBADA", decision);
        Assert.Equal("Conforme.", observaciones);
        Assert.Equal("FINALIZADA", resultado.EstadoDenuncia);
        Assert.Equal(DenunciaId, resultado.DenunciaId);
        Assert.Equal(Jefe, resultado.Decision.DecididoPorId);
    }

    // CA de la HU: las observaciones son opcionales al aprobar (sin body o en blanco).
    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task AprobarAsync_SinObservaciones_ApruebaConObservacionesNulas(string? observaciones)
    {
        var repository = new FakeResolucionCierreRepository();
        var service = CrearServicio(repository);
        var request = observaciones is null ? null : new ResolverCierreRequest(observaciones);

        await service.AprobarAsync(SolicitudId, request, CancellationToken.None);

        Assert.Null(Assert.Single(repository.Resoluciones).Observaciones);
    }

    // ---------- Rechazo (caso de prueba #190) ----------

    // CA #2 y #6: el rechazo devuelve la denuncia al estado que tenia antes
    // de la solicitud de cierre.
    [Theory]
    [InlineData("EN_TIEMPO")]
    [InlineData("PRONTA_VENCER")]
    [InlineData("ATRASADA")]
    public async Task RechazarAsync_JefeDelArea_VuelveAlEstadoPrevio(string estadoPrevio)
    {
        var repository = new FakeResolucionCierreRepository { EstadoPrevio = estadoPrevio };
        var service = CrearServicio(repository);

        var resultado = await service.RechazarAsync(
            SolicitudId, new ResolverCierreRequest(" Falta el acta de inspeccion. "), CancellationToken.None);

        var (_, _, decision, observaciones) = Assert.Single(repository.Resoluciones);
        Assert.Equal("RECHAZADA", decision);
        Assert.Equal("Falta el acta de inspeccion.", observaciones);
        Assert.Equal(estadoPrevio, resultado.EstadoDenuncia);
    }

    // Fallback documentado: solicitud sin estado previo (anterior a HU-017).
    [Fact]
    public async Task RechazarAsync_SinEstadoPrevio_VuelveAEnTiempo()
    {
        var repository = new FakeResolucionCierreRepository { EstadoPrevio = null };
        var service = CrearServicio(repository);

        var resultado = await service.RechazarAsync(SolicitudId, new ResolverCierreRequest("Ajustar."), CancellationToken.None);

        Assert.Equal("EN_TIEMPO", resultado.EstadoDenuncia);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RechazarAsync_SinObservaciones_LanzaValidacionYNoResuelve(string? observaciones)
    {
        var repository = new FakeResolucionCierreRepository();
        var service = CrearServicio(repository);
        var request = observaciones is null ? null : new ResolverCierreRequest(observaciones);

        var ex = await Assert.ThrowsAsync<DenunciaValidationException>(() =>
            service.RechazarAsync(SolicitudId, request, CancellationToken.None));

        Assert.Contains(ex.Errores, e => e.Contains("observaciones"));
        Assert.Empty(repository.Resoluciones);
    }

    // ---------- Intento sin permisos (caso de prueba #190) ----------

    // CA #4: solo la jefatura. El funcionario (validador) no tiene APROBAR_CIERRE.
    [Fact]
    public async Task AprobarAsync_FuncionarioSinPermiso_LanzaPermisoDenegadoYNoResuelve()
    {
        var repository = new FakeResolucionCierreRepository();
        var service = CrearServicio(repository, funcionarioId: 100, rol: Roles.Validador);

        await Assert.ThrowsAsync<PermisoDenegadoException>(() =>
            service.AprobarAsync(SolicitudId, null, CancellationToken.None));

        Assert.Empty(repository.Resoluciones);
    }

    // El superusuario tambien resuelve, y sin importar el area de la solicitud
    // (p. ej. un area sin jefe activo).
    [Fact]
    public async Task AprobarAsync_SuperusuarioDeOtraArea_RegistraLaAprobacion()
    {
        var repository = new FakeResolucionCierreRepository();
        var service = CrearServicio(repository, funcionarioId: 50, rol: Roles.Superusuario, areaId: 99);

        var resultado = await service.AprobarAsync(SolicitudId, null, CancellationToken.None);

        var (_, decididoPor, decision, _) = Assert.Single(repository.Resoluciones);
        Assert.Equal(50, decididoPor);
        Assert.Equal("APROBADA", decision);
        Assert.Equal("FINALIZADA", resultado.EstadoDenuncia);
    }

    [Fact]
    public async Task RechazarAsync_SuperusuarioDeOtraArea_RegistraElRechazo()
    {
        var repository = new FakeResolucionCierreRepository();
        var service = CrearServicio(repository, funcionarioId: 50, rol: Roles.Superusuario, areaId: 99);

        await service.RechazarAsync(SolicitudId, new ResolverCierreRequest("Ajustar el informe."), CancellationToken.None);

        var (_, decididoPor, decision, observaciones) = Assert.Single(repository.Resoluciones);
        Assert.Equal(50, decididoPor);
        Assert.Equal("RECHAZADA", decision);
        Assert.Equal("Ajustar el informe.", observaciones);
    }

    // Un superusuario inactivo no resuelve.
    [Fact]
    public async Task AprobarAsync_SuperusuarioInactivo_LanzaPermisoDenegadoYNoResuelve()
    {
        var repository = new FakeResolucionCierreRepository();
        var service = CrearServicio(repository, funcionarioId: 50, rol: Roles.Superusuario, activo: false);

        await Assert.ThrowsAsync<PermisoDenegadoException>(() =>
            service.AprobarAsync(SolicitudId, null, CancellationToken.None));

        Assert.Empty(repository.Resoluciones);
    }

    [Fact]
    public async Task RechazarAsync_JefeInactivo_LanzaPermisoDenegadoYNoResuelve()
    {
        var repository = new FakeResolucionCierreRepository();
        var service = CrearServicio(repository, activo: false);

        await Assert.ThrowsAsync<PermisoDenegadoException>(() =>
            service.RechazarAsync(SolicitudId, new ResolverCierreRequest("Ajustar."), CancellationToken.None));

        Assert.Empty(repository.Resoluciones);
    }

    // CA #4: el jefe de OTRA area no puede resolver la solicitud.
    [Fact]
    public async Task AprobarAsync_JefeDeOtraArea_LanzaPermisoDenegadoYNoResuelve()
    {
        var repository = new FakeResolucionCierreRepository();
        var service = CrearServicio(repository, funcionarioId: 9110, areaId: 6);

        var ex = await Assert.ThrowsAsync<PermisoDenegadoException>(() =>
            service.AprobarAsync(SolicitudId, null, CancellationToken.None));

        Assert.Contains("jefatura del área", ex.Message);
        Assert.Empty(repository.Resoluciones);
    }

    // ---------- Estado de la solicitud y de la denuncia ----------

    [Theory]
    [InlineData("APROBADA")]
    [InlineData("RECHAZADA")]
    public async Task AprobarAsync_SolicitudYaResuelta_LanzaReglaNegocioYNoResuelve(string estadoSolicitud)
    {
        var repository = new FakeResolucionCierreRepository
        {
            SolicitudARetornar = new SolicitudResolucionInfo(SolicitudId, DenunciaId, AreaSolicitud, estadoSolicitud, "EN_TIEMPO"),
        };
        var service = CrearServicio(repository);

        var ex = await Assert.ThrowsAsync<ReglaNegocioException>(() =>
            service.AprobarAsync(SolicitudId, null, CancellationToken.None));

        Assert.Contains("ya fue resuelta", ex.Message);
        Assert.Empty(repository.Resoluciones);
    }

    // CA #5: no se cierra una denuncia que no esta en revision.
    [Fact]
    public async Task AprobarAsync_DenunciaNoEnRevision_LanzaReglaNegocioYNoResuelve()
    {
        var repository = new FakeResolucionCierreRepository
        {
            SolicitudARetornar = new SolicitudResolucionInfo(SolicitudId, DenunciaId, AreaSolicitud, "PENDIENTE", "EN_TIEMPO"),
        };
        var service = CrearServicio(repository);

        await Assert.ThrowsAsync<ReglaNegocioException>(() =>
            service.AprobarAsync(SolicitudId, null, CancellationToken.None));

        Assert.Empty(repository.Resoluciones);
    }

    // Denuncia sin solicitud de cierre pendiente: la solicitud no existe.
    [Fact]
    public async Task RechazarAsync_SolicitudInexistente_LanzaRecursoNoEncontrado()
    {
        var repository = new FakeResolucionCierreRepository { SolicitudARetornar = null };
        var service = CrearServicio(repository);

        await Assert.ThrowsAsync<RecursoNoEncontradoException>(() =>
            service.RechazarAsync(SolicitudId, new ResolverCierreRequest("Ajustar."), CancellationToken.None));

        Assert.Empty(repository.Resoluciones);
    }

    // ---------- Historial ----------

    [Fact]
    public async Task ObtenerHistorialAsync_ConAcceso_DevuelveElHistorialDeLaDenuncia()
    {
        var solicitud = new SolicitudCierreDto(SolicitudId, DenunciaId, "ABAN-2026-000001", 100, "Funcionario",
            AreaSolicitud, "Area", "Resolucion", "Obs", "Acciones", "2026-10-08T10:00:00.000", "RECHAZADA");
        var decision = new DecisionCierreDto(1, "RECHAZADA", Jefe, "Jefe", "Ajustar.", "2026-10-09T10:00:00.000");
        var repository = new FakeResolucionCierreRepository
        {
            HistorialARetornar = [new HistorialSolicitudCierreDto(solicitud, "ATRASADA", decision)],
        };
        var service = CrearServicio(repository);

        var historial = await service.ObtenerHistorialAsync(DenunciaId, CancellationToken.None);

        Assert.Equal(DenunciaId, repository.DenunciaHistorialConsultada);
        var entrada = Assert.Single(historial);
        Assert.Equal("ATRASADA", entrada.EstadoPrevio);
        Assert.Equal("RECHAZADA", entrada.Decision?.Decision);
    }

    [Fact]
    public async Task ObtenerHistorialAsync_SinAccesoALaDenuncia_PropagaPermisoDenegado()
    {
        var repository = new FakeResolucionCierreRepository();
        var service = CrearServicio(repository, visibilidad: new VisibilidadQueDeniega());

        await Assert.ThrowsAsync<PermisoDenegadoException>(() =>
            service.ObtenerHistorialAsync(DenunciaId, CancellationToken.None));

        Assert.Null(repository.DenunciaHistorialConsultada);
    }

    // Visibilidad que rechaza el acceso (403), como VerificarAccesoDenunciaAsync
    // cuando la denuncia esta fuera del alcance del usuario.
    private sealed class VisibilidadQueDeniega : IVisibilidadDenunciasService
    {
        public Task<AlcanceResuelto> ResolverAlcanceAsync(long? areaSolicitada, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<List<DenunciaVisibleDto>> ListarAsync(
            long? areaSolicitada, string? filtro, CancellationToken ct, string? criterios = null)
            => throw new NotSupportedException();

        public Task<long> ResolverAreaOperativaAsync(long? areaSolicitada, string permisoRequerido, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<DenunciaAlcanceRegistro> VerificarAccesoDenunciaAsync(long denunciaId, CancellationToken ct)
            => throw new PermisoDenegadoException("No tiene acceso a esta denuncia.");
    }
}
