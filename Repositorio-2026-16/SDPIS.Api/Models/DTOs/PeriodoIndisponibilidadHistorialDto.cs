namespace SDPIS.Api.Models.DTOs;

// Periodo de indisponibilidad dentro del historial de un funcionario,
// con su estado calculado automaticamente (programado/vigente/finalizado) y
// la trazabilidad del registro (quien y cuando). Si el dato no existe
// (registrado antes de la trazabilidad) viene null.
public record PeriodoIndisponibilidadHistorialDto(
    long IdPeriodo,
    string Motivo,
    string FechaInicio,
    string? FechaFinEstimada,
    string Estado,
    string? RegistradoPor,
    string? FechaRegistro
);