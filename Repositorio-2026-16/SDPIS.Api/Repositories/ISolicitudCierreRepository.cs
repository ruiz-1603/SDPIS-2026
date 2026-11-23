using SDPIS.Api.Models.DTOs;

namespace SDPIS.Api.Repositories;

// HU-018: datos de la denuncia necesarios para decidir si se puede solicitar
// su cierre, y las carpetas del expediente (null si no tiene).
public record DenunciaCierreInfo(long IdDenuncia, string Estado, long AreaActualId, long? FuncionarioAsignadoId,
    string? CarpetaConfidencialUrl = null, string? CarpetaNoConfidencialUrl = null);

// HU-018: la unica via de escritura es sp_solicitar_cierre_denuncia (registra
// la solicitud, cambia el estado, escribe la bitacora y notifica en una sola
// transaccion).
public interface ISolicitudCierreRepository
{
    Task<DenunciaCierreInfo?> ObtenerDenunciaAsync(long denunciaId, CancellationToken ct);

    // Devuelve el id de la solicitud creada.
    Task<long> SolicitarAsync(long denunciaId, long funcionarioId, SolicitarCierreRequest request, CancellationToken ct);

    Task<SolicitudCierreDto?> ObtenerAsync(long solicitudId, CancellationToken ct);

    Task<SolicitudCierreDto?> ObtenerPendienteAsync(long denunciaId, CancellationToken ct);

    // areaId null = todas las areas (superusuario). Orden: mas antigua primero.
    Task<List<SolicitudCierreDto>> ListarPendientesAsync(long? areaId, CancellationToken ct);
}
