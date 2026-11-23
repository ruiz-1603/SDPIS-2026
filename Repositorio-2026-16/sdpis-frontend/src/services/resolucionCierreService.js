import { solicitarApi } from './apiCliente';

// HU-017. Todas las llamadas van con el token de la sesion (solicitarApi):
// el backend toma a la jefatura del token, nunca del body.

// Solo la jefatura del area de la solicitud. La denuncia pasa a FINALIZADA.
// Observaciones opcionales.
export const aprobarCierre = (solicitudId, { observaciones } = {}) =>
    solicitarApi(`/solicitudes-cierre/${solicitudId}/aprobar`, {
        metodo: 'POST',
        cuerpo: { observaciones: observaciones || null },
    });

// Devuelve la resolucion para ajustes: la denuncia vuelve al estado que tenia
// antes de la solicitud. Observaciones obligatorias.
export const rechazarCierre = (solicitudId, { observaciones }) =>
    solicitarApi(`/solicitudes-cierre/${solicitudId}/rechazar`, {
        metodo: 'POST',
        cuerpo: { observaciones },
    });

// Solicitudes de cierre de la denuncia con la decision de la jefatura
// (la mas reciente primero).
export const obtenerHistorialCierre = (denunciaId) =>
    solicitarApi(`/denuncias/${denunciaId}/solicitudes-cierre/historial`);
