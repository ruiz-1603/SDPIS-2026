using SDPIS.Api.Models.DTOs;

namespace SDPIS.Api.Services;

public interface IAsignacionEncargadoService
{
    Task<List<DenunciaParaEncargadoDto>> ObtenerDenunciasBandejaAsync(long areaId, CancellationToken ct);

    Task<List<FuncionarioEncargadoDto>> ObtenerEncargadosDisponiblesAsync(long areaId, CancellationToken ct);

    Task<List<AreaReasignacionDto>> ObtenerAreasDestinoAsync(CancellationToken ct);

    Task<List<HistorialAsignacionDto>> ObtenerHistorialAsync(long denunciaId, CancellationToken ct);

    Task AsignarEncargadoAsync(long denunciaId, AsignarEncargadoRequest request, CancellationToken ct);

    Task ReasignarAreaAsync(long denunciaId, ReasignarAreaRequest request, CancellationToken ct);

    Task ReasignarAUvcAsync(long denunciaId, long jefeAreaId, string motivo, string? numeroOficio, CancellationToken ct);

    Task<int> GenerarAlertasVencimientoAsync(long areaId, CancellationToken ct);

    Task ConfirmarAtencionUvcAsync(long denunciaId, ConfirmarAtencionUvcRequest request, CancellationToken ct);

    Task DevolverAreaRectoraAsync(long denunciaId, DevolverAreaRectoraRequest request, CancellationToken ct);
}