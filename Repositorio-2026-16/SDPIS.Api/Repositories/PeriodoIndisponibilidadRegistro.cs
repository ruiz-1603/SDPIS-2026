namespace SDPIS.Api.Repositories;

// Periodo de indisponibilidad tal como lo devuelve el acceso a datos.
public record PeriodoIndisponibilidadRegistro(
    long IdPeriodo,
    long FuncionarioId,
    string Motivo,
    DateOnly FechaInicio,
    DateOnly? FechaFinEstimada,
    string? RegistradoPor,
    DateOnly? FechaRegistro
);