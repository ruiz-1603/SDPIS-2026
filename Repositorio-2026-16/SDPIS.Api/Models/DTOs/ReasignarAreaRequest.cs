namespace SDPIS.Api.Models.DTOs;

public record ReasignarAreaRequest(
    long AreaNuevaId,
    long JefeAreaId,
    string? Motivo
);