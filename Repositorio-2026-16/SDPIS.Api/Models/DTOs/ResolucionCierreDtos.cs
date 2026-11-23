namespace SDPIS.Api.Models.DTOs;

// HU-017: body (opcional) de POST /api/solicitudes-cierre/{id}/aprobar y
// /rechazar. Observaciones de la jefatura: obligatorias al rechazar,
// opcionales al aprobar. El jefe sale del token y la fecha la pone la BD.
public record ResolverCierreRequest(string? Observaciones);

// HU-017: decision de la jefatura sobre una solicitud de cierre (usuario,
// fecha y hora). Decision: APROBADA | RECHAZADA.
public record DecisionCierreDto(
    long IdDecisionCierre,
    string Decision,
    long DecididoPorId,
    string DecididoPorNombre,
    string? ObservacionesJefatura,
    string FechaDecision
);

// HU-017: respuesta de aprobar/rechazar. EstadoDenuncia es el estado al que
// paso la denuncia automaticamente (FINALIZADA o el previo a la revision).
public record ResolucionCierreDto(
    long IdSolicitudCierre,
    long DenunciaId,
    string EstadoDenuncia,
    DecisionCierreDto Decision
);

// HU-017: una entrada del historial de cierre de la denuncia. Reutiliza la
// solicitud de HU-018; Decision es null mientras este PENDIENTE.
// EstadoPrevio: estado en curso antes de EN_REVISION (null en solicitudes
// anteriores a HU-017).
public record HistorialSolicitudCierreDto(
    SolicitudCierreDto Solicitud,
    string? EstadoPrevio,
    DecisionCierreDto? Decision
);
