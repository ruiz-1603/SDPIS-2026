namespace SDPIS.Api.Models.DTOs;

// HU-009. AreaId / AreaNombre: ver DenunciaPendienteDto.
public record DenunciaAsignadaDto(
    long IdDenuncia,
    string Consecutivo,
    string FechaRecepcion,
    long ValidadorAsignadoId,
    string NombreValidador,
    long? AreaId = null,
    string? AreaNombre = null
);