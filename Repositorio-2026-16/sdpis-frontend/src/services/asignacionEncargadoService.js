const URL_API_BASE = 'http://localhost:5290/api';

// HU-012: bandeja de denuncias del área para asignar funcionario
// encargado o reasignar la denuncia a otra área (incluye UVC).
export async function obtenerBandejaEncargado(areaId) {
  const respuesta = await fetch(`${URL_API_BASE}/areas/${areaId}/encargados/bandeja`);

  if (!respuesta.ok) {
    const errorBody = await respuesta.json().catch(() => null);
    console.error('Error del backend:', errorBody);
    throw new Error(
      errorBody?.errores?.join(' ') || errorBody?.error || 'No fue  posible cargar la bandeja de asignación de encargados'
    );
  }

  return respuesta.json();
}

// HU-012: funcionarios del área elegibles como encargado
// (activos, disponibles según HU-007 y que no son jefes de área).
export async function obtenerEncargadosDisponibles(areaId) {
  const respuesta = await fetch(`${URL_API_BASE}/areas/${areaId}/encargados/disponibles`);

  if (!respuesta.ok) {
    const errorBody = await respuesta.json().catch(() => null);
    console.error('Error del backend:', errorBody);
    throw new Error(
      errorBody?.errores?.join(' ') || errorBody?.error || 'No fue posible cargar los funcionarios encargados disponibles'
    );
  }

  return respuesta.json();
}

// HU-012: áreas candidatas para la reasignación de una denuncia.
export async function obtenerAreasDestino() {
  const respuesta = await fetch(`${URL_API_BASE}/areas/destino`);

  if (!respuesta.ok) {
    const errorBody = await respuesta.json().catch(() => null);
    console.error('Error del backend:', errorBody);
    throw new Error(
      errorBody?.errores?.join(' ') || errorBody?.error || 'No fue posible cargar las áreas de destino'
    );
  }

  return respuesta.json();
}

// HU-012: asignar el funcionario encargado de una denuncia.
export async function asignarEncargado(areaId, denunciaId, { encargadoId, jefeAreaId, motivo }) {
  const respuesta = await fetch(
    `${URL_API_BASE}/areas/${areaId}/denuncias/${denunciaId}/asignar-encargado`,
    {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ encargadoId, jefeAreaId, motivo: motivo || null }),
    }
  );

  if (!respuesta.ok) {
    const errorBody = await respuesta.json().catch(() => null);
    console.error('Error del backend:', errorBody);
    throw new Error(
      errorBody?.errores?.join(' ') || errorBody?.error || 'No fue posible asignar el funcionario encargado'
    );
  }

  return respuesta.json();
}

// HU-012: reasignar la denuncia a otra área.
export async function reasignarArea(areaId, denunciaId, { areaNuevaId, jefeAreaId, motivo }) {
  const respuesta = await fetch(
    `${URL_API_BASE}/areas/${areaId}/denuncias/${denunciaId}/reasignar-area`,
    {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ areaNuevaId, jefeAreaId, motivo: motivo || null }),
    }
  );

  if (!respuesta.ok) {
    const errorBody = await respuesta.json().catch(() => null);
    console.error('Error del backend:', errorBody);
    throw new Error(
      errorBody?.errores?.join(' ') || errorBody?.error || 'No fue posible reasignar la denuncia de área'
    );
  }

  return respuesta.json();
}

// HU-012, AC #5: reasignación real a UVC (motivo obligatorio; número de oficio opcional).
export async function reasignarAUvc(areaId, denunciaId, { jefeAreaId, motivo, numeroOficio }) {
  const respuesta = await fetch(
    `${URL_API_BASE}/areas/${areaId}/denuncias/${denunciaId}/reasignar-uvc`,
    {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ jefeAreaId, motivo, numeroOficio: numeroOficio || null }),
    }
  );

  if (!respuesta.ok) {
    const errorBody = await respuesta.json().catch(() => null);
    console.error('Error del backend:', errorBody);
    throw new Error(
      errorBody?.errores?.join(' ') || errorBody?.error || 'No fue posible reasignar la denuncia a UVC'
    );
  }

  return respuesta.json();
}

// HU-012: alerta a la jefatura cuando una denuncia lleva +3 días sin encargado.
export async function generarAlertasVencimiento(areaId) {
  const respuesta = await fetch(`${URL_API_BASE}/areas/${areaId}/denuncias/alertas/vencimiento`, {
    method: 'POST',
  });

  if (!respuesta.ok) {
    const errorBody = await respuesta.json().catch(() => null);
    console.error('Error del backend:', errorBody);
    throw new Error(
      errorBody?.errores?.join(' ') || errorBody?.error || 'No fue posible generar las alertas de vencimiento'
    );
  }

  return respuesta.json();
}

// HU-012: confirmación de atención de la denuncia en UVC por el superusuario.
export async function confirmarAtencionUvc(areaId, denunciaId, { superusuarioId, motivo }) {
  const respuesta = await fetch(
    `${URL_API_BASE}/areas/${areaId}/denuncias/${denunciaId}/confirmar-atencion-uvc`,
    {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ superusuarioId, motivo: motivo || null }),
    }
  );

  if (!respuesta.ok) {
    const errorBody = await respuesta.json().catch(() => null);
    console.error('Error del backend:', errorBody);
    throw new Error(
      errorBody?.errores?.join(' ') || errorBody?.error || 'No fue posible confirmar la atención en UVC'
    );
  }

  return respuesta.json();
}

// HU-012: devolución de la denuncia desde UVC al Área Rectora.
export async function devolverAreaRectora(areaId, denunciaId, { areaRectoraId, superusuarioId, motivo }) {
  const respuesta = await fetch(
    `${URL_API_BASE}/areas/${areaId}/denuncias/${denunciaId}/devolver-area-rectora`,
    {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ areaRectoraId, superusuarioId, motivo: motivo || null }),
    }
  );

  if (!respuesta.ok) {
    const errorBody = await respuesta.json().catch(() => null);
    console.error('Error del backend:', errorBody);
    throw new Error(
      errorBody?.errores?.join(' ') || errorBody?.error || 'No fue posible devolver la denuncia al área rectora'
    );
  }

  return respuesta.json();
}

// HU-012: historial de reasignaciones de área/encargado de una denuncia.
export async function obtenerHistorial(denunciaId) {
    const respuesta = await fetch(`${URL_API_BASE}/denuncias/${denunciaId}/historial-asignacion`);

    if (!respuesta.ok) {
        const errorBody = await respuesta.json().catch(() => null);
        console.error('Error del backend:', errorBody);
        throw new Error(
            errorBody?.errores?.join(' ') || errorBody?.error || 'No fue posible cargar el historial de asignación'
        );
    }

    return respuesta.json();
}