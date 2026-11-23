using SDPIS.Api.Models.DTOs;

namespace SDPIS.Api.Services;

// HU-009 + HU-015: areaId null = todas las areas (solo superusuario).
public interface IAsignacionService
{
    Task<List<DenunciaPendienteDto>> ObtenerDenunciasPendientesAsync(long? areaId, CancellationToken ct);
    Task<List<DenunciaAsignadaDto>> ObtenerDenunciasAsignadasAsync(long? areaId, CancellationToken ct);
    Task<List<ValidadorCargaDto>> ObtenerValidadoresDisponiblesAsync(long? areaId, CancellationToken ct);
    Task AsignarDenunciaAsync(long denunciaId, long validadorId, CancellationToken ct);
}