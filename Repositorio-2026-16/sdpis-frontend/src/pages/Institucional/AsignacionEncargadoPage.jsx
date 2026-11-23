import { useState, useEffect, useMemo } from 'react';
import { AvisoToast } from '../../components/comunes/AvisoToast';
import { SelectorBuscable } from '../../components/comunes/SelectorBuscable';
import { BotonOrdenFecha } from '../../components/comunes/BotonOrdenFecha';
import { compararFechas } from '../../utils/ordenarFechas';
import { useAvisoToast } from '../../hooks/useAvisoToast';
import {
    obtenerBandejaEncargado,
    obtenerEncargadosDisponibles,
    obtenerAreasDestino,
    asignarEncargado,
    reasignarArea,
    reasignarAUvc,
    generarAlertasVencimiento,
    confirmarAtencionUvc,
    devolverAreaRectora,
    obtenerHistorial,
} from '../../services/asignacionEncargadoService';
import { useSesion } from '../../context/SesionContext';
import { PERMISOS } from '../../config/permisos';
import './AsignacionEncargadoPage.css';

const LONGITUD_MAXIMA_OFICIO = 50;

export function AsignacionEncargadoPage() {
    const { aviso, mostrarAviso } = useAvisoToast();
    const { sesion, tienePermiso } = useSesion();

    // El jefe solo trabaja la bandeja de su propia area; solo quien tiene
    // VER_TODAS_AREAS (superusuario) puede cambiar de area.
    const miAreaId = Number(sesion?.usuario?.areaId);
    const veTodasAreas = tienePermiso(PERMISOS.VER_TODAS_AREAS);

    const [areaSeleccionada, setAreaSeleccionada] = useState(miAreaId);
    const [areasDestino, setAreasDestino] = useState([]);
    const areaUvc = areasDestino.find((a) => a.tipo === 'UVC') || null;
    // Quien ejecuta cada accion es el usuario autenticado; el backend vuelve
    // a validar su rol y permisos.
    const emisorActual = Number(sesion?.usuario?.funcionarioId);
    const enUvc = Boolean(areaUvc) && Number(areaSeleccionada) === areaUvc.idArea;
    // Confirmar o devolver desde la UVC: superusuario o jefatura con el permiso.
    const puedeGestionarUvc = tienePermiso(PERMISOS.CONFIRMAR_UVC) || tienePermiso(PERMISOS.ASIGNAR_DENUNCIAS);

    const [denuncias, setDenuncias] = useState([]);
    const [encargados, setEncargados] = useState([]);
    const [cargando, setCargando] = useState(true);
    const [busqueda, setBusqueda] = useState('');
    const [ordenFecha, setOrdenFecha] = useState('asc');
    const [seleccion, setSeleccion] = useState({});

    // Selector de encargado (modal con busqueda, escala a n funcionarios).
    const [denunciaSelector, setDenunciaSelector] = useState(null);
    const [busquedaEncargado, setBusquedaEncargado] = useState('');
    const [ordenEncargado, setOrdenEncargado] = useState('carga');

    // Confirmación de asignación de encargado.
    const [denunciaAConfirmar, setDenunciaAConfirmar] = useState(null);
    const [motivoAsignacion, setMotivoAsignacion] = useState('');
    const [enviandoAsignacion, setEnviandoAsignacion] = useState(false);

    // Reasignación a UVC (justificación obligatoria; número de oficio opcional).
    const [denunciaUvc, setDenunciaUvc] = useState(null);
    const [motivoUvc, setMotivoUvc] = useState('');
    const [oficioUvc, setOficioUvc] = useState('');
    const [enviandoUvc, setEnviandoUvc] = useState(false);

    // Reasignación a otra área.
    const [denunciaArea, setDenunciaArea] = useState(null);
    const [areaNuevaId, setAreaNuevaId] = useState('');
    const [motivoArea, setMotivoArea] = useState('');
    const [enviandoArea, setEnviandoArea] = useState(false);

    // Confirmación de atención en UVC (superusuario).
    const [denunciaConfirmar, setDenunciaConfirmar] = useState(null);
    const [motivoConfirmar, setMotivoConfirmar] = useState('');
    const [enviandoConfirmar, setEnviandoConfirmar] = useState(false);

    // Devolución de la denuncia al Área Rectora (superusuario).
    const [denunciaDevolver, setDenunciaDevolver] = useState(null);
    const [areaRectoraId, setAreaRectoraId] = useState('');
    const [motivoDevolver, setMotivoDevolver] = useState('');
    const [enviandoDevolver, setEnviandoDevolver] = useState(false);

    // Generación de alertas de vencimiento de asignación (3 días).
    const [generandoAlertas, setGenerandoAlertas] = useState(false);
    const [confirmarAlertas, setConfirmarAlertas] = useState(false);
    const [denunciaHistorial, setDenunciaHistorial] = useState(null);
    const [historial, setHistorial] = useState([]);
    const [cargandoHistorial, setCargandoHistorial] = useState(false);

    async function cargarDatos() {
        if (!areaSeleccionada) return;
        try {
            const [resBandeja, resEncargados, resAreas] = await Promise.all([
                obtenerBandejaEncargado(areaSeleccionada),
                obtenerEncargadosDisponibles(areaSeleccionada),
                obtenerAreasDestino(),
            ]);
            setDenuncias(resBandeja);
            setEncargados(resEncargados);
            setAreasDestino(resAreas);

            const nuevaSeleccion = {};
            resBandeja.forEach((d) => {
                nuevaSeleccion[d.idDenuncia] = '';
            });
            setSeleccion(nuevaSeleccion);
        } catch (error) {
            console.error('Error cargando datos:', error);
            mostrarAviso(error.message || 'No fue posible cargar la bandeja de asignación de encargados.', 'error');
        } finally {
            setCargando(false);
        }
    }

    useEffect(() => {
        // eslint-disable-next-line react-hooks/set-state-in-effect
        cargarDatos();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [areaSeleccionada]);

    async function cambiarArea(areaId) {
        if (!areaId || areaId === areaSeleccionada) return;
        setBusqueda('');
        setCargando(true);
        setAreaSeleccionada(areaId);
    }

    function handleSeleccion(denunciaId, encargadoId) {
        setSeleccion((prev) => ({ ...prev, [denunciaId]: encargadoId }));
    }

    function abrirSelectorEncargado(denuncia) {
        setDenunciaSelector(denuncia);
        setBusquedaEncargado('');
        setOrdenEncargado('carga');
    }

    function cerrarSelectorEncargado() {
        setDenunciaSelector(null);
    }

    function elegirEncargado(encargadoId) {
        if (!denunciaSelector) return;
        handleSeleccion(denunciaSelector.idDenuncia, String(encargadoId));
        cerrarSelectorEncargado();
    }

    function encargadoPorId(id) {
        return encargados.find((e) => String(e.idFuncionario) === String(id)) || null;
    }

    const encargadosFiltrados = encargados
        .filter((e) => {
            const q = busquedaEncargado.trim().toLowerCase();
            if (!q) return true;
            return `${e.nombre} ${e.especialidad || ''} ${e.rol || ''}`.toLowerCase().includes(q);
        })
        .sort((a, b) =>
            ordenEncargado === 'nombre'
                ? a.nombre.localeCompare(b.nombre, 'es')
                : (a.cargaActiva - b.cargaActiva) || a.nombre.localeCompare(b.nombre, 'es')
        );

    function iniciarAsignacion(denuncia) {
        const encargadoId = seleccion[denuncia.idDenuncia];
        if (!encargadoId) {
            mostrarAviso('Seleccione un funcionario encargado antes de confirmar.', 'aviso');
            return;
        }
        setDenunciaAConfirmar({ denunciaId: denuncia.idDenuncia, consecutivo: denuncia.consecutivo, encargadoId });
        setMotivoAsignacion('');
    }

    async function confirmarAsignacion() {
        if (!denunciaAConfirmar) return;
        setEnviandoAsignacion(true);
        try {
            await asignarEncargado(areaSeleccionada, denunciaAConfirmar.denunciaId, {
                encargadoId: parseInt(denunciaAConfirmar.encargadoId, 10),
                jefeAreaId: emisorActual,
                motivo: motivoAsignacion,
            });
            setDenunciaAConfirmar(null);
            mostrarAviso('Denuncia asignada al funcionario encargado correctamente.', 'exito');
            await cargarDatos();
        } catch (error) {
            console.error('Error asignando encargado:', error);
            mostrarAviso(error.message || 'No fue posible asignar el funcionario encargado.', 'error');
        } finally {
            setEnviandoAsignacion(false);
        }
    }

    async function confirmarReasignacionUvc() {
        if (!denunciaUvc) return;
        if (!motivoUvc.trim()) {
            mostrarAviso('Debe indicar la justificación de la reasignación a UVC.', 'aviso');
            return;
        }
        setEnviandoUvc(true);
        try {
            await reasignarAUvc(areaSeleccionada, denunciaUvc.idDenuncia, {
                jefeAreaId: emisorActual,
                motivo: motivoUvc,
                numeroOficio: oficioUvc.trim(),
            });
            setDenunciaUvc(null);
            mostrarAviso('Denuncia reasignada a UVC correctamente.', 'exito');
            await cargarDatos();
        } catch (error) {
            console.error('Error reasignando a UVC:', error);
            mostrarAviso(error.message || 'No fue posible reasignar la denuncia a UVC.', 'error');
        } finally {
            setEnviandoUvc(false);
        }
    }

    async function confirmarReasignacionArea() {
        if (!denunciaArea) return;
        if (!areaNuevaId) {
            mostrarAviso('Seleccione el área de destino.', 'aviso');
            return;
        }
        setEnviandoArea(true);
        try {
            await reasignarArea(areaSeleccionada, denunciaArea.idDenuncia, {
                areaNuevaId: parseInt(areaNuevaId, 10),
                jefeAreaId: emisorActual,
                motivo: motivoArea,
            });
            setDenunciaArea(null);
            setAreaNuevaId('');
            mostrarAviso('Denuncia reasignada de área correctamente.', 'exito');
            await cargarDatos();
        } catch (error) {
            console.error('Error reasignando área:', error);
            mostrarAviso(error.message || 'No fue posible reasignar la denuncia de área.', 'error');
        } finally {
            setEnviandoArea(false);
        }
    }

    // Opciones de los selectores con busqueda de los modales de reasignar y devolver.
    const opcionesAreaDestino = useMemo(
        () => areasDestino
            .filter((a) => a.idArea !== denunciaArea?.areaActualId)
            .map((a) => ({ valor: String(a.idArea), texto: a.nombre, detalle: a.tipo !== 'ARS' ? a.tipo : undefined })),
        [areasDestino, denunciaArea],
    );
    const opcionesAreaRectora = useMemo(
        () => areasDestino
            .filter((a) => a.idArea !== areaUvc?.idArea)
            .map((a) => ({ valor: String(a.idArea), texto: a.nombre, detalle: a.tipo !== 'ARS' ? a.tipo : undefined })),
        [areasDestino, areaUvc],
    );

    const denunciasFiltradas = useMemo(() => {
        const filtradas = denuncias.filter((d) =>
            d.consecutivo.toLowerCase().includes(busqueda.toLowerCase())
        );
        return filtradas.sort((a, b) => {
            const cmp = compararFechas(a.fechaRecepcion, b.fechaRecepcion, ordenFecha);
            if (cmp !== 0) return cmp;
            return a.consecutivo.localeCompare(b.consecutivo, 'es');
        });
    }, [denuncias, busqueda, ordenFecha]);

    async function confirmarAtencion() {
        if (!denunciaConfirmar) return;
        setEnviandoConfirmar(true);
        try {
            await confirmarAtencionUvc(areaSeleccionada, denunciaConfirmar.idDenuncia, {
                superusuarioId: emisorActual,
                motivo: motivoConfirmar,
            });
            setDenunciaConfirmar(null);
            mostrarAviso('Denuncia confirmada como atendida en la UVC.', 'exito');
            await cargarDatos();
        } catch (error) {
            console.error('Error confirmando atención UVC:', error);
            mostrarAviso(error.message || 'No fue posible confirmar la atención en UVC.', 'error');
        } finally {
            setEnviandoConfirmar(false);
        }
    }

    async function confirmarDevolucion() {
        if (!denunciaDevolver) return;
        if (!areaRectoraId) {
            mostrarAviso('Seleccione el área rectora de destino.', 'aviso');
            return;
        }
        if (!motivoDevolver.trim()) {
            mostrarAviso('Debe indicar la justificación de la devolución.', 'aviso');
            return;
        }
        setEnviandoDevolver(true);
        try {
            await devolverAreaRectora(areaSeleccionada, denunciaDevolver.idDenuncia, {
                areaRectoraId: parseInt(areaRectoraId, 10),
                superusuarioId: emisorActual,
                motivo: motivoDevolver,
            });
            setDenunciaDevolver(null);
            setAreaRectoraId('');
            mostrarAviso('Denuncia devuelta al área rectora correctamente.', 'exito');
            await cargarDatos();
        } catch (error) {
            console.error('Error devolviendo al área rectora:', error);
            mostrarAviso(error.message || 'No fue posible devolver la denuncia al área rectora.', 'error');
        } finally {
            setEnviandoDevolver(false);
        }
    }

    async function generarAlertas() {
        setGenerandoAlertas(true);
        try {
            const res = await generarAlertasVencimiento(areaSeleccionada);
            mostrarAviso(res.mensaje || 'Alertas de vencimiento generadas.', 'exito');
            await cargarDatos();
        } catch (error) {
            console.error('Error generando alertas:', error);
            mostrarAviso(error.message || 'No fue posible generar las alertas de vencimiento.', 'error');
        } finally {
            setGenerandoAlertas(false);
        }
    }

    async function verHistorial(denuncia) {
        setDenunciaHistorial(denuncia);
        setCargandoHistorial(true);
        try {
            setHistorial(await obtenerHistorial(denuncia.idDenuncia));
        } catch (error) {
            mostrarAviso(error.message || 'No fue posible cargar el historial.', 'error');
            setDenunciaHistorial(null);
        } finally {
            setCargandoHistorial(false);
        }
    }

    const nombreMiArea =
        areasDestino.find((a) => a.idArea === miAreaId)?.nombre || `Mi área (${miAreaId})`;

    // Mi area primero; el resto (solo quien ve todas las areas) por el orden del servicio.
    const opcionesBandeja = [
        { valor: miAreaId, texto: nombreMiArea },
        ...(veTodasAreas
            ? areasDestino
                .filter((a) => a.idArea !== miAreaId)
                .map((a) => ({ valor: a.idArea, texto: `${a.nombre}${a.tipo !== 'ARS' ? ` (${a.tipo})` : ''}` }))
            : []),
    ];

    return (
        <div className="page-encargado">
            <div className="masthead">
                <aside className="nota-info">
                    <span className="nota-info__icono" aria-hidden="true">
                        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                            <circle cx="12" cy="12" r="9" /><path d="M12 11v5" /><path d="M12 7.5h.01" />
                        </svg>
                    </span>
                    <p>
                        Asigne o reasigne el encargado de cada denuncia. Las de comercio electrónico deben reasignarse a UVC antes de quedar en el área.
                    </p>
                </aside>
                <div className="masthead__stats">
                    <div className="masthead__stat">
                        <div className="cifra">{denuncias.length}</div>
                        <div className="etiqueta">En bandeja</div>
                    </div>
                    <div className="masthead__stat">
                        <div className="cifra">{encargados.length}</div>
                        <div className="etiqueta">Encargados disponibles</div>
                    </div>
                    <div className="masthead__stat">
                        <div className="cifra">{denuncias.filter((d) => d.pendienteReasignacionUvc).length}</div>
                        <div className="etiqueta">Pendientes UVC</div>
                    </div>
                </div>
            </div>

            <div className="toolbar">
                <div className="ambito">
                    <span className="ambito__label">Bandeja</span>
                    <div className="ambito__selector">
                        <SelectorBuscable
                            etiqueta="Bandeja"
                            titulo="Seleccionar bandeja"
                            valor={String(areaSeleccionada)}
                            onCambiar={(v) => cambiarArea(parseInt(v, 10))}
                            placeholder="Buscar área por nombre…"
                            opciones={opcionesBandeja}
                        />
                    </div>
                </div>
                <div className="search">
                    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><circle cx="11" cy="11" r="7" /><path d="m21 21-4.3-4.3" /></svg>
                    <input
                        type="text"
                        placeholder="Buscar por N.º de denuncia…"
                        value={busqueda}
                        onChange={(e) => setBusqueda(e.target.value)}
                    />
                </div>
                <BotonOrdenFecha
                    orden={ordenFecha}
                    alCambiar={setOrdenFecha}
                />
                <div className="toolbar__resultado">{denunciasFiltradas.length} denuncias en bandeja</div>
                <button className="btn-alertas" onClick={() => setConfirmarAlertas(true)} disabled={generandoAlertas}>
                    {generandoAlertas ? 'Generando…' : 'Generar alertas de vencimiento'}
                </button>
            </div>

            {cargando ? (
                <p className="cargando">Cargando bandeja…</p>
            ) : (
                <div className="table-wrap">
                    <table className="tabla-tarjetas">
                        <thead>
                            <tr>
                                <th>N.º de denuncia</th>
                                <th
                                    className="th-ordenable"
                                    onClick={() => setOrdenFecha(o => (o === 'asc' ? 'desc' : 'asc'))}
                                    title="Clic para alternar orden ascendente o descendente por fecha"
                                >
                                    Fecha de recepción {ordenFecha === 'asc' ? '↑' : '↓'}
                                </th>
                                <th>Área actual</th>
                                <th>Encargado actual</th>
                                <th>Seleccionar Encargado</th>
                                <th>Acciones</th>
                            </tr>
                        </thead>
                        <tbody>
                            {denunciasFiltradas.length === 0 ? (
                                <tr><td colSpan="6" style={{ textAlign: 'center' }}>No hay denuncias en la bandeja.</td></tr>
                            ) : (
                                denunciasFiltradas.map((d) => (
                                    <tr key={d.idDenuncia}>
                                        <td className="celda-numero" data-label="N.º de denuncia">{d.consecutivo}</td>
                                        <td className="celda-fecha" data-label="Fecha de recepción">{d.fechaRecepcion}</td>
                                        <td data-label="Área actual">
                                            <span className="area-badge">{d.nombreAreaActual}</span>
                                            {d.pendienteReasignacionUvc && <span className="etiqueta-uvc">Comercio electrónico</span>}
                                        </td>
                                        <td data-label="Encargado actual">{d.nombreEncargadoActual || <span className="sin-encargado">Sin asignar</span>}</td>
                                        <td data-label="Seleccionar encargado">
                                            {(() => {
                                                const elegido = encargadoPorId(seleccion[d.idDenuncia]);
                                                return (
                                                    <button
                                                        type="button"
                                                        className={`btn-selector-encargado${elegido ? ' btn-selector-encargado--elegido' : ''}`}
                                                        onClick={() => abrirSelectorEncargado(d)}
                                                        aria-haspopup="dialog"
                                                    >
                                                        <span className="btn-selector-encargado__texto">
                                                            {elegido ? elegido.nombre : 'Seleccionar encargado'}
                                                        </span>
                                                        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true"><circle cx="11" cy="11" r="7" /><path d="m21 21-4.3-4.3" /></svg>
                                                    </button>
                                                );
                                            })()}
                                        </td>
                                        <td data-label="Acciones">
                                            <div className="celda-acciones">
                                                <button
                                                    className="btn-accion"
                                                    disabled={!seleccion[d.idDenuncia]}
                                                    onClick={() => iniciarAsignacion(d)}
                                                >
                                                    {d.nombreEncargadoActual ? 'Reasignar' : 'Asignar'}
                                                </button>
                                                {/* La bandeja solo lista denuncias ya evaluadas en criticidad. */}
                                                {areaUvc && !enUvc && (
                                                    <button className="btn-uvc" onClick={() => { setDenunciaUvc(d); setMotivoUvc(''); setOficioUvc(''); }}>
                                                        Reasignar a UVC
                                                    </button>
                                                )}
                                                {enUvc && puedeGestionarUvc && (
                                                    <>
                                                        {d.nombreEncargadoActual && (
                                                            <button className="btn-confirmar-uvc" onClick={() => { setDenunciaConfirmar(d); setMotivoConfirmar(''); }}>
                                                                Confirmar atención
                                                            </button>
                                                        )}
                                                        <button className="btn-uvc-devolver" onClick={() => { setDenunciaDevolver(d); setAreaRectoraId(''); setMotivoDevolver(''); }}>
                                                            Devolver a Área Rectora
                                                        </button>
                                                    </>
                                                )}
                                                <button
                                                    className="btn-area"
                                                    onClick={() => { setDenunciaArea(d); setAreaNuevaId(''); setMotivoArea(''); }}
                                                >
                                                    Reasignar área
                                                </button>
                                                <button className="btn-enlace" onClick={() => verHistorial(d)}>
                                                    Ver historial
                                                </button>
                                            </div>
                                        </td>
                                    </tr>
                                ))
                            )}
                        </tbody>
                    </table>
                </div>
            )}

            {denunciaSelector && (
                <div className="modal-overlay" onClick={cerrarSelectorEncargado}>
                    <div
                        className="modal-content modal-content--encargados"
                        role="dialog"
                        aria-modal="true"
                        onClick={(e) => e.stopPropagation()}
                        onKeyDown={(e) => e.key === 'Escape' && cerrarSelectorEncargado()}
                    >
                        <h3>Seleccionar encargado — {denunciaSelector.consecutivo}</h3>
                        <div className="selector-encargado__filtros">
                            <div className="search selector-encargado__buscar">
                                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><circle cx="11" cy="11" r="7" /><path d="m21 21-4.3-4.3" /></svg>
                                <input
                                    type="text"
                                    autoFocus
                                    placeholder="Buscar por nombre o especialidad…"
                                    value={busquedaEncargado}
                                    onChange={(e) => setBusquedaEncargado(e.target.value)}
                                />
                            </div>
                            <select
                                className="selector-encargado__orden"
                                value={ordenEncargado}
                                onChange={(e) => setOrdenEncargado(e.target.value)}
                                aria-label="Ordenar encargados"
                            >
                                <option value="carga">Menor carga primero</option>
                                <option value="nombre">Nombre (A-Z)</option>
                            </select>
                        </div>
                        <div className="selector-encargado__resultado">
                            {encargadosFiltrados.length} de {encargados.length} funcionarios
                        </div>
                        <ul className="selector-encargado__lista">
                            {encargadosFiltrados.length === 0 ? (
                                <li className="selector-encargado__vacio">No hay funcionarios que coincidan con la búsqueda.</li>
                            ) : (
                                encargadosFiltrados.map((e) => {
                                    const activo = String(seleccion[denunciaSelector.idDenuncia]) === String(e.idFuncionario);
                                    return (
                                        <li key={e.idFuncionario}>
                                            <button
                                                type="button"
                                                className={`selector-encargado__item${activo ? ' selector-encargado__item--activo' : ''}`}
                                                onClick={() => elegirEncargado(e.idFuncionario)}
                                            >
                                                <span className="selector-encargado__info">
                                                    <span className="selector-encargado__nombre">{e.nombre}</span>
                                                    <span className="selector-encargado__detalle">{e.especialidad || e.rol}</span>
                                                </span>
                                                <span className="selector-encargado__carga">Carga: {e.cargaActiva}</span>
                                            </button>
                                        </li>
                                    );
                                })
                            )}
                        </ul>
                        <div className="modal-actions">
                            {seleccion[denunciaSelector.idDenuncia] && (
                                <button
                                    className="btn-cancelar"
                                    onClick={() => { handleSeleccion(denunciaSelector.idDenuncia, ''); cerrarSelectorEncargado(); }}
                                >
                                    Quitar selección
                                </button>
                            )}
                            <button className="btn-cancelar" onClick={cerrarSelectorEncargado}>Cerrar</button>
                        </div>
                    </div>
                </div>
            )}

            {denunciaAConfirmar && (
                <div className="modal-overlay">
                    <div className="modal-content">
                        <h3>Confirmar asignación de encargado</h3>
                        <p>
                            ¿Está seguro que desea asignar la denuncia <strong>{denunciaAConfirmar.consecutivo}</strong> al
                            funcionario encargado seleccionado?
                        </p>
                        <label className="campo-motivo">
                            Motivo (opcional)
                            <textarea
                                value={motivoAsignacion}
                                onChange={(e) => setMotivoAsignacion(e.target.value)}
                                rows="3"
                                maxLength="500"
                                placeholder="Justifique la asignación si lo considera necesario…"
                            />
                        </label>
                        <div className="modal-actions">
                            <button
                                className="btn-cancelar"
                                onClick={() => setDenunciaAConfirmar(null)}
                                disabled={enviandoAsignacion}
                            >
                                Cancelar
                            </button>
                            <button className="btn-confirmar" onClick={confirmarAsignacion} disabled={enviandoAsignacion}>
                                {enviandoAsignacion ? 'Asignando…' : 'Confirmar'}
                            </button>
                        </div>
                    </div>
                </div>
            )}

            {denunciaUvc && (
                <div className="modal-overlay">
                    <div className="modal-content">
                        <h3>Reasignar a UVC</h3>
                        <p>
                            La denuncia <strong>{denunciaUvc.consecutivo}</strong> será reasignada a la{' '}
                            <strong>Unidad de Vigilancia de Comercio Electrónico (UVC)</strong>. La justificación
                            y el número de oficio quedan registrados en el historial y en la bitácora.
                        </p>
                        <label className="campo-motivo">
                            Número de oficio <span className="opcional">(opcional)</span>
                            <input
                                type="text"
                                value={oficioUvc}
                                onChange={(e) => setOficioUvc(e.target.value)}
                                maxLength={LONGITUD_MAXIMA_OFICIO}
                                placeholder="Ej. OF-2026-001"
                            />
                        </label>
                        <label className="campo-motivo">
                            Justificación <span className="obligatorio">*</span>
                            <textarea
                                value={motivoUvc}
                                onChange={(e) => setMotivoUvc(e.target.value)}
                                rows="3"
                                maxLength="500"
                                placeholder="Justifique la reasignación a UVC…"
                            />
                        </label>
                        <div className="modal-actions">
                            <button className="btn-cancelar" onClick={() => setDenunciaUvc(null)} disabled={enviandoUvc}>
                                Cancelar
                            </button>
                            <button className="btn-confirmar" onClick={confirmarReasignacionUvc} disabled={enviandoUvc}>
                                {enviandoUvc ? 'Reasignando…' : 'Confirmar reasignación'}
                            </button>
                        </div>
                    </div>
                </div>
            )}

            {denunciaArea && (
                <div className="modal-overlay">
                    <div className="modal-content">
                        <h3>Reasignar a otra área</h3>
                        <p>
                            La denuncia <strong>{denunciaArea.consecutivo}</strong> será movida al área de destino. Se liberará el
                            funcionario encargado y quedará pendiente de nueva asignación. ¿Desea continuar?
                        </p>
                        <div className="campo-motivo">
                            Área de destino <span className="obligatorio">*</span>
                            <div className="campo-selector">
                                <SelectorBuscable
                                    etiqueta="Área de destino"
                                    titulo="Seleccionar área de destino"
                                    valor={areaNuevaId}
                                    onCambiar={setAreaNuevaId}
                                    placeholder="Buscar área por nombre…"
                                    opciones={opcionesAreaDestino}
                                    sobreModal
                                />
                            </div>
                        </div>
                        <label className="campo-motivo">
                            Motivo (opcional)
                            <textarea
                                value={motivoArea}
                                onChange={(e) => setMotivoArea(e.target.value)}
                                rows="3"
                                maxLength="500"
                                placeholder="Justifique la reasignación si lo considera necesario…"
                            />
                        </label>
                        <div className="modal-actions">
                            <button className="btn-cancelar" onClick={() => setDenunciaArea(null)} disabled={enviandoArea}>
                                Cancelar
                            </button>
                            <button className="btn-confirmar" onClick={confirmarReasignacionArea} disabled={enviandoArea}>
                                {enviandoArea ? 'Reasignando…' : 'Confirmar reasignación'}
                            </button>
                        </div>
                    </div>
                </div>
            )}

            {denunciaConfirmar && (
                <div className="modal-overlay">
                    <div className="modal-content">
                        <h3>Confirmar atención en UVC</h3>
                        <p>
                            La denuncia <strong>{denunciaConfirmar.consecutivo}</strong> será marcada como{' '}
                            <strong>atendida (FINALIZADA)</strong> dentro de la UVC. ¿Desea continuar?
                        </p>
                        <label className="campo-motivo">
                            Motivo (opcional)
                            <textarea
                                value={motivoConfirmar}
                                onChange={(e) => setMotivoConfirmar(e.target.value)}
                                rows="3"
                                maxLength="500"
                                placeholder="Justifique la confirmación si lo considera necesario…"
                            />
                        </label>
                        <div className="modal-actions">
                            <button className="btn-cancelar" onClick={() => setDenunciaConfirmar(null)} disabled={enviandoConfirmar}>
                                Cancelar
                            </button>
                            <button className="btn-confirmar" onClick={confirmarAtencion} disabled={enviandoConfirmar}>
                                {enviandoConfirmar ? 'Confirmando…' : 'Confirmar atención'}
                            </button>
                        </div>
                    </div>
                </div>
            )}

            {denunciaDevolver && (
                <div className="modal-overlay">
                    <div className="modal-content">
                        <h3>Devolver a Área Rectora</h3>
                        <p>
                            La denuncia <strong>{denunciaDevolver.consecutivo}</strong> saldrá de la UVC, se liberará el
                            funcionario encargado y quedará pendiente de nueva asignación en el área rectora de destino.
                        </p>
                        <div className="campo-motivo">
                            Área rectora de destino <span className="obligatorio">*</span>
                            <div className="campo-selector">
                                <SelectorBuscable
                                    etiqueta="Área rectora"
                                    titulo="Seleccionar área rectora"
                                    valor={areaRectoraId}
                                    onCambiar={setAreaRectoraId}
                                    placeholder="Buscar área por nombre…"
                                    opciones={opcionesAreaRectora}
                                    sobreModal
                                />
                            </div>
                        </div>
                        <label className="campo-motivo">
                            Justificación <span className="obligatorio">*</span>
                            <textarea
                                value={motivoDevolver}
                                onChange={(e) => setMotivoDevolver(e.target.value)}
                                rows="3"
                                maxLength="500"
                                placeholder="Justifique la devolución al área rectora…"
                            />
                        </label>
                        <div className="modal-actions">
                            <button className="btn-cancelar" onClick={() => setDenunciaDevolver(null)} disabled={enviandoDevolver}>
                                Cancelar
                            </button>
                            <button className="btn-confirmar" onClick={confirmarDevolucion} disabled={enviandoDevolver || !motivoDevolver.trim()}>
                                {enviandoDevolver ? 'Devolviendo…' : 'Confirmar devolución'}
                            </button>
                        </div>
                    </div>
                </div>
            )}

            {denunciaHistorial && (
                <div className="modal-overlay">
                    <div className="modal-content modal-content--historial">
                        <h3>Historial de asignación — {denunciaHistorial.consecutivo}</h3>
                        {cargandoHistorial ? (
                            <p>Cargando historial…</p>
                        ) : historial.length === 0 ? (
                            <p>Esta denuncia no tiene reasignaciones registradas.</p>
                        ) : (
                            <table className="denuncias-table tabla-tarjetas">
                                <thead>
                                    <tr><th>Fecha</th><th>Área anterior</th><th>Área nueva</th><th>Motivo</th><th>N.º de oficio</th></tr>
                                </thead>
                                <tbody>
                                    {historial.map((h) => (
                                        <tr key={h.idHistorial}>
                                            <td data-label="Fecha">{h.fecha}</td>
                                            <td data-label="Área anterior">{h.areaAnteriorId ?? '—'}</td>
                                            <td data-label="Área nueva">{h.areaNuevaId}</td>
                                            <td data-label="Motivo">{h.motivo ?? '—'}</td>
                                            <td data-label="N.º de oficio">{h.numeroOficio ?? '—'}</td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                        )}
                        <div className="modal-actions">
                            <button className="btn-cancelar" onClick={() => setDenunciaHistorial(null)}>Cerrar</button>
                        </div>
                    </div>
                </div>
            )}

            {confirmarAlertas && (
                <div className="modal-overlay">
                    <div className="modal-content">
                        <h3>Generar alertas de vencimiento</h3>
                        <p>
                            Se generarán alertas para las denuncias de <strong>{nombreMiArea}</strong> pendientes de encargado
                            con 3 días o más de espera. ¿Desea continuar?
                        </p>
                        <div className="modal-actions">
                            <button className="btn-cancelar" onClick={() => setConfirmarAlertas(false)} disabled={generandoAlertas}>
                                Cancelar
                            </button>
                            <button
                                className="btn-confirmar"
                                onClick={async () => { setConfirmarAlertas(false); await generarAlertas(); }}
                                disabled={generandoAlertas}
                            >
                                {generandoAlertas ? 'Generando…' : 'Confirmar'}
                            </button>
                        </div>
                    </div>
                </div>
            )}
            <AvisoToast aviso={aviso} />
        </div>
    );
}