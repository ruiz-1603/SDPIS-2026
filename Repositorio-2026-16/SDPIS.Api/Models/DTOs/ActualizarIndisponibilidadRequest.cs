namespace SDPIS.Api.Models.DTOs;

// Entrada para actualizar (editar) un periodo de indisponibilidad (HU-007).
// Mismos campos que el registro, más el IdPeriodo que identifica el periodo
// a modificar. Al editar la fecha_fin_estimada se recalcula automáticamente
// la disponibilidad del funcionario (ej. acortar el fin lo deja disponible).
public record ActualizarIndisponibilidadRequest(
    long IdPeriodo,
    long FuncionarioId,
    string Motivo,
    string FechaInicio,
    string? FechaFinEstimada,
    string? RegistradoPor
);