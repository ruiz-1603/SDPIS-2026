namespace SDPIS.Api.Models.DTOs;

// PuntajeTotal ya viene recalculado (suma de lo que quede en
// respuesta_criterio) para que el frontend lo muestre de inmediato, sin
// necesidad de una consulta GET adicional (HU-010, correccion de alcance).
public record CriterioTecnicoActualizadoResponse(
    long IdEvaluacion,
    string Criterio,
    string ValorSeleccionado,
    decimal PuntajeTotal
);
