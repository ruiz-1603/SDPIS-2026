namespace SDPIS.Api.Models.DTOs;

public record ReasignarAUvcRequest(
    long JefeAreaId,
    string Motivo,
    string? NumeroOficio = null
);