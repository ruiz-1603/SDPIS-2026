namespace SDPIS.Api.Models.DTOs;

// Entrada para registrar un periodo de indisponibilidad (HU-007).
// Fechas en formato ISO 'yyyy-MM-dd' (solo dia, sin zona horaria).
// RegistradoPor es quien ejecuta el registro (login MSI); hoy viene del
// usuario simulado del frontend y queda guardado para trazabilidad.
public record RegistrarIndisponibilidadRequest(
    long FuncionarioId,
    string Motivo,
    string FechaInicio,
    string? FechaFinEstimada,
    string? RegistradoPor
);
