namespace SDPIS.Api.Models.DTOs;

// Periodo de indisponibilidad registrado para un funcionario.
public record PeriodoIndisponibilidadDto(
    long IdPeriodo,
    long FuncionarioId,
    string Motivo,
    string FechaInicio,
    string? FechaFinEstimada,
    bool Vigente
);
