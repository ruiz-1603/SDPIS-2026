namespace SDPIS.Api.Models.DTOs;

public record AsignarEncargadoRequest(
    long EncargadoId,
    long JefeAreaId,
    string? Motivo
);