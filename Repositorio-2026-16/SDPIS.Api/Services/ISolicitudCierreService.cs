using SDPIS.Api.Models.DTOs;

namespace SDPIS.Api.Services;

// HU-018: solicitar el cierre de una denuncia a la jefatura del area. La
// aprobacion/rechazo es HU-017.
public interface ISolicitudCierreService
{
    // Si el usuario autenticado puede solicitar el cierre y la solicitud
    // pendiente (si hay). 403 si la denuncia esta fuera de su alcance.
    Task<EstadoSolicitudCierreDto> ObtenerEstadoAsync(long denunciaId, CancellationToken ct);

    // Solo el funcionario responsable asignado. La denuncia pasa a EN_REVISION.
    Task<SolicitudCierreDto> SolicitarAsync(long denunciaId, SolicitarCierreRequest request, CancellationToken ct);

    // Bandeja de la jefatura: pendientes de su area (superusuario: todas).
    Task<List<SolicitudCierreDto>> ListarPendientesAsync(CancellationToken ct);
}
