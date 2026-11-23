namespace SDPIS.Api.Models.DTOs;

// HU-012: confirmación de atención de una denuncia en la UVC
// por el superusuario (consideración "super usuarios en UVC").
public record ConfirmarAtencionUvcRequest(
    long SuperusuarioId,
    string? Motivo
);