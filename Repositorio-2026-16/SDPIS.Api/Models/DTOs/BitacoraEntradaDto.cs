namespace SDPIS.Api.Models.DTOs;

// HU-016: una entrada de la bitacora de una denuncia. FuncionarioId null
// significa accion automatica del sistema.
public record BitacoraEntradaDto(
    long IdBitacora,
    long DenunciaId,
    long? FuncionarioId,
    string? FuncionarioNombre,
    long? AreaOrigenId,
    string? AreaOrigenNombre,
    string Fecha,
    string TipoRegistro,
    string? Contenido
);
