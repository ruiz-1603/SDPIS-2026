import { useEffect, useMemo, useState } from 'react';
import { createPortal } from 'react-dom';
import { AvisoToast } from '../../components/comunes/AvisoToast';
import { ModalConfirmacion } from '../../components/comunes/ModalConfirmacion';
import { BotonOrdenFecha } from '../../components/comunes/BotonOrdenFecha';
import { compararFechas } from '../../utils/ordenarFechas';
import { useAvisoToast } from '../../hooks/useAvisoToast';
import { SelectorBuscable } from '../../components/comunes/SelectorBuscable';
import { useSesion } from '../../context/SesionContext';
import { PERMISOS } from '../../config/permisos';
import {
  listarFuncionariosDisponibilidad,
  obtenerDisponibilidadFuncionario,
  registrarPeriodoIndisponibilidad,
  actualizarPeriodoIndisponibilidad,
  eliminarPeriodoIndisponibilidad,
} from '../../services/disponibilidadService';
import { validarFormularioDisponibilidad } from '../../utils/validacionesDisponibilidad';
import './GestionDisponibilidadFuncionario.css';

const MOTIVOS = [
  { valor: 'vacaciones', etiqueta: 'Vacaciones' },
  { valor: 'incapacidad', etiqueta: 'Incapacidad' },
  { valor: 'ausencia', etiqueta: 'Ausencia' },
];

const ETIQUETA_MOTIVO = {
  vacaciones: 'Vacaciones',
  incapacidad: 'Incapacidad',
  ausencia: 'Ausencia',
};

const ETIQUETA_ESTADO = {
  programado: 'Programado',
  vigente: 'Vigente',
  finalizado: 'Finalizado',
};

const ETIQUETA_ROL = {
  validador: 'Validador',
  jefe_area: 'Jefe de Area',
};

const MESES = ['ene', 'feb', 'mar', 'abr', 'may', 'jun', 'jul', 'ago', 'set', 'oct', 'nov', 'dic'];

function fmtFecha(fecha) {
  if (!fecha) return '—';
  const [anio, mes, dia] = fecha.split('-').map(Number);
  if (!anio || !mes || !dia || fecha.length !== 10) return '—';
  return `${dia}-${MESES[mes - 1]}-${anio}`;
}

const datosIniciales = {
  motivo: '',
  fechaInicio: '',
  fechaFinEstimada: '',
};

// onFuncionarioSeleccionado: el portal reduce el menu lateral cuando el usuario
// elige un funcionario (no en la seleccion automatica inicial).
export function GestionDisponibilidadFuncionario({ usuarioLogueado, onFuncionarioSeleccionado }) {
  const { aviso, mostrarAviso } = useAvisoToast();
  const { tienePermiso } = useSesion();
  const veTodas = tienePermiso(PERMISOS.VER_TODAS_AREAS);
  const [filtroArea, setFiltroArea] = useState('');
  const [funcionarios, setFuncionarios] = useState([]);
  const [seleccionadoId, setSeleccionadoId] = useState(null);
  const [detalle, setDetalle] = useState(null);
  const [ordenFecha, setOrdenFecha] = useState('desc');
  const [cargandoLista, setCargandoLista] = useState(true);
  const [panelAbierto, setPanelAbierto] = useState(false);
  const [periodoEditando, setPeriodoEditando] = useState(null);
  const [datos, setDatos] = useState(datosIniciales);
  const [errores, setErrores] = useState({});
  const [avisoTraslape, setAvisoTraslape] = useState(null);
  const [enviando, setEnviando] = useState(false);
  const [confirmarEliminar, setConfirmarEliminar] = useState(false);
  const [eliminando, setEliminando] = useState(false);

  useEffect(() => {
    let cancelado = false;

    listarFuncionariosDisponibilidad()
      .then((lista) => {
        if (cancelado) return;
        setFuncionarios(lista);
        if (lista.length > 0) {
          setSeleccionadoId(lista[0].funcionarioId);
        }
      })
      .catch((err) => {
        if (!cancelado) mostrarAviso(err.message || 'No fue posible cargar la lista de funcionarios.', 'error');
      })
      .finally(() => {
        if (!cancelado) setCargandoLista(false);
      });

    return () => {
        cancelado = true;
    };
  }, [mostrarAviso]);

  useEffect(() => {
    if (seleccionadoId === null || seleccionadoId === undefined) {
      return;
    }

    let cancelado = false;

    obtenerDisponibilidadFuncionario(seleccionadoId)
      .then((resultado) => {
        if (!cancelado) setDetalle(resultado);
      })
      .catch((err) => {
        if (cancelado) return;
        setDetalle(null);
        mostrarAviso(err.message || 'No fue posible consultar la disponibilidad.', 'error');
      });

    return () => {
      cancelado = true;
    };
  }, [seleccionadoId, mostrarAviso]);

  const cargandoDetalle =
    seleccionadoId !== null &&
    seleccionadoId !== undefined &&
    (!detalle || detalle.funcionarioId !== seleccionadoId);

    // Superusuario: puede filtrar por area. Jefe: el backend ya envia solo su area.
    const opcionesArea = useMemo(
        () => [...new Set(funcionarios.map((f) => f.areaNombre))]
            .sort((a, b) => a.localeCompare(b, 'es'))
            .map((nombre) => ({ valor: nombre, texto: nombre })),
        [funcionarios]
    );

    const funcionariosDelArea = useMemo(
        () => (filtroArea ? funcionarios.filter((f) => f.areaNombre === filtroArea) : funcionarios),
        [funcionarios, filtroArea]
    );

    // Opciones del selector de funcionario (con buscador). Si el seleccionado
    // quedo fuera del area filtrada, se agrega para que el selector lo muestre.
    const opcionesFuncionario = useMemo(() => {
        const opciones = funcionariosDelArea.map((f) => ({
            valor: f.funcionarioId,
            texto: f.nombre,
            detalle: [
                f.rol ? (ETIQUETA_ROL[f.rol] || f.rol) : null,
                f.disponible ? 'Disponible' : (motivoVigenteDe(f) ?? 'No disponible'),
                `Carga ${f.cargaActiva ?? 0}`,
                veTodas && !filtroArea ? f.areaNombre : null,
            ].filter(Boolean).join(' · '),
        }));
        const elegido = funcionarios.find((f) => f.funcionarioId === seleccionadoId);
        if (elegido && !opciones.some((o) => o.valor === seleccionadoId)) {
            opciones.unshift({ valor: elegido.funcionarioId, texto: elegido.nombre });
        }
        return opciones;
    }, [funcionariosDelArea, funcionarios, seleccionadoId, veTodas, filtroArea]);

    const historialOrdenado = useMemo(() => {
        if (!detalle?.historial) return [];
        return [...detalle.historial].sort((a, b) =>
            compararFechas(a.fechaInicio, b.fechaInicio, ordenFecha)
        );
    }, [detalle?.historial, ordenFecha]);

    function elegirFuncionario(valor) {
        const id = Number(valor);
        if (!Number.isInteger(id)) return;
        seleccionar(id);
        onFuncionarioSeleccionado?.();
    }

  function seleccionar(id) {
    if (id === seleccionadoId && detalle?.funcionarioId === id) {
      return;
    }
    setSeleccionadoId(id);
    setDetalle(null);
  }

  function abrirPanel(periodo = null) {
    setPeriodoEditando(periodo);
    setDatos(
      periodo
        ? {
            motivo: periodo.motivo,
            fechaInicio: periodo.fechaInicio,
            fechaFinEstimada: periodo.fechaFinEstimada ?? '',
          }
        : datosIniciales
    );
    setErrores({});
    setAvisoTraslape(null);
    setPanelAbierto(true);
  }

  function cerrarPanel() {
    setPanelAbierto(false);
    setPeriodoEditando(null);
  }

  function checarTraslape(nuevosDatos) {
    if (!nuevosDatos.fechaInicio || !nuevosDatos.fechaFinEstimada || !detalle) {
      setAvisoTraslape(null);
      return;
    }

    const nIni = nuevosDatos.fechaInicio;
    const nFin = nuevosDatos.fechaFinEstimada;
    const encontrado = detalle.historial.find(
      (p) =>
        p.idPeriodo !== periodoEditando?.idPeriodo &&
        nIni <= (p.fechaFinEstimada ?? '9999-12-31') &&
        nFin >= p.fechaInicio
    );

    if (encontrado) {
      setAvisoTraslape(
        `Este periodo se traslapa con uno ya registrado (${fmtFecha(encontrado.fechaInicio)} – ${fmtFecha(encontrado.fechaFinEstimada)}). Puede continuar si es correcto.`
      );
    } else {
      setAvisoTraslape(null);
    }
  }

  function onChange(nuevosDatos) {
    setDatos(nuevosDatos);
    setErrores((actuales) => {
      const campo = Object.keys(nuevosDatos).find(
        (c) => c in actuales && nuevosDatos[c] !== datos[c]
      );
      if (!campo) return actuales;
      const restantes = { ...actuales };
      delete restantes[campo];
      return restantes;
    });
    checarTraslape(nuevosDatos);
  }

  async function manejarRegistro(e) {
    e.preventDefault();
    const erroresFormulario = validarFormularioDisponibilidad({
      funcionarioId: String(seleccionadoId),
      ...datos,
    });
    setErrores(erroresFormulario);

    if (Object.keys(erroresFormulario).length > 0) {
      mostrarAviso('Revise los campos marcados en rojo.', 'error');
      return;
    }

    setEnviando(true);
    try {
      if (periodoEditando) {
        await actualizarPeriodoIndisponibilidad({
          idPeriodo: periodoEditando.idPeriodo,
          funcionarioId: seleccionadoId,
          motivo: datos.motivo,
          fechaInicio: datos.fechaInicio,
          fechaFinEstimada: datos.fechaFinEstimada || null,
          registradoPor: usuarioLogueado || null,
        });
      } else {
        await registrarPeriodoIndisponibilidad({
          funcionarioId: seleccionadoId,
          motivo: datos.motivo,
          fechaInicio: datos.fechaInicio,
          fechaFinEstimada: datos.fechaFinEstimada || null,
          registradoPor: usuarioLogueado || null,
        });
      }

      setPanelAbierto(false);
      setPeriodoEditando(null);
      setDatos(datosIniciales);
      setErrores({});
      setAvisoTraslape(null);

      const resultado = await obtenerDisponibilidadFuncionario(seleccionadoId);
      setDetalle(resultado);

      const lista = await listarFuncionariosDisponibilidad();
      setFuncionarios(lista);

      mostrarAviso(
        `${periodoEditando ? 'Periodo de indisponibilidad actualizado' : `Periodo de indisponibilidad registrado (${ETIQUETA_MOTIVO[datos.motivo] ?? datos.motivo})`} para ${resultado.nombre}.`,
        'exito'
      );
    } catch (err) {
      mostrarAviso(
        err.message || `No fue posible ${periodoEditando ? 'actualizar' : 'registrar'} el periodo de indisponibilidad.`,
        'error'
      );
    } finally {
      setEnviando(false);
    }
  }

  async function manejarEliminacion() {
    if (!periodoEditando) return;

    setEliminando(true);
    try {
      await eliminarPeriodoIndisponibilidad(periodoEditando.idPeriodo, seleccionadoId);

      setConfirmarEliminar(false);
      setPanelAbierto(false);
      setPeriodoEditando(null);
      setDatos(datosIniciales);
      setErrores({});
      setAvisoTraslape(null);

      const resultado = await obtenerDisponibilidadFuncionario(seleccionadoId);
      setDetalle(resultado);

      const lista = await listarFuncionariosDisponibilidad();
      setFuncionarios(lista);

      mostrarAviso(`Periodo de indisponibilidad eliminado para ${resultado.nombre}.`, 'exito');
    } catch (err) {
      setConfirmarEliminar(false);
      mostrarAviso(
        err.message || 'No fue posible eliminar el periodo de indisponibilidad.',
        'error'
      );
    } finally {
      setEliminando(false);
    }
  }

   const etiquetaArea = veTodas ? (filtroArea || 'Todas las áreas') : darArea(funcionarios, detalle);
   const disponiblesAhora = funcionariosDelArea.filter((f) => f.disponible).length;
   const noDisponiblesAhora = funcionariosDelArea.length - disponiblesAhora;
   const conPeriodoProximo = funcionariosDelArea.filter((f) => f.tieneProximo).length;

    return (
        <div className="page-disp">
            <div className="disp-masthead">
                <aside className="disp-nota">
                    <span className="disp-nota__icono" aria-hidden="true">
                        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                            <circle cx="12" cy="12" r="9" /><path d="M12 11v5" /><path d="M12 7.5h.01" />
                        </svg>
                    </span>
                    <p>
                        Registre los periodos de indisponibilidad de los funcionarios del área. Quien tenga un periodo vigente de indisponibilidad, NO podrá recibir nuevas denuncias.
                    </p>
                </aside>
                <div className="disp-masthead__stats">
                    <div className="disp-masthead__stat">
                        <div className="cifra">{cargandoLista ? '—' : funcionariosDelArea.length}</div>
                        <div className="etiqueta">Funcionarios</div>
                    </div>
                    <div className="disp-masthead__stat">
                        <div className="cifra">{cargandoLista ? '—' : disponiblesAhora}</div>
                        <div className="etiqueta">Disponibles ahora</div>
                    </div>
                    <div className="disp-masthead__stat">
                        <div className="cifra">{cargandoLista ? '—' : noDisponiblesAhora}</div>
                        <div className="etiqueta">No disponibles</div>
                    </div>
                    <div className="disp-masthead__stat">
                        <div className="cifra">{cargandoLista ? '—' : conPeriodoProximo}</div>
                        <div className="etiqueta">Con periodo próximo</div>
                    </div>
                </div>
            </div>

            <div className="disp-layout">
                <section className="card disp-selector" aria-label="Seleccionar funcionario">
                    <div className="disp-selector-cabecera">
                        <div>
                            <span className="section-divider-label">{cargandoLista ? 'Disponibilidad' : etiquetaArea || 'Ministerio de Salud'}</span>
                            <h3>{veTodas ? 'Funcionarios' : 'Funcionarios del área'}</h3>
                        </div>
                        <div className="subtitulo">
                            {cargandoLista ? 'Cargando…' : `${funcionariosDelArea.length} funcionario${funcionariosDelArea.length === 1 ? '' : 's'} registrado${funcionariosDelArea.length === 1 ? '' : 's'}`}
                        </div>
                    </div>

                    <div className="disp-selector-fila">
                        {veTodas && (
                            <div className="disp-selector-campo">
                                <span>Área</span>
                                <SelectorBuscable
                                    etiqueta="Área"
                                    valor={filtroArea}
                                    onCambiar={setFiltroArea}
                                    textoTodos="Todas las áreas"
                                    placeholder="Buscar área…"
                                    opciones={opcionesArea}
                                />
                            </div>
                        )}
                        <div className="disp-selector-campo">
                            <span>Funcionario</span>
                            <SelectorBuscable
                                etiqueta="Funcionario"
                                valor={seleccionadoId === null ? '' : String(seleccionadoId)}
                                onCambiar={elegirFuncionario}
                                placeholder="Buscar por nombre…"
                                opciones={opcionesFuncionario}
                                deshabilitado={cargandoLista || opcionesFuncionario.length === 0}
                            />
                        </div>
                    </div>
                </section>

                <section className="disp-detalle" aria-label="Detalle de disponibilidad">
                    <div className="card disp-detalle-card">
                        <div className="card-head disp-detalle-head">
                            <div>
                                <span className="section-divider-label">Detalle del funcionario</span>
                                <h3>{detalle ? detalle.nombre : 'Funcionario'}</h3>
                                <div className="subtitulo">
                                    {detalle ? ((ETIQUETA_ROL[detalle.rol] || detalle.rol) || 'Funcionario del área')
                                        : 'Seleccione un funcionario del área'}
                                    {detalle ? ` · ${detalle.areaNombre}` : ''}
                                </div>
                            </div>
                            <div className="disp-detalle-acciones">
                                {detalle && (
                                    <span className={`badge-estado ${detalle.disponible ? 'en-tiempo' : 'pendiente'}`}>
                                        {detalle.disponible ? 'Disponible' : 'No disponible'}
                                    </span>
                                )}
                                <button type="button" className="btn-primary disp-btn-registrar" onClick={() => abrirPanel()} disabled={!detalle}>
                                    Registrar no disponibilidad
                                </button>
                            </div>
                        </div>

                        <div className="card-body">
                            <div className="disp-historial-titulo" style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap', gap: '0.5rem' }}>
                                <h4>Historial de periodos de indisponibilidad</h4>
                                {detalle && detalle.historial && detalle.historial.length > 0 && (
                                    <BotonOrdenFecha
                                        orden={ordenFecha}
                                        alCambiar={setOrdenFecha}
                                        etiqueta="Fecha inicio"
                                    />
                                )}
                            </div>

                            {cargandoDetalle ? (
                                <div className="disp-vacio">Cargando historial…</div>
                            ) : !detalle ? (
                                <div className="disp-vacio">
                                    {seleccionadoId === null ? 'Seleccione un funcionario para ver su historial.' : 'No fue posible consultar el funcionario.'}
                                </div>
                            ) : detalle.historial.length === 0 ? (
                                <div className="disp-vacio">Este funcionario no tiene periodos de indisponibilidad registrados.</div>
                            ) : (
                                <div className="denuncias-table-wrap disp-tabla-wrap">
                                    <table className="denuncias-table disp-historial-tabla tabla-tarjetas">
                                        <thead>
                                            <tr>
                                                <th>Motivo</th>
                                                <th
                                                    className="th-ordenable"
                                                    onClick={() => setOrdenFecha(o => (o === 'asc' ? 'desc' : 'asc'))}
                                                    title={`Ordenar por fecha de inicio (${ordenFecha === 'asc' ? 'más recientes primero' : 'más antiguas primero'})`}
                                                >
                                                    Fecha de inicio {ordenFecha === 'asc' ? '↑' : '↓'}
                                                </th>
                                                <th>Fecha estimada de fin</th>
                                                <th>Estado</th>
                                                <th>Registrado por</th>
                                                <th className="disp-col-acciones">Acciones</th>
                                            </tr>
                                        </thead>
                                        <tbody>
                                            {historialOrdenado.map((p) => {
                                                const clusterFin = p.estado === 'finalizado' ? ' finalizado' : '';
                                                return (
                                                    <tr key={p.idPeriodo} className={clusterFin}>
                                                        <td data-label="Motivo">{ETIQUETA_MOTIVO[p.motivo] ?? p.motivo}</td>
                                                        <td data-label="Fecha de inicio">{fmtFecha(p.fechaInicio)}</td>
                                                        <td data-label="Fecha estimada de fin">{fmtFecha(p.fechaFinEstimada)}</td>
                                                        <td data-label="Estado">
                                                            <span className={`badge-estado disp-estado-${p.estado}`}>
                                                                {ETIQUETA_ESTADO[p.estado] ?? p.estado}
                                                            </span>
                                                        </td>
                                                        <td data-label="Registrado por">
                                                            <span>{p.registradoPor ?? '—'}</span>
                                                            {p.fechaRegistro && (
                                                                <span className="disp-bitacora">{fmtFecha(p.fechaRegistro)}</span>
                                                            )}
                                                        </td>
                                                        <td data-label="Acciones">
                                                            {p.estado !== 'finalizado' && (
                                                                <button
                                                                    type="button"
                                                                    className="btn-enlace"
                                                                    onClick={() => abrirPanel(p)}
                                                                >
                                                                    Editar
                                                                </button>
                                                            )}
                                                        </td>
                                                    </tr>
                                                );
                                            })}
                                        </tbody>
                                    </table>
                                </div>
                            )}
                        </div>
                    </div>
                </section>

                {createPortal(
                    <>
                        {panelAbierto && (
                            <div className="disp-overlay" onClick={cerrarPanel} aria-hidden="true" />
                        )}

                        <div className={`panel-lateral${panelAbierto ? ' visible' : ''}`} role="dialog" aria-modal="true" aria-label={`${periodoEditando ? 'Editar' : 'Registrar'} periodo de indisponibilidad`}>
                            <div className="panel-lateral-header">
                                <div>
                                    <div className="disp-eyebrow">
                                        {periodoEditando ? 'Editar periodo de indisponibilidad' : 'Registrar periodo de indisponibilidad'}
                                    </div>
                                    <h2>{detalle ? detalle.nombre : '—'}</h2>
                                </div>
                                <button type="button" className="modal-close" onClick={cerrarPanel} aria-label="Cerrar panel">
                                    ✕
                                </button>
                            </div>

                            <form id="disp-form" onSubmit={manejarRegistro} noValidate className="panel-lateral-body">
                                <div className="field">
                                    <label htmlFor="disp-motivo">
                                        Motivo de indisponibilidad<span className="req">*</span>
                                    </label>
                                    <select
                                        id="disp-motivo"
                                        value={datos.motivo}
                                        onChange={(e) => onChange({ ...datos, motivo: e.target.value })}
                                        aria-invalid={!!errores.motivo}
                                        aria-describedby={errores.motivo ? 'disp-motivo-error' : undefined}
                                    >
                                        <option value="">Seleccione un motivo…</option>
                                        {MOTIVOS.map((m) => (
                                            <option key={m.valor} value={m.valor}>
                                                {m.etiqueta}
                                            </option>
                                        ))}
                                    </select>
                                    {errores.motivo && (
                                        <div className="error-msg" id="disp-motivo-error" role="alert">
                                            {errores.motivo}
                                        </div>
                                    )}
                                </div>

                                <div className="field">
                                    <label htmlFor="disp-inicio">
                                        Fecha de inicio de la indisponibilidad<span className="req">*</span>
                                    </label>
                                    <input
                                        id="disp-inicio"
                                        type="date"
                                        value={datos.fechaInicio}
                                        onChange={(e) => onChange({ ...datos, fechaInicio: e.target.value })}
                                        aria-invalid={!!errores.fechaInicio}
                                        aria-describedby={errores.fechaInicio ? 'disp-inicio-error' : undefined}
                                    />
                                    <div className="help">Puede registrar una fecha pasada si la indisponibilidad ya inició.</div>
                                    {errores.fechaInicio && (
                                        <div className="error-msg" id="disp-inicio-error" role="alert">
                                            {errores.fechaInicio}
                                        </div>
                                    )}
                                </div>

                                <div className="field">
                                    <label htmlFor="disp-fin">
                                        Fecha estimada de fin<span className="opt">(opcional)</span>
                                    </label>
                                    <input
                                        id="disp-fin"
                                        type="date"
                                        value={datos.fechaFinEstimada}
                                        onChange={(e) => onChange({ ...datos, fechaFinEstimada: e.target.value })}
                                        aria-invalid={!!errores.fechaFinEstimada}
                                        aria-describedby={errores.fechaFinEstimada ? 'disp-fin-error' : undefined}
                                    />
                                    <div className="help">Es una fecha estimada; si no la indica, la indisponibilidad se considera indefinida hasta nuevo aviso.</div>
                                    {errores.fechaFinEstimada && (
                                        <div className="error-msg" id="disp-fin-error" role="alert">
                                            {errores.fechaFinEstimada}
                                        </div>
                                    )}
                                </div>

                                {avisoTraslape && <div className="aviso-traslape visible">{avisoTraslape}</div>}
                            </form>

                            <div className="panel-lateral-footer">
                                {periodoEditando ? (
                                    <button
                                        type="button"
                                        className="btn-danger"
                                        onClick={() => setConfirmarEliminar(true)}
                                        disabled={enviando || eliminando}
                                    >
                                        {eliminando ? 'Eliminando…' : 'Eliminar'}
                                    </button>
                                ) : (
                                    <button type="button" className="btn-secondary" onClick={cerrarPanel} disabled={enviando}>
                                        Cancelar
                                    </button>
                                )}
                                <button type="submit" className="btn-primary" form="disp-form" disabled={enviando || eliminando}>
                                    {enviando ? 'Guardando…' : periodoEditando ? 'Guardar cambios' : 'Guardar periodo'}
                                </button>
                            </div>
                        </div>

                        <ModalConfirmacion
                            visible={confirmarEliminar}
                            titulo="Eliminar periodo de indisponibilidad"
                            mensaje={`Se eliminará el periodo de ${ETIQUETA_MOTIVO[periodoEditando?.motivo] ?? 'indisponibilidad'} de ${detalle?.nombre ?? 'este funcionario'}. Esta acción no se puede deshacer.`}
                            textoConfirmar="Sí, eliminar"
                            onConfirmar={manejarEliminacion}
                            onCancelar={() => setConfirmarEliminar(false)}
                        />
                    </>,
                    document.body
                )}

                <AvisoToast aviso={aviso} />
            </div>
        </div>
    );
}

function motivoVigenteDe(funcionario) {
  if (funcionario.disponible || !funcionario.motivoVigente) return null;
  return `${ETIQUETA_MOTIVO[funcionario.motivoVigente] ?? funcionario.motivoVigente} hasta ${fmtFecha(funcionario.fechaFinVigente)}`;
}

function darArea(funcionarios, detalle) {
  if (detalle?.areaNombre) return detalle.areaNombre;
  if (funcionarios.length > 0) return funcionarios[0].areaNombre;
  return null;
}
