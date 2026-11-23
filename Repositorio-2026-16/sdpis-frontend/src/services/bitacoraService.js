import { solicitarApi } from './apiCliente';

// HU-016. HU-015: todas las llamadas van con el token de la sesion
// (solicitarApi). El backend toma al funcionario del token: ya no se envia
// funcionarioId ni en la URL ni en el body.

// Linea de tiempo de una denuncia, en orden cronologico ascendente y
// paginada. 403 si la denuncia esta fuera del alcance del usuario.
export const obtenerBitacora = (denunciaId, { tipo, pagina = 1, tamano = 20 } = {}) =>
    solicitarApi(`/denuncias/${denunciaId}/bitacora`, { consulta: { tipo, pagina, tamano } });

// Comentario manual (se guarda con tipo "comentario").
export const agregarComentarioBitacora = (denunciaId, { contenido }) =>
    solicitarApi(`/denuncias/${denunciaId}/bitacora`, { metodo: 'POST', cuerpo: { contenido } });

// Panel izquierdo: denuncias visibles para el usuario autenticado
// (funcionario: sus asignadas; jefe: las de su area; superusuario: todas),
// en cualquier estado.
export const obtenerDenunciasVisibles = () => solicitarApi('/bitacora/denuncias');