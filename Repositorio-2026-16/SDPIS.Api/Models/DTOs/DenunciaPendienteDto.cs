namespace SDPIS.Api.Models.DTOs;

// HU-009. AreaId / AreaNombre: el superusuario ve denuncias de todas las
// areas y necesita saber de cual es cada una (y cargar sus validadores).
public record DenunciaPendienteDto(
    long IdDenuncia,
    string Consecutivo,
    string FechaRecepcion,
    long? AreaId = null,
    string? AreaNombre = null
);