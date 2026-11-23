import { leerSesionLocal } from './autenticacionService';

// Igual que el resto de servicios: el frontend consume el backend directo.
const URL_API_BASE = 'http://localhost:5290/api';

// Evento global que dispara cualquier 401: SesionContext lo escucha y
// limpia la sesion (App vuelve al sitio publico con el login abierto).
export const EVENTO_SESION_INVALIDA = 'sdpis:sesion-invalida';

export class ErrorApi extends Error {
    constructor(mensaje, estado) {
        super(mensaje);
        this.name = 'ErrorApi';
        this.estado = estado;
    }
}

const MENSAJES_POR_ESTADO = {
    401: 'Su sesión expiró. Inicie sesión nuevamente.',
    403: 'No cuenta con permisos para realizar esta acción.',
    404: 'El recurso solicitado no existe.',
    422: 'La acción no cumple las reglas del sistema.',
};

// Solicitud autenticada (HU-015). Agrega el token de la sesion, arma la
// consulta (omite valores vacios) y traduce los errores del backend:
// 400 { errores: [] } | resto { error } / { mensaje }.
export async function solicitarApi(ruta, { metodo = 'GET', cuerpo, consulta } = {}) {
    const token = leerSesionLocal()?.token;

    const url = new URL(`${URL_API_BASE}${ruta}`);
    Object.entries(consulta ?? {}).forEach(([clave, valor]) => {
        if (valor !== undefined && valor !== null && valor !== '') url.searchParams.set(clave, valor);
    });

    const encabezados = {};
    if (cuerpo !== undefined) encabezados['Content-Type'] = 'application/json';
    if (token) encabezados.Authorization = `Bearer ${token}`;

    let respuesta;
    try {
        respuesta = await fetch(url, {
            method: metodo,
            headers: encabezados,
            body: cuerpo !== undefined ? JSON.stringify(cuerpo) : undefined,
        });
    } catch {
        throw new ErrorApi('No fue posible conectar con el servidor. Intente nuevamente.', 0);
    }

    if (respuesta.status === 204) return null;
    const datos = await respuesta.json().catch(() => null);

    if (!respuesta.ok) {
        if (respuesta.status === 401) window.dispatchEvent(new CustomEvent(EVENTO_SESION_INVALIDA));

        const codigoTecnico = ['token_invalido', 'sesion_expirada'].includes(datos?.error);
        const mensaje = datos?.errores?.join(' ')
            || (codigoTecnico ? null : datos?.error)
            || datos?.mensaje
            || MENSAJES_POR_ESTADO[respuesta.status]
            || 'Ocurrió un error inesperado. Intente nuevamente.';
        throw new ErrorApi(mensaje, respuesta.status);
    }

    return datos;
}
