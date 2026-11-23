using SDPIS.Api.Models.DTOs;

namespace SDPIS.Api.Repositories;

public interface IAsignacionRepository
{
    Task<List<DenunciaPendienteDto>> ObtenerDenunciasPendientesAsignacionAsync(long? areaId, CancellationToken ct);
    Task<List<DenunciaAsignadaDto>> ObtenerDenunciasAsignadasAsync(long? areaId, CancellationToken ct);
    Task<List<ValidadorCargaDto>> ObtenerValidadoresDisponiblesConCargaAsync(long areaId, CancellationToken ct);
    Task AsignarValidadorManualAsync(long denunciaId, long validadorId, long jefeAreaId, CancellationToken ct);
    Task<List<DenunciaAtrasadaDto>> ObtenerDenunciasAtrasadasAsignacionAsync(int diasLimite, CancellationToken ct);
    Task GenerarRecordatorioAtrasoAsync(long denunciaId, string consecutivo, CancellationToken ct);
}
