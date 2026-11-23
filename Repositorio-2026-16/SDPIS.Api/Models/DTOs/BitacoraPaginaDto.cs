namespace SDPIS.Api.Models.DTOs;

// HU-016: pagina de la bitacora de una denuncia, en orden cronologico ascendente.
public record BitacoraPaginaDto(
    List<BitacoraEntradaDto> Items,
    int Pagina,
    int TamanoPagina,
    int Total
);
