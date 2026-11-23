import { solicitarApi } from './apiCliente';

// HU-015: gestion de roles y permisos. El actor sale siempre del token;
// ningun metodo envia jefeAreaId / funcionarioId del usuario autenticado.
const BASE = '/gestion-usuarios';

// { permisos: string[], alcance: 'TODAS' | 'AREA' | 'PROPIAS' | 'NINGUNA' }
export const obtenerMisPermisos = () => solicitarApi(`${BASE}/mis-permisos`);

// [{ codigo, nivelDelegante, grupo, descripcion }]
export const obtenerPermisosDelegables = () => solicitarApi(`${BASE}/permisos-delegables`);

// areaId solo lo usa el superusuario; rol = 'jefe_area' da la lista de jefes.
export const listarFuncionarios = ({ areaId, rol } = {}) =>
    solicitarApi(`${BASE}/funcionarios`, { consulta: { areaId, rol } });

// { nombre, correo, rol, areaId, especialidad } -> { idFuncionario }
export const crearFuncionario = (datos) =>
    solicitarApi(`${BASE}/funcionarios`, { metodo: 'POST', cuerpo: datos });

// { rol?, activo?, especialidad? } (null = sin cambio; '' quita especialidad)
export const actualizarFuncionario = (id, cambios) =>
    solicitarApi(`${BASE}/funcionarios/${id}`, { metodo: 'PUT', cuerpo: cambios });

// Validador: sus asignadas. Jefe: las por asignar de su area.
export const listarDenunciasFuncionario = (id) =>
    solicitarApi(`${BASE}/funcionarios/${id}/denuncias`);

// accion: 'OTORGAR' | 'REVOCAR'
export const gestionarPermiso = (id, codigo, accion) =>
    solicitarApi(`${BASE}/funcionarios/${id}/permisos`, { metodo: 'POST', cuerpo: { codigo, accion } });

// Areas ADICIONALES (solo superusuario): el funcionario (validador) trabaja en
// varias areas a la vez. Su area base no cambia. accion: 'OTORGAR' | 'REVOCAR'
export const gestionarAreaAdicional = (id, areaId, accion) =>
    solicitarApi(`${BASE}/funcionarios/${id}/areas-adicionales`, { metodo: 'POST', cuerpo: { areaId, accion } });

// Queda como jefe en renuncia de su area actual (no se puede elegir otra).
export const renunciarSuperusuario = () =>
    solicitarApi(`${BASE}/superusuario/renuncia`, { metodo: 'POST' });

export const listarAreas = () => solicitarApi(`${BASE}/areas`);

export const configurarMaxJefes = (areaId, maxJefes) =>
    solicitarApi(`${BASE}/areas/${areaId}/max-jefes`, { metodo: 'PUT', cuerpo: { maxJefes } });

// -> { items, total, pagina, tamano }
// desde / hasta: 'AAAA-MM-DD' (hasta incluye todo ese dia). texto busca en
// funcionario y detalle. Las consultas a listas se excluyen salvo que se pidan.
export const listarBitacoraAdministrativa = ({
    areaId, categoria, desde, hasta, texto, incluirConsultas = false, pagina = 1, tamano = 25,
} = {}) =>
    solicitarApi(`${BASE}/bitacora`, {
        consulta: { areaId, categoria, desde, hasta, texto, incluirConsultas, pagina, tamano },
    });

// Filtro unico de denuncias segun el rol. filtro: TODAS | POR_ASIGNAR | ASIGNADAS
// criterios (opcional): REGISTRADOS (con criterios tecnicos) | PENDIENTES
export const listarDenunciasVisibles = ({ areaId, filtro, criterios } = {}) =>
    solicitarApi('/denuncias-visibles', { consulta: { areaId, filtro, criterios } });

// TEMPORAL - ELIMINAR AL INTEGRAR EL MSI.
// { identificacion, nombre, apellido1, apellido2, clave }
export const crearCredencialTemporal = (id, datos) =>
    solicitarApi(`${BASE}/funcionarios/${id}/credencial-temporal`, { metodo: 'POST', cuerpo: datos });
