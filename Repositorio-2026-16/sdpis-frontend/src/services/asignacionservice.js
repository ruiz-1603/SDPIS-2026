import { solicitarApi } from './apiCliente';

// HU-009 + HU-015: asignacion de validador. Todas las llamadas van con el
// token (solicitarApi): el backend toma del login el area y quien asigna.
//   superusuario -> todas las areas (areaId opcional para filtrar);
//   jefe/delegado -> solo su area.
const BASE = '/asignaciones';

export const obtenerDenunciasPendientes = (areaId) =>
    solicitarApi(`${BASE}/denuncias/pendientes`, { consulta: { areaId } });

export const obtenerDenunciasAsignadas = (areaId) =>
    solicitarApi(`${BASE}/denuncias/asignadas`, { consulta: { areaId } });

// Validadores disponibles (activos, sin indisponibilidad vigente) del area
// indicada, con su carga activa. El jefe puede omitir el area.
export const obtenerValidadoresDisponibles = (areaId) =>
    solicitarApi(`${BASE}/validadores`, { consulta: { areaId } });

export const asignarValidador = (denunciaId, validadorId) =>
    solicitarApi(`${BASE}/denuncias/${denunciaId}/asignar`, {
        metodo: 'POST',
        cuerpo: { validadorId: Number(validadorId) },
    });