using SDPIS.Api.Models.DTOs;

namespace SDPIS.Api.Services;

// HU-017: la jefatura aprueba o rechaza la solicitud de cierre (HU-018).
public interface IResolucionCierreService
{
    // La denuncia pasa a FINALIZADA. Observaciones opcionales.
    Task<ResolucionCierreDto> AprobarAsync(long solicitudId, ResolverCierreRequest? request, CancellationToken ct);

    // La denuncia vuelve al estado previo a la revision. Observaciones obligatorias.
    Task<ResolucionCierreDto> RechazarAsync(long solicitudId, ResolverCierreRequest? request, CancellationToken ct);

    // Historial de solicitudes y decisiones. 404/403 segun la visibilidad de la denuncia.
    Task<List<HistorialSolicitudCierreDto>> ObtenerHistorialAsync(long denunciaId, CancellationToken ct);
}
