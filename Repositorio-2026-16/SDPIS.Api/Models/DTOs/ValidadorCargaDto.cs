namespace SDPIS.Api.Models.DTOs;

public record ValidadorCargaDto(
    long IdFuncionario,
    string Nombre,
    int CargaActiva
);
