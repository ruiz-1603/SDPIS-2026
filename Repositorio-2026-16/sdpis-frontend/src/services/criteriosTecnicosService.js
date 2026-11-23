import { solicitarApi } from './apiCliente';

// HU-010/011. HU-015: todas las llamadas van con el token de la sesion
// (solicitarApi). El backend toma el funcionario y el rol del token: ya no se
// envian funcionarioValidadorId / funcionarioId / rolFuncionarioSimulado.
const BASE = '/denuncias';

// Slugs de ruta estables para cada criterio puntuable (deben coincidir con
// el regex de ruta en CriteriosTecnicosController). No son el nombre de
// catalogo -- eso es un detalle interno de la base de datos.
export const CRITERIO_DANO_POTENCIAL_SALUD = 'dano-potencial-salud';
export const CRITERIO_ESPECIALIZADO_UVC = 'criterio-especializado-uvc';

// HU-010: consultar los criterios tecnicos actuales de una denuncia.
// Devuelve null si la denuncia aun no tiene criterios registrados (204).
export const obtenerCriteriosTecnicos = (denunciaId) =>
    solicitarApi(`${BASE}/${denunciaId}/criterios-tecnicos`);

// HU-010: registrar los criterios tecnicos de analisis de una denuncia
// (funcionario asignado a la denuncia o superusuario).
export const registrarCriteriosTecnicos = (denunciaId, datos) =>
    solicitarApi(`${BASE}/${denunciaId}/criterios-tecnicos`, {
        metodo: 'POST',
        cuerpo: {
            vinculadoComercioElectronico: datos.vinculadoComercioElectronico,
            danoPotencialSalud: datos.danoPotencialSalud,
            requiereCriterioEspecializado: datos.requiereCriterioEspecializado,
            observacionesTecnicas: datos.observacionesTecnicas || null,
        },
    });

// HU-010 (correccion de alcance): agregar o actualizar (upsert) UN criterio
// tecnico individual, sin afectar el otro (solo superusuario).
// criterio: CRITERIO_DANO_POTENCIAL_SALUD | CRITERIO_ESPECIALIZADO_UVC.
// justificacion: obligatoria en el backend solo cuando se EDITA un valor que
// el funcionario validador ya habia registrado (null en el "Agregar").
export const actualizarCriterioTecnico = (denunciaId, criterio, { valorSeleccionado, justificacion = null }) =>
    solicitarApi(`${BASE}/${denunciaId}/criterios-tecnicos/${criterio}`, {
        metodo: 'PUT',
        cuerpo: { valorSeleccionado, justificacion },
    });

// HU-010: actualizar la metadata de la evaluacion (comercio electronico/
// observaciones), separada de los criterios individuales porque no
// son "criterios" en el modelo de datos (solo superusuario).
export const actualizarMetadataCriteriosTecnicos = (denunciaId, datos) =>
    solicitarApi(`${BASE}/${denunciaId}/criterios-tecnicos/metadata`, {
        metodo: 'PUT',
        cuerpo: {
            vinculadoComercioElectronico: datos.vinculadoComercioElectronico,
            observacionesTecnicas: datos.observacionesTecnicas || null,
            justificacion: datos.justificacion || null,
        },
    });

// HU-010 (subtarea #116): consultar si algun producto de la denuncia tiene
// una alerta sanitaria vigente, como apoyo para elegir el dano potencial a la salud.
export const obtenerAlertaSanitariaVigente = (denunciaId) =>
    solicitarApi(`${BASE}/${denunciaId}/alerta-sanitaria-vigente`);

// HU-010: informacion registrada de la denuncia (hecho, ubicacion, productos
// y motivos) para que el funcionario la consulte antes de elegir los criterios.
export const obtenerDetalleDenuncia = (denunciaId) =>
    solicitarApi(`${BASE}/${denunciaId}/detalle`);