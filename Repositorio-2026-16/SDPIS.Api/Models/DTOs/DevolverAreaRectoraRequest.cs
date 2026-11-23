namespace SDPIS.Api.Models.DTOs;

// HU-012: devolución de una denuncia desde la UVC al Área Rectora
// por el superusuario (consideración "devolverlo al Área Rectora").
public record DevolverAreaRectoraRequest(
    long AreaRectoraId,
    long SuperusuarioId,
    string? Motivo
);