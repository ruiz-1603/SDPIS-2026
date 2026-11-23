// Validaciones del formulario de disponibilidad de funcionarios (HU-007).
// Devuelve un objeto { campo: 'mensaje de error' } — mismo contrato que
// utils/validaciones.js del formulario de denuncia.
export function validarFormularioDisponibilidad({ funcionarioId, motivo, fechaInicio, fechaFinEstimada }) {
  const errores = {};

  if (!funcionarioId) {
    errores.funcionarioId = 'El identificador del funcionario es obligatorio.';
  } else if (!/^\d+$/.test(String(funcionarioId).trim())) {
    errores.funcionarioId = 'El identificador del funcionario debe ser numérico.';
  }

  if (!motivo) {
    errores.motivo = 'Debe seleccionar el motivo de indisponibilidad.';
  }

  if (!fechaInicio) {
    errores.fechaInicio = 'La fecha de inicio es obligatoria.';
  }

  if (fechaFinEstimada && fechaInicio && fechaFinEstimada <= fechaInicio) {
    errores.fechaFinEstimada = 'La fecha estimada de disponibilidad debe ser posterior a la fecha de inicio.';
  }

  return errores;
}