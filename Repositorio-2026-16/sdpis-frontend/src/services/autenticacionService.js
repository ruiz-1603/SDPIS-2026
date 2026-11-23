// URL_API_AUTENTICACION: el frontend consume http://localhost:5290 directo
// (igual que denunciaService.js); el proxy de Vite no interviene.
const URL_API_AUTENTICACION = 'http://localhost:5290/api/autenticacion';

const CLAVE_SESION = 'sdpis.sesion';

// Forma de la sesion guardada: { token, usuario } donde usuario es el
// UsuarioAutenticadoDto que devuelve el backend (sin clave).
export async function iniciarSesion(codigoUsuario, clave) {
    const respuesta = await fetch(`${URL_API_AUTENTICACION}/iniciar-sesion`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ codigoUsuario, clave }),
    });

    const cuerpo = await respuesta.json().catch(() => null);

    if (!respuesta.ok) {
        const mensaje = cuerpo?.errores?.join(' ') || cuerpo?.error
            || 'No fue posible iniciar sesion. Intente nuevamente.';
        throw new Error(mensaje);
    }

    return cuerpo; // { token, expiraEn, usuario }
}

// Valida que el token guardado siga siendo valido (GET /sesion). El backend
// responde 401 sesion_expirada cuando el token vencio y token_invalido cuando
// no corresponde; en ambos casos hay que limpiar la sesion local.
export async function obtenerSesionActual(token) {
    const respuesta = await fetch(`${URL_API_AUTENTICACION}/sesion`, {
        headers: { Authorization: `Bearer ${token}` },
    });

    if (!respuesta.ok) throw new Error('sesion_invalida');
    return respuesta.json();
}

// Cierra la sesion del lado del servidor (HU-028): revoca el JWT en el
// backend (token_revocado) y luego elimina la sesion local. Si el token ya
// no es valido (expirado o previamente revocado) el backend responde 401 y
// aun asi se limpia lo local: el usuario queda fuera en cualquier caso.
// Nunca lanza: cerrar sesion no debe dejar al usuario atrapado por un error
// de red ni revelar detalles internos del mecanismo de autenticacion.
// motivo: 'manual' (boton) o 'inactividad' (cierre automatico); el backend
// lo registra como CIERRE_MANUAL o CIERRE_INACTIVIDAD.
export async function cerrarSesion(motivo = 'manual') {
    const sesion = leerSesionLocal();
    const token = sesion?.token;

    if (token) {
        try {
            const consulta = motivo === 'inactividad' ? '?motivo=inactividad' : '';
            await fetch(`${URL_API_AUTENTICACION}/logout${consulta}`, {
                method: 'POST',
                headers: { Authorization: `Bearer ${token}` },
            });
        } catch {
            // La sesion local se limpia igual; el token caduca solo si la
            // notificacion no llego.
        }
    }

    sessionStorage.removeItem(CLAVE_SESION);
}

export function guardarSesionLocal(sesion) {
    sessionStorage.setItem(CLAVE_SESION, JSON.stringify(sesion));
}

// { token, usuario } | null
export function leerSesionLocal() {
    const crudo = sessionStorage.getItem(CLAVE_SESION);
    if (!crudo) return null;
    try {
        return JSON.parse(crudo);
    } catch {
        return null;
    }
}