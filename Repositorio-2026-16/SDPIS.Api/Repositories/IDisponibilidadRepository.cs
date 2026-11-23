namespace SDPIS.Api.Repositories;

public interface IDisponibilidadRepository
{
    Task<PeriodoIndisponibilidadRegistro> InsertarPeriodoAsync(
        long funcionarioId, string motivo, DateOnly fechaInicio, DateOnly? fechaFinEstimada,
        string? registradoPor, CancellationToken ct);

    Task<List<PeriodoIndisponibilidadRegistro>> ObtenerPeriodosAsync(
        long funcionarioId, CancellationToken ct);

    Task<bool> ExisteFuncionarioAsync(long funcionarioId, CancellationToken ct);

    Task<List<FuncionarioInfo>> ObtenerFuncionariosAsync(long? areaId, CancellationToken ct);
    Task<List<PeriodoIndisponibilidadRegistro>> ObtenerTodosLosPeriodosAsync(CancellationToken ct);

    Task<PeriodoIndisponibilidadRegistro> ActualizarPeriodoAsync(
        long idPeriodo, long funcionarioId, string motivo, DateOnly fechaInicio,
        DateOnly? fechaFinEstimada, string? registradoPor, CancellationToken ct);

    Task EliminarPeriodoAsync(long idPeriodo, long funcionarioId, CancellationToken ct);
}
