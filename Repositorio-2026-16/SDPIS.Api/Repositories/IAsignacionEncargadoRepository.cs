using SDPIS.Api.Models.DTOs;

namespace SDPIS.Api.Repositories;

public interface IAsignacionEncargadoRepository
{
    Task<List<DenunciaParaEncargadoDto>> ObtenerDenunciasBandejaAsync(long areaId, CancellationToken ct);

    Task<List<FuncionarioEncargadoDto>> ObtenerEncargadosDisponiblesAsync(long areaId, CancellationToken ct);

    Task<List<AreaReasignacionDto>> ObtenerAreasDestinoAsync(CancellationToken ct);

    Task<List<HistorialAsignacionDto>> ObtenerHistorialAsync(long denunciaId, CancellationToken ct);

    Task AsignarEncargadoAsync(long denunciaId, long encargadoId, long jefeAreaId, string? motivo, CancellationToken ct);

    Task ReasignarAreaAsync(long denunciaId, long areaNuevaId, long jefeAreaId, string? motivo, CancellationToken ct);

    Task ReasignarAUvcAsync(long denunciaId, long jefeAreaId, string motivo, string? numeroOficio, CancellationToken ct);

    Task<int> GenerarAlertasVencimientoAsync(long areaId, CancellationToken ct);

    Task ConfirmarAtencionUvcAsync(long denunciaId, long superusuarioId, string? motivo, CancellationToken ct);

    Task DevolverAreaRectoraAsync(long denunciaId, long areaRectoraId, long superusuarioId, string? motivo, CancellationToken ct);
}