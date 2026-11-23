namespace SDPIS.Api.Models.DTOs;

// Entrada para que el superusuario actualice la metadata de la evaluacion
// (comercio electronico/observaciones), separada de los criterios
// individuales porque estos campos no son "criterios" en el modelo de datos:
// viven en evaluacion_criticidad, no en respuesta_criterio.
// TODO: reemplazar por validacion real de rol via MSI cuando este disponible.
public record ActualizarMetadataCriteriosTecnicosRequest(
    string? RolFuncionarioSimulado,
    long FuncionarioId,
    string VinculadoComercioElectronico,
    string? ObservacionesTecnicas,
    string? Justificacion = null
);
