using SDPIS.Api.Models.DTOs;
using SDPIS.Api.Repositories;
using SDPIS.Api.Services.Autorizacion;
using SDPIS.Api.Services.GestionUsuarios;
using SDPIS.Api.Services.Validation;

namespace SDPIS.Api.Services;

// HU-017. El jefe sale SIEMPRE del token (IUsuarioActual), nunca del body.
// Reglas:
//   * Resolver: usuario activo con APROBAR_CIERRE (jefe_area y superusuario;
//     no es delegable). El jefe solo resuelve las solicitudes de SU area
//     (solicitud_cierre.area_id); el superusuario, las de cualquier area
//     (p. ej. un area sin jefe activo).
//   * Solo solicitudes PENDIENTE de denuncias EN_REVISION.
//   * El cambio de estado (FINALIZADA o el estado previo), la bitacora y la
//     notificacion los hace sp_resolver_solicitud_cierre, que vuelve a
//     validar todo dentro de la transaccion.
//   * Historial: cualquiera que pueda ver la denuncia (IVisibilidadDenunciasService).
public class ResolucionCierreService(
    IResolucionCierreRepository repository,
    IUsuarioActual usuario,
    IPermisosService permisos,
    IVisibilidadDenunciasService visibilidad) : IResolucionCierreService
{
    public const string DecisionAprobada = "APROBADA";
    public const string DecisionRechazada = "RECHAZADA";
    private const string EstadoPendiente = "PENDIENTE";
    private const string EstadoEnRevision = "EN_REVISION";

    public Task<ResolucionCierreDto> AprobarAsync(long solicitudId, ResolverCierreRequest? request, CancellationToken ct)
    {
        ResolucionCierreValidator.ValidarAprobacion(solicitudId, request);
        return ResolverAsync(solicitudId, DecisionAprobada, request?.Observaciones, ct);
    }

    public Task<ResolucionCierreDto> RechazarAsync(long solicitudId, ResolverCierreRequest? request, CancellationToken ct)
    {
        ResolucionCierreValidator.ValidarRechazo(solicitudId, request);
        return ResolverAsync(solicitudId, DecisionRechazada, request?.Observaciones, ct);
    }

    public async Task<List<HistorialSolicitudCierreDto>> ObtenerHistorialAsync(long denunciaId, CancellationToken ct)
    {
        await visibilidad.VerificarAccesoDenunciaAsync(denunciaId, ct);
        return await repository.ListarHistorialAsync(denunciaId, ct);
    }

    private async Task<ResolucionCierreDto> ResolverAsync(long solicitudId, string decision, string? observaciones,
        CancellationToken ct)
    {
        var actor = await permisos.ObtenerContextoAsync(usuario.FuncionarioId, ct);
        if (actor is null || !actor.Activo || !actor.Tiene(Permisos.AprobarCierre))
            throw new PermisoDenegadoException(
                "Solo la jefatura de área o un superusuario puede aprobar o rechazar la resolución final de una denuncia.");

        var solicitud = await repository.ObtenerSolicitudAsync(solicitudId, ct)
            ?? throw new RecursoNoEncontradoException("La solicitud de cierre indicada no existe.");

        // Por rol y no por VER_TODAS_AREAS: ese permiso es delegable y no debe
        // habilitar a un jefe a resolver cierres de otras areas.
        if (!actor.EsRol(Roles.Superusuario) && solicitud.AreaId != actor.AreaId)
            throw new PermisoDenegadoException(
                "Solo la jefatura del área a la que se envió la solicitud puede aprobar o rechazar la resolución.");

        if (solicitud.Estado != EstadoPendiente)
            throw new ReglaNegocioException("La solicitud de cierre ya fue resuelta por la jefatura.");

        if (solicitud.EstadoDenuncia != EstadoEnRevision)
            throw new ReglaNegocioException(
                "La denuncia no está en revisión: no se puede resolver la solicitud de cierre.");

        var texto = string.IsNullOrWhiteSpace(observaciones) ? null : observaciones.Trim();
        var estadoFinal = await repository.ResolverAsync(solicitudId, actor.FuncionarioId, decision, texto, ct);

        var registrada = await repository.ObtenerDecisionAsync(solicitudId, ct)
            ?? throw new InvalidOperationException("No se pudo leer la decisión recién registrada.");
        return new ResolucionCierreDto(solicitudId, solicitud.DenunciaId, estadoFinal, registrada);
    }
}
