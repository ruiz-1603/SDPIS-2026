import { solicitarApi } from './apiCliente';
const URL_API_DISPONIBILIDAD = 'http://localhost:5290/api/disponibilidad';


// HU-007 + HU-015: funcionarios con disponibilidad calculada. El backend
// filtra por el login: superusuario -> todas las areas; jefe -> su area.
export const listarFuncionariosDisponibilidad = () => solicitarApi('/disponibilidad/funcionarios');

// HU-007: registrar un periodo de indisponibilidad para un funcionario.
// registradoPor: quien ejecuta el registro (login MSI simulado por ahora);
// se guarda en la tabla para trazabilidad (SRS HU-007).
export async function registrarPeriodoIndisponibilidad({ funcionarioId, motivo, fechaInicio, fechaFinEstimada, registradoPor }) {
  const payload = {
    funcionarioId,
    motivo,
    fechaInicio,
    fechaFinEstimada: fechaFinEstimada || null,
    registradoPor: registradoPor || null,
  };

  const respuesta = await fetch(URL_API_DISPONIBILIDAD, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
  });

  if (!respuesta.ok) {
    const errorBody = await respuesta.json().catch(() => null);
    console.error('Error del backend:', errorBody);
    throw new Error(
      errorBody?.errores?.join(' ') || errorBody?.error || 'No fue posible registrar el periodo de indisponibilidad'
    );
  }

  return respuesta.json();
}

// HU-007: actualizar un periodo de indisponibilidad programado o vigente.
// Se usa para acortar la fecha estimada de fin (el funcionario regresa antes)
// y recalcular automaticamente la disponibilidad. Provoca un UPDATE.
export async function actualizarPeriodoIndisponibilidad({ idPeriodo, funcionarioId, motivo, fechaInicio, fechaFinEstimada, registradoPor }) {
  const payload = {
    idPeriodo,
    funcionarioId,
    motivo,
    fechaInicio,
    fechaFinEstimada: fechaFinEstimada || null,
    registradoPor: registradoPor || null,
  };

  const respuesta = await fetch(`${URL_API_DISPONIBILIDAD}/periodos/${idPeriodo}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
  });

  if (!respuesta.ok) {
    const errorBody = await respuesta.json().catch(() => null);
    console.error('Error del backend:', errorBody);
    throw new Error(
      errorBody?.errores?.join(' ') || errorBody?.error || 'No fue posible actualizar el periodo de indisponibilidad'
    );
  }

  return respuesta.json();
}

// HU-007: eliminar un periodo de indisponibilidad programado o vigente.
// Se usa cuando el periodo se registró por error o el funcionario nunca
// estuvo indisponible. Provoca un DELETE.
export async function eliminarPeriodoIndisponibilidad(idPeriodo, funcionarioId) {
  const respuesta = await fetch(
    `${URL_API_DISPONIBILIDAD}/periodos/${idPeriodo}?funcionarioId=${funcionarioId}`,
    { method: 'DELETE' }
  );

  if (!respuesta.ok) {
    const errorBody = await respuesta.json().catch(() => null);
    console.error('Error del backend:', errorBody);
    throw new Error(
      errorBody?.errores?.join(' ') || errorBody?.error || 'No fue posible eliminar el periodo de indisponibilidad'
    );
  }
}

// HU-007: consultar la disponibilidad calculada de un funcionario.
export async function obtenerDisponibilidadFuncionario(funcionarioId) {
  const respuesta = await fetch(`${URL_API_DISPONIBILIDAD}/funcionarios/${funcionarioId}`);

  if (!respuesta.ok) {
    const errorBody = await respuesta.json().catch(() => null);
    console.error('Error del backend:', errorBody);
    throw new Error(
      errorBody?.errores?.join(' ') || errorBody?.error || 'No fue posible consultar la disponibilidad'
    );
  }

  return respuesta.json();
}