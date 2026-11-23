import { solicitarApi } from './apiCliente';

// HU-018. Todas las llamadas van con el token de la sesion (solicitarApi):
// el backend toma al funcionario del token, nunca del body.

// Si el usuario puede solicitar el cierre de la denuncia, la solicitud
// pendiente (si hay) y las carpetas del expediente.
export const obtenerEstadoSolicitudCierre = (denunciaId) =>
    solicitarApi(`/denuncias/${denunciaId}/solicitud-cierre`);

// Solo el funcionario responsable asignado. La denuncia pasa a EN_REVISION.
export const solicitarCierreDenuncia = (denunciaId, { resolucionFinal, observacionesFinales, accionesRealizadas }) =>
    solicitarApi(`/denuncias/${denunciaId}/solicitud-cierre`, {
        metodo: 'POST',
        cuerpo: { resolucionFinal, observacionesFinales, accionesRealizadas },
    });

// Bandeja de la jefatura: pendientes de su area (superusuario: todas).
export const obtenerSolicitudesCierrePendientes = () => solicitarApi('/solicitudes-cierre/pendientes');
