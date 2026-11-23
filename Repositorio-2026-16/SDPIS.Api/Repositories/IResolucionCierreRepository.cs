using SDPIS.Api.Models.DTOs;

namespace SDPIS.Api.Repositories;

// HU-017: lo necesario de una solicitud de cierre para decidir si la
// jefatura puede resolverla. Estado: PENDIENTE | APROBADA | RECHAZADA;
// EstadoDenuncia: estado actual de la denuncia (deberia ser EN_REVISION).
public record SolicitudResolucionInfo(long IdSolicitudCierre, long DenunciaId, long AreaId,
    string Estado, string EstadoDenuncia);

// HU-017: la unica via de escritura es sp_resolver_solicitud_cierre (decision,
// cambio de estado, bitacora y notificacion en una sola transaccion).
public interface IResolucionCierreRepository
{
    Task<SolicitudResolucionInfo?> ObtenerSolicitudAsync(long solicitudId, CancellationToken ct);

    // decision: APROBADA | RECHAZADA. Devuelve el estado final de la denuncia.
    Task<string> ResolverAsync(long solicitudId, long jefeId, string decision, string? observaciones,
        CancellationToken ct);

    Task<DecisionCierreDto?> ObtenerDecisionAsync(long solicitudId, CancellationToken ct);

    // Todas las solicitudes de la denuncia con su decision; la mas reciente primero.
    Task<List<HistorialSolicitudCierreDto>> ListarHistorialAsync(long denunciaId, CancellationToken ct);
}
