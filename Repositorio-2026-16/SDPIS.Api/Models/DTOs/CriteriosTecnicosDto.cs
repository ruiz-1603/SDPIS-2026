namespace SDPIS.Api.Models.DTOs;

// Estado actual de los criterios tecnicos de una denuncia (HU-010), para que
// el frontend pueda mostrar el formulario en modo edicion y prellenar datos.
// NivelCriticidad y FechaLimite son el resultado de HU-011 (#62): pueden
// venir null si la evaluacion esta incompleta (falta algun criterio) o aun
// no tiene criticidad calculada.
public record CriteriosTecnicosDto(
    long IdEvaluacion,
    long DenunciaId,
    long FuncionarioValidadorId,
    DateTime FechaEvaluacion,
    decimal? PuntajeTotal,
    string VinculadoComercioElectronico,
    string? DanoPotencialSalud,
    string? RequiereCriterioEspecializado,
    string? ObservacionesTecnicas,
    string? NivelCriticidad,
    DateTime? FechaLimite
);
