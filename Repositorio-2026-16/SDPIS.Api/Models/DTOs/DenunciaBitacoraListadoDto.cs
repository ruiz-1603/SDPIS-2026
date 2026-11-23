namespace SDPIS.Api.Models.DTOs;

// HU-016: fila del panel de busqueda de la pantalla de Bitacora. A diferencia
// de los listados de HU-009/HU-010 (pendientes-asignacion, asignadas), este
// no excluye denuncias por estado ni por tener ya evaluacion de criticidad:
// el objetivo es que una denuncia nunca desaparezca del buscador principal
// solo porque avanzo de etapa (p. ej. al asignarsele un funcionario
// encargado). El alcance de que denuncias se incluyen lo decide el rol de
// quien consulta (BitacoraService.ListarDenunciasVisiblesAsync).
public record DenunciaBitacoraListadoDto(
    long IdDenuncia,
    string Consecutivo,
    string Estado,
    string FechaCreacion,
    long? FuncionarioAsignadoId,
    string? FuncionarioAsignadoNombre,
    long? AreaId,
    string? AreaNombre,
    // yyyy-MM-dd: la usa el frontend para agrupar por mes de recepcion.
    string? FechaCreacionIso = null
);
