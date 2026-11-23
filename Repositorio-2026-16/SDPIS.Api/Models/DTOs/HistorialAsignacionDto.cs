namespace SDPIS.Api.Models.DTOs;

// HU-012: registro del historial de asignaciones de una denuncia
// (historial_asignacion), para trazabilidad completa.
public record HistorialAsignacionDto(
    long IdHistorial,
    long IdDenuncia,
    long? AreaAnteriorId,
    long AreaNuevaId,
    long? FuncionarioAnteriorId,
    long? FuncionarioNuevoId,
    string Fecha,
    string? Motivo,
    string? NumeroOficio = null
);