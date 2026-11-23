namespace SDPIS.Api.Models.DTOs;

// NivelCriticidad es el resultado de HU-011 (#62): se calcula
// automaticamente al registrar los criterios tecnicos.
public record CriteriosTecnicosRegistradosResponse(
    long IdEvaluacion,
    decimal PuntajeTotal,
    string NivelCriticidad
);
