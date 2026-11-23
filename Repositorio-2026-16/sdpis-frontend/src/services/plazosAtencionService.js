import { solicitarApi } from './apiCliente';

// HU-014: plazos de atencion por nivel de criticidad. Solo el superusuario
// (GESTIONAR_PLAZOS); el actor sale siempre del token.
const BASE = '/plazos-atencion';

// -> [{ nivel: 'ALTO' | 'MEDIO' | 'BAJO', puntajeMin, puntajeMax, plazoDias }]
export const listarPlazos = () => solicitarApi(BASE);

// Cambia los dias de un nivel. Aplica a las evaluaciones de criticidad
// futuras; las fechas limite ya calculadas no se modifican.
export const actualizarPlazo = (nivel, plazoDias) =>
    solicitarApi(`${BASE}/${nivel}`, { metodo: 'PUT', cuerpo: { plazoDias } });
