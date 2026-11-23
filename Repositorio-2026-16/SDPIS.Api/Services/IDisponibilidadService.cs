using SDPIS.Api.Models.DTOs;

namespace SDPIS.Api.Services;

public interface IDisponibilidadService
{
    Task<PeriodoIndisponibilidadDto> RegistrarPeriodoAsync(RegistrarIndisponibilidadRequest request, CancellationToken ct);

    Task<PeriodoIndisponibilidadDto> ActualizarPeriodoAsync(ActualizarIndisponibilidadRequest request, CancellationToken ct);

    Task EliminarPeriodoAsync(long idPeriodo, long funcionarioId, CancellationToken ct);

    Task<List<FuncionarioDisponibilidadResumenDto>> ObtenerResumenFuncionariosAsync(long? areaId, CancellationToken ct);
    Task<DisponibilidadFuncionarioDto> ObtenerDisponibilidadAsync(long funcionarioId, CancellationToken ct);

    // Metodo reutilizable por los servicios de asignacion (HU-009 y HU-012)
    // para impedir asignar denuncias a funcionarios no disponibles.
    Task<bool> EsDisponibleAsync(long funcionarioId, DateOnly fechaCorte, CancellationToken ct);
}
