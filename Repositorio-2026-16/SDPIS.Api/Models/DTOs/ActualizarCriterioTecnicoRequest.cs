namespace SDPIS.Api.Models.DTOs;

// Entrada para que el superusuario agregue o actualice (upsert) UN criterio
// tecnico individual de una evaluacion (HU-010, correccion de alcance: por
// criterio, no por evaluacion completa). RolFuncionarioSimulado es el rol
// de quien ejecuta la accion.
// TODO: reemplazar por validacion real de rol via MSI cuando este disponible.
//
// Justificacion: obligatoria unicamente cuando se esta editando un valor que
// el funcionario validador ya habia registrado (HU-010, subtarea "panel de
// justificacion"). Si el criterio no tenia valor previo ("Agregar" en vez de
// "Editar"), no aplica -- ver CriteriosTecnicosService.ActualizarCriterioAsync.
public record ActualizarCriterioTecnicoRequest(
    string? RolFuncionarioSimulado,
    long FuncionarioId,
    string ValorSeleccionado,
    string? Justificacion = null
);