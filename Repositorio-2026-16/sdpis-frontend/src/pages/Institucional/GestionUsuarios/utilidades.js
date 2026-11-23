import { ROLES_ETIQUETA, PERMISOS_ETIQUETA } from '../../../config/permisos';

// Utilidades de presentacion de HU-015 (sin logica de permisos).

const FORMATO_FECHA = new Intl.DateTimeFormat('es-CR', { day: '2-digit', month: '2-digit', year: 'numeric' });
const FORMATO_FECHA_HORA = new Intl.DateTimeFormat('es-CR', {
    day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit',
});

export function formatearFecha(valor) {
    if (!valor) return '—';
    const fecha = new Date(valor);
    return Number.isNaN(fecha.getTime()) ? '—' : FORMATO_FECHA.format(fecha);
}

export function formatearFechaHora(valor) {
    if (!valor) return '—';
    const fecha = new Date(valor);
    return Number.isNaN(fecha.getTime()) ? '—' : FORMATO_FECHA_HORA.format(fecha);
}

export const etiquetaRol = (rol) => ROLES_ETIQUETA[rol] ?? rol ?? '—';
export const etiquetaPermiso = (codigo) => PERMISOS_ETIQUETA[codigo] ?? codigo;

// "SDPIS-2026-000123" o, si no tiene consecutivo, el id.
export const numeroDenuncia = (d) => d.consecutivo ?? `#${d.idDenuncia}`;

export const normalizarTexto = (texto) =>
    (texto ?? '').normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase();

export const ESTADOS_DENUNCIA_ETIQUETA = {
    REGISTRADA: 'Registrada',
    EN_VALIDACION: 'En validación',
    EN_PROCESO: 'En proceso',
    FINALIZADA: 'Finalizada',
};

export const etiquetaEstadoDenuncia = (estado) =>
    ESTADOS_DENUNCIA_ETIQUETA[estado] ?? (estado ?? '—').replaceAll('_', ' ').toLowerCase();

// ---------- Historial administrativo ----------

const FORMATO_HORA = new Intl.DateTimeFormat('es-CR', { hour: '2-digit', minute: '2-digit' });
const FORMATO_DIA = new Intl.DateTimeFormat('es-CR', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' });

export function formatearHora(valor) {
    const fecha = new Date(valor);
    return Number.isNaN(fecha.getTime()) ? '—' : FORMATO_HORA.format(fecha);
}

// 'AAAA-MM-DD' en hora local (no UTC, para que un evento de la noche no
// aparezca en el dia siguiente).
export function aFechaIso(fecha) {
    const mes = String(fecha.getMonth() + 1).padStart(2, '0');
    const dia = String(fecha.getDate()).padStart(2, '0');
    return `${fecha.getFullYear()}-${mes}-${dia}`;
}

export const claveDia = (valor) => aFechaIso(new Date(valor));

// "Hoy", "Ayer" o "lunes, 28 de septiembre de 2026".
export function etiquetaDia(valor) {
    const fecha = new Date(valor);
    const hoy = new Date();
    const ayer = new Date(hoy);
    ayer.setDate(hoy.getDate() - 1);
    const clave = aFechaIso(fecha);
    const texto = FORMATO_DIA.format(fecha);
    if (clave === aFechaIso(hoy)) return `Hoy · ${texto}`;
    if (clave === aFechaIso(ayer)) return `Ayer · ${texto}`;
    return texto.charAt(0).toUpperCase() + texto.slice(1);
}

// Cambia codigos internos (permisos y roles) por su nombre en pantalla.
// Sirve tambien para registros viejos que guardaron el codigo.
const CODIGOS_LEGIBLES = { ...PERMISOS_ETIQUETA, ...ROLES_ETIQUETA };
const PATRON_CODIGOS = new RegExp(`\\b(${Object.keys(CODIGOS_LEGIBLES).join('|')})\\b`, 'g');

export const humanizarDetalle = (texto) =>
    (texto ?? '').replace(PATRON_CODIGOS, (codigo) =>
        (PERMISOS_ETIQUETA[codigo] ? `«${PERMISOS_ETIQUETA[codigo]}»` : CODIGOS_LEGIBLES[codigo].toLowerCase()));

// Valores "rol=validador; activo=S; ..." -> [{ etiqueta, antes?, despues }]
// Solo devuelve lo que cambio (o todo, si es un alta sin valor anterior).
const CAMPOS = {
    nombre: { etiqueta: 'Nombre' },
    correo: { etiqueta: 'Correo' },
    rol: { etiqueta: 'Rol', valor: (v) => ROLES_ETIQUETA[v] ?? v },
    activo: { etiqueta: 'Estado', valor: (v) => ({ S: 'Activo', N: 'Inactivo' }[v] ?? v) },
    especialidad: { etiqueta: 'Especialidad', valor: (v) => (v === '-' ? 'Sin especialidad' : v) },
    max_jefes: { etiqueta: 'Cupo de jefes' },
    plazo_dias: { etiqueta: 'Plazo de atención', valor: (v) => `${v} días` },
    area_id: null, // el area ya se muestra en su columna
};

function leerValores(texto) {
    const mapa = new Map();
    (texto ?? '').split(/;\s*/).forEach((par) => {
        const i = par.indexOf('=');
        if (i > 0) mapa.set(par.slice(0, i).trim(), par.slice(i + 1).trim());
    });
    return mapa;
}

function presentarCampo(clave, valor) {
    if (PERMISOS_ETIQUETA[clave]) {
        return { etiqueta: `Permiso «${PERMISOS_ETIQUETA[clave]}»`, valor: valor === 'SI' ? 'Otorgado' : 'No otorgado' };
    }
    const campo = CAMPOS[clave];
    if (campo === null) return null;
    if (!campo) return { etiqueta: clave, valor };
    return { etiqueta: campo.etiqueta, valor: campo.valor ? campo.valor(valor) : valor };
}

export function describirCambios(anterior, nuevo) {
    const antes = leerValores(anterior);
    const despues = leerValores(nuevo);
    const esAlta = antes.size === 0;
    const cambios = [];
    despues.forEach((valorNuevo, clave) => {
        const valorAnterior = antes.get(clave);
        if (!esAlta && valorAnterior === valorNuevo) return;
        const presentado = presentarCampo(clave, valorNuevo);
        if (!presentado) return;
        cambios.push({
            etiqueta: presentado.etiqueta,
            antes: valorAnterior === undefined ? undefined : presentarCampo(clave, valorAnterior).valor,
            despues: presentado.valor,
        });
    });
    return cambios;
}

// El total de "areas" incluye la UVC, que en la practica no se llama area:
// se detalla como "82 ARS y UVC".
export function detalleAreas(areas) {
    const uvc = areas.filter((a) => a.tipo === 'UVC').length;
    return uvc > 0 ? `${areas.length - uvc} ARS y UVC` : null;
}
