namespace SDPIS.Api.Models.DTOs;

// HU-012: área candidata para reasignar una denuncia.
public record AreaReasignacionDto(
    long IdArea,
    string Nombre,
    string Tipo
);