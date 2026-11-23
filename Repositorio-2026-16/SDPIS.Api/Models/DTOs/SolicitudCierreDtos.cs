namespace SDPIS.Api.Models.DTOs;

// HU-018: body del POST /api/denuncias/{denunciaId}/solicitud-cierre. La
// denuncia sale de la URL y el funcionario del token; la fecha de las
// observaciones finales es la fecha de la solicitud (la pone la BD).
public record SolicitarCierreRequest(
    string ResolucionFinal,
    string ObservacionesFinales,
    string AccionesRealizadas
);

// HU-018: una solicitud de cierre. AreaId/AreaNombre = jefatura a la que
// quedo asignada para su evaluacion. Estado: PENDIENTE | APROBADA | RECHAZADA.
public record SolicitudCierreDto(
    long IdSolicitudCierre,
    long DenunciaId,
    string Consecutivo,
    long SolicitadoPorId,
    string SolicitadoPorNombre,
    long AreaId,
    string AreaNombre,
    string ResolucionFinal,
    string ObservacionesFinales,
    string AccionesRealizadas,
    string FechaSolicitud,
    string Estado
);

// HU-018: lo que la bitacora necesita para mostrar (o no) el boton de
// solicitar cierre. Motivo explica por que no se puede, cuando aplica.
// Las carpetas del expediente se muestran en la revision previa al cierre
// (null = sin documentos registrados).
public record EstadoSolicitudCierreDto(
    bool PuedeSolicitar,
    string? Motivo,
    SolicitudCierreDto? SolicitudPendiente,
    string? CarpetaConfidencialUrl = null,
    string? CarpetaNoConfidencialUrl = null
);
