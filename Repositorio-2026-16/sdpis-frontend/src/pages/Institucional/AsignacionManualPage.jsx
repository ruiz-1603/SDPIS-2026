import { useState, useEffect, useMemo } from 'react';
import { AvisoToast } from '../../components/comunes/AvisoToast';
import { SelectorBuscable } from '../../components/comunes/SelectorBuscable';
import { BotonOrdenFecha } from '../../components/comunes/BotonOrdenFecha';
import { compararFechas } from '../../utils/ordenarFechas';
import { useAvisoToast } from '../../hooks/useAvisoToast';
import { useSesion } from '../../context/SesionContext';
import { PERMISOS } from '../../config/permisos';
import {
    obtenerDenunciasPendientes,
    obtenerDenunciasAsignadas,
    obtenerValidadoresDisponibles,
    asignarValidador,
} from '../../services/asignacionService';
import './AsignacionManualPage.css';

// HU-009 + HU-015: el area y quien asigna salen del login (ya no hay area ni
// jefe fijos en el codigo).
//   superusuario  -> denuncias de TODAS las areas, organizadas en carpetas por
//                    area (pendientes y asignadas); al abrir una carpeta ve la
//                    tabla de esa area. Los validadores se cargan segun el area
//                    de cada denuncia.
//   jefe/delegado -> solo las de su area (el backend lo garantiza).
export function AsignacionManualPage() {
    const { aviso, mostrarAviso } = useAvisoToast();
    const { tienePermiso } = useSesion();
    const veTodas = tienePermiso(PERMISOS.VER_TODAS_AREAS);

    const [cargando, setCargando] = useState(true);
    const [denunciasPendientes, setDenunciasPendientes] = useState([]);
    const [denunciasAsignadas, setDenunciasAsignadas] = useState([]);
    // Validadores por area: { [areaId]: lista }. La clave 'propia' es el
    // area del jefe (el backend la resuelve sin areaId).
    const [validadoresPorArea, setValidadoresPorArea] = useState({});
    const [seleccion, setSeleccion] = useState({});
    const [busqueda, setBusqueda] = useState('');
    const [ordenFecha, setOrdenFecha] = useState('asc');
    const [filtroArea, setFiltroArea] = useState('');

    // Selector de validador (modal con busqueda, escala a n funcionarios).
    const [denunciaSelector, setDenunciaSelector] = useState(null);
    const [busquedaValidador, setBusquedaValidador] = useState('');
    const [ordenValidador, setOrdenValidador] = useState('carga');

    const [filtroVista, setFiltroVista] = useState('pendientes'); // 'pendientes' | 'asignadas'
    const [denunciaAConfirmar, setDenunciaAConfirmar] = useState(null);
    const [enviandoAsignacion, setEnviandoAsignacion] = useState(false);

    const claveArea = (areaId) => (veTodas ? String(areaId ?? '') : 'propia');

    const cargarValidadores = async (areaId) => {
        const clave = claveArea(areaId);
        if (validadoresPorArea[clave]) return validadoresPorArea[clave];
        const lista = await obtenerValidadoresDisponibles(veTodas ? areaId : undefined);
        setValidadoresPorArea(prev => ({ ...prev, [clave]: lista }));
        return lista;
    };

    const cargarDatos = async () => {
        setCargando(true);
        try {
            const [pendientes, asignadas] = await Promise.all([
                obtenerDenunciasPendientes(),
                obtenerDenunciasAsignadas(),
            ]);
            setDenunciasPendientes(pendientes);
            setDenunciasAsignadas(asignadas);
            // La carga activa cambia al asignar: se vuelven a pedir los validadores.
            setValidadoresPorArea({});
            if (!veTodas) {
                const lista = await obtenerValidadoresDisponibles();
                setValidadoresPorArea({ propia: lista });
            }
        } catch (error) {
            console.error('Error cargando datos:', error);
            mostrarAviso(error.message || 'No fue posible cargar las denuncias.', 'error');
        } finally {
            setCargando(false);
        }
    };

    useEffect(() => {
        // eslint-disable-next-line react-hooks/set-state-in-effect
        cargarDatos();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [veTodas]);

    useEffect(() => {
        const nuevaSeleccion = { ...seleccion };
        denunciasAsignadas.forEach(d => {
            if (!nuevaSeleccion[d.idDenuncia]) {
                nuevaSeleccion[d.idDenuncia] = d.validadorAsignadoId;
            }
        });
        // eslint-disable-next-line react-hooks/set-state-in-effect
        setSeleccion(nuevaSeleccion);
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [denunciasAsignadas]);

    const handleSeleccion = (denunciaId, validadorId) => {
        setSeleccion(prev => ({ ...prev, [denunciaId]: validadorId }));
    };

    const abrirSelectorValidador = async (denuncia) => {
        setDenunciaSelector(denuncia);
        setBusquedaValidador('');
        setOrdenValidador('carga');
        try {
            await cargarValidadores(denuncia.areaId);
        } catch (error) {
            mostrarAviso(error.message || 'No fue posible cargar los validadores del área.', 'error');
        }
    };

    const cerrarSelectorValidador = () => setDenunciaSelector(null);

    const elegirValidador = (validadorId) => {
        if (!denunciaSelector) return;
        handleSeleccion(denunciaSelector.idDenuncia, String(validadorId));
        cerrarSelectorValidador();
    };

    const validadoresDe = (denuncia) => validadoresPorArea[claveArea(denuncia?.areaId)] ?? null;

    const validadorPorId = (denuncia, id) =>
        (validadoresDe(denuncia) ?? []).find(v => String(v.idFuncionario) === String(id)) || null;

    const validadoresSelector = validadoresDe(denunciaSelector) ?? [];
    const validadoresFiltrados = validadoresSelector
        .filter(v => {
            const q = busquedaValidador.trim().toLowerCase();
            return !q || (v.nombre ?? '').toLowerCase().includes(q);
        })
        .sort((a, b) =>
            ordenValidador === 'nombre'
                ? a.nombre.localeCompare(b.nombre, 'es')
                : ((a.cargaActiva ?? 0) - (b.cargaActiva ?? 0)) || a.nombre.localeCompare(b.nombre, 'es')
        );

    const iniciarAsignacion = (denuncia) => {
        const validadorId = seleccion[denuncia.idDenuncia];
        if (!validadorId) return;

        const validadorObj = validadorPorId(denuncia, validadorId);

        setDenunciaAConfirmar({
            idDenuncia: denuncia.idDenuncia,
            consecutivo: denuncia.consecutivo,
            validadorId: validadorId,
            nombreValidador: validadorObj ? validadorObj.nombre : 'el validador seleccionado'
        });
    };

    const confirmarAsignacion = async () => {
        if (!denunciaAConfirmar || enviandoAsignacion) return;
        setEnviandoAsignacion(true);

        try {
            await asignarValidador(denunciaAConfirmar.idDenuncia, denunciaAConfirmar.validadorId);
            mostrarAviso(`Denuncia ${denunciaAConfirmar.consecutivo} asignada a ${denunciaAConfirmar.nombreValidador}.`, 'exito');
            setSeleccion(prev => {
                const newState = { ...prev };
                delete newState[denunciaAConfirmar.idDenuncia];
                return newState;
            });
            setDenunciaAConfirmar(null);
            await cargarDatos();
        } catch (error) {
            console.error('Error asignando denuncia:', error);
            mostrarAviso(error.message || 'Ocurrió un error al asignar la denuncia. Intente de nuevo.', 'error');
            setDenunciaAConfirmar(null);
        } finally {
            setEnviandoAsignacion(false);
        }
    };

    // Areas que tienen denuncias (para el filtro del superusuario).
    const opcionesArea = useMemo(() => {
        const mapa = new Map();
        [...denunciasPendientes, ...denunciasAsignadas].forEach(d => {
            if (d.areaId != null && !mapa.has(d.areaId)) mapa.set(d.areaId, d.areaNombre ?? `Área ${d.areaId}`);
        });
        return [...mapa.entries()]
            .map(([valor, texto]) => ({ valor, texto }))
            .sort((a, b) => a.texto.localeCompare(b.texto, 'es'));
    }, [denunciasPendientes, denunciasAsignadas]);

    // Solo se avisa "Con UVC" si esa unidad realmente tiene denuncias en la lista.
    const hayDenunciasUvc = useMemo(
        () => [...denunciasPendientes, ...denunciasAsignadas].some((d) => /UVC|Vigilancia y Control/i.test(d.areaNombre ?? '')),
        [denunciasPendientes, denunciasAsignadas],
    );

    const delArea = (lista) => (filtroArea ? lista.filter(d => String(d.areaId) === filtroArea) : lista);
    const pendientesVisibles = delArea(denunciasPendientes);
    const asignadasVisibles = delArea(denunciasAsignadas);
    const listaActual = filtroVista === 'pendientes' ? pendientesVisibles : asignadasVisibles;

    const denunciasFiltradas = useMemo(() => {
        const filtradas = listaActual.filter(d =>
            d.consecutivo.toLowerCase().includes(busqueda.toLowerCase())
        );
        return filtradas.sort((a, b) => {
            const cmp = compararFechas(a.fechaRecepcion, b.fechaRecepcion, ordenFecha);
            if (cmp !== 0) return cmp;
            return a.consecutivo.localeCompare(b.consecutivo, 'es');
        });
    }, [listaActual, busqueda, ordenFecha]);

    // Carpetas por area (solo superusuario): cada area con las denuncias de la
    // pestana activa. Se muestran mientras no haya area elegida ni busqueda;
    // al escribir en el buscador se busca en todas las areas (tabla plana).
    const carpetas = useMemo(() => {
        const base = filtroVista === 'pendientes' ? denunciasPendientes : denunciasAsignadas;
        const porArea = new Map();
        base.forEach(d => {
            const clave = String(d.areaId ?? 'sin-area');
            if (!porArea.has(clave)) {
                porArea.set(clave, { id: clave, nombre: d.areaNombre ?? 'Sin área', total: 0 });
            }
            porArea.get(clave).total += 1;
        });
        return [...porArea.values()].sort((a, b) => a.nombre.localeCompare(b.nombre, 'es'));
    }, [filtroVista, denunciasPendientes, denunciasAsignadas]);

    const enVistaCarpetas = veTodas && !filtroArea && !busqueda.trim();
    const areaAbierta = veTodas && filtroArea
        ? (opcionesArea.find(a => String(a.valor) === filtroArea)?.texto ?? 'Área')
        : null;

    const columnas = veTodas ? 6 : 5;
    const validadoresPropios = validadoresPorArea.propia ?? [];

    return (
        <div className="page-asignacion-manual">
            <div className="masthead">
                <aside className="nota-info">
                    <span className="nota-info__icono" aria-hidden="true">
                        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                            <circle cx="12" cy="12" r="9" /><path d="M12 11v5" /><path d="M12 7.5h.01" />
                        </svg>
                    </span>
                    <p>
                    {veTodas
                        ? 'Denuncias de todas las áreas. Cada denuncia se asigna a un validador de su propia área; verifique la "Carga activa" para distribuir equitativamente.'
                        : 'Asigne o reasigne un validador a cada denuncia. Verifique la "Carga activa" para distribuir equitativamente.'}</p>
                </aside>
                <div className="masthead__stats">
                    <div className="masthead__stat">
                        <div className="cifra">{pendientesVisibles.length}</div>
                        <div className="etiqueta">Pendientes</div>
                    </div>
                    <div className="masthead__stat">
                        <div className="cifra">{asignadasVisibles.length}</div>
                        <div className="etiqueta">Asignadas</div>
                    </div>
                    <div className="masthead__stat">
                        {veTodas ? (
                            <>
                                <div className="cifra">{opcionesArea.length}</div>
                                <div className="etiqueta">
                                    Áreas con denuncias
                                    {hayDenunciasUvc && <span className="etiqueta__detalle">Con UVC</span>}
                                </div>
                            </>
                        ) : (
                            <>
                                <div className="cifra">{validadoresPropios.length}</div>
                                <div className="etiqueta">Validadores</div>
                            </>
                        )}
                    </div>
                </div>
            </div>
            <div className="tabs-filtro">
                <button
                    className={`tab-btn ${filtroVista === 'pendientes' ? 'active' : ''}`}
                    onClick={() => setFiltroVista('pendientes')}
                >
                    Pendientes de asignación
                </button>
                <button
                    className={`tab-btn ${filtroVista === 'asignadas' ? 'active' : ''}`}
                    onClick={() => setFiltroVista('asignadas')}
                >
                    Asignadas
                </button>
            </div>

            <div className="toolbar">
                {areaAbierta && (
                    <button type="button" className="carpetas-volver" onClick={() => setFiltroArea('')}>
                        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.4" aria-hidden="true">
                            <polyline points="15 6 9 12 15 18" />
                        </svg>
                        Todas las áreas
                    </button>
                )}
                {veTodas && (
                    <div className="toolbar__area">
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
                {!enVistaCarpetas && (
                    <div className="toolbar__resultado">{denunciasFiltradas.length} denuncias {filtroVista}</div>
                )}
            </div>

            {enVistaCarpetas && !cargando && (
                carpetas.length === 0 ? (
                    <div className="carpetas-vacio">No hay denuncias en esta vista.</div>
                ) : (
                    <div className="carpetas-grid">
                        {carpetas.map(c => (
                            <button
                                type="button"
                                key={c.id}
                                className="carpeta"
                                onClick={() => setFiltroArea(c.id)}
                                title={`Abrir la carpeta de ${c.nombre}`}
                            >
                                <svg className="carpeta__icono" width="30" height="30" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
                                    <path d="M3 6a2 2 0 0 1 2-2h4.2a2 2 0 0 1 1.5.7L12 6h7a2 2 0 0 1 2 2v9a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z" />
                                </svg>
                                <span className="carpeta__nombre">{c.nombre}</span>
                                <span className="carpeta__total">{c.total}</span>
                            </button>
                        ))}
                    </div>
                )
            )}

            {enVistaCarpetas && cargando && (
                <div className="carpetas-vacio">Cargando denuncias…</div>
            )}

            <div className="table-wrap" hidden={enVistaCarpetas}>
                <table className="tabla-tarjetas">
                    <thead>
                        <tr>
                            <th>N.º de denuncia</th>
                            {veTodas && <th>Área</th>}
                            <th
                                className="th-ordenable"
                                onClick={() => setOrdenFecha(o => (o === 'asc' ? 'desc' : 'asc'))}
                                title="Clic para alternar orden ascendente o descendente por fecha"
                            >
                                Fecha de recepción {ordenFecha === 'asc' ? '↑' : '↓'}
                            </th>
                            <th>Estado actual</th>
                            <th>{filtroVista === 'pendientes' ? 'Seleccionar Validador' : 'Validador Asignado'}</th>
                            <th>Acción</th>
                        </tr>
                    </thead>
                    <tbody>
                        {cargando ? (
                            <tr><td colSpan={columnas} style={{ textAlign: 'center' }}>Cargando denuncias…</td></tr>
                        ) : denunciasFiltradas.length === 0 ? (
                            <tr><td colSpan={columnas} style={{ textAlign: 'center' }}>No hay denuncias en esta vista.</td></tr>
                        ) : (
                            denunciasFiltradas.map(d => {
                                const seleccionActual = seleccion[d.idDenuncia] || '';
                                // Deshabilitar el botón si no hay selección o si la selección no cambió respecto a la actual asignada
                                const botonDeshabilitado = !seleccionActual ||
                                    (filtroVista === 'asignadas' && seleccionActual.toString() === d.validadorAsignadoId?.toString());

                                return (
                                    <tr key={d.idDenuncia}>
                                        <td className="celda-numero" data-label="N.º de denuncia">{d.consecutivo}</td>
                                        {veTodas && <td data-label="Área">{d.areaNombre ?? '—'}</td>}
                                        <td className="celda-fecha" data-label="Fecha de recepción">{d.fechaRecepcion}</td>
                                        <td data-label="Estado actual">
                                            <span className="estado">
                                                {filtroVista === 'pendientes' ? 'Pendiente de asignación' : 'En tiempo'}
                                            </span>
                                        </td>
                                        <td data-label={filtroVista === 'pendientes' ? 'Validador' : 'Validador asignado'}>
                                            {(() => {
                                                const lista = validadoresDe(d);
                                                const elegido = validadorPorId(d, seleccionActual);
                                                const esElAsignado = seleccionActual &&
                                                    String(seleccionActual) === String(d.validadorAsignadoId);
                                                // Solo se marca "No disponible" si ya se cargaron los
                                                // validadores del area y el asignado no esta entre ellos.
                                                const texto = elegido
                                                    ? elegido.nombre
                                                    : esElAsignado
                                                        ? `${d.nombreValidador ?? 'Validador actual'}${lista ? ' — No disponible' : ''}`
                                                        : 'Seleccionar validador';
                                                return (
                                                    <button
                                                        type="button"
                                                        className={`btn-selector-validador${seleccionActual ? ' btn-selector-validador--elegido' : ''}`}
                                                        onClick={() => abrirSelectorValidador(d)}
                                                        aria-haspopup="dialog"
                                                    >
                                                        <span className="btn-selector-validador__texto">{texto}</span>
                                                        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true"><circle cx="11" cy="11" r="7" /><path d="m21 21-4.3-4.3" /></svg>
                                                    </button>
                                                );
                                            })()}
                                        </td>
                                        <td data-label="">
                                            <button
                                                className="btn-asignar"
                                                disabled={botonDeshabilitado}
                                                onClick={() => iniciarAsignacion(d)}
                                            >
                                                {filtroVista === 'pendientes' ? 'Asignar' : 'Reasignar'}
                                            </button>
                                        </td>
                                    </tr>
                                );
                            })
                        )}
                    </tbody>
                </table>
            </div>

            {denunciaSelector && (
                <div className="modal-overlay" onClick={cerrarSelectorValidador}>
                    <div
                        className="modal-content modal-content--validadores"
                        role="dialog"
                        aria-modal="true"
                        onClick={(e) => e.stopPropagation()}
                        onKeyDown={(e) => e.key === 'Escape' && cerrarSelectorValidador()}
                    >
                        <h3>Seleccionar validador — {denunciaSelector.consecutivo}</h3>
                        {veTodas && denunciaSelector.areaNombre && (
                            <p className="selector-validador__resultado">Validadores de {denunciaSelector.areaNombre}</p>
                        )}
                        <div className="selector-validador__filtros">
                            <div className="search selector-validador__buscar">
                                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><circle cx="11" cy="11" r="7" /><path d="m21 21-4.3-4.3" /></svg>
                                <input
                                    type="text"
                                    autoFocus
                                    placeholder="Buscar validador por nombre…"
                                    value={busquedaValidador}
                                    onChange={(e) => setBusquedaValidador(e.target.value)}
                                />
                            </div>
                            <select
                                className="selector-validador__orden"
                                value={ordenValidador}
                                onChange={(e) => setOrdenValidador(e.target.value)}
                                aria-label="Ordenar validadores"
                            >
                                <option value="carga">Menor carga primero</option>
                                <option value="nombre">Nombre (A-Z)</option>
                            </select>
                        </div>
                        <div className="selector-validador__resultado">
                            {validadoresDe(denunciaSelector) === null
                                ? 'Cargando validadores…'
                                : `${validadoresFiltrados.length} de ${validadoresSelector.length} validadores`}
                        </div>
                        <ul className="selector-validador__lista">
                            {validadoresDe(denunciaSelector) !== null && validadoresFiltrados.length === 0 ? (
                                <li className="selector-validador__vacio">
                                    {validadoresSelector.length === 0
                                        ? 'Esta área no tiene validadores disponibles.'
                                        : 'No hay validadores que coincidan con la búsqueda.'}
                                </li>
                            ) : (
                                validadoresFiltrados.map(v => {
                                    const activo = String(seleccion[denunciaSelector.idDenuncia] ?? '') === String(v.idFuncionario);
                                    return (
                                        <li key={v.idFuncionario}>
                                            <button
                                                type="button"
                                                className={`selector-validador__item${activo ? ' selector-validador__item--activo' : ''}`}
                                                onClick={() => elegirValidador(v.idFuncionario)}
                                            >
                                                <span className="selector-validador__nombre">{v.nombre}</span>
                                                <span className="selector-validador__carga">Carga: {v.cargaActiva ?? 0}</span>
                                            </button>
                                        </li>
                                    );
                                })
                            )}
                        </ul>
                        <div className="modal-actions">
                            {seleccion[denunciaSelector.idDenuncia] && filtroVista === 'pendientes' && (
                                <button
                                    className="btn-cancelar"
                                    onClick={() => { handleSeleccion(denunciaSelector.idDenuncia, ''); cerrarSelectorValidador(); }}
                                >
                                    Quitar selección
                                </button>
                            )}
                            <button className="btn-cancelar" onClick={cerrarSelectorValidador}>Cerrar</button>
                        </div>
                    </div>
                </div>
            )}

            {denunciaAConfirmar && (
                <div className="modal-overlay">
                    <div className="modal-content">
                        <h3>Confirmar {filtroVista === 'pendientes' ? 'asignación' : 'reasignación'}</h3>
                        <p>
                            ¿Está seguro que desea asignar la denuncia <strong>{denunciaAConfirmar.consecutivo}</strong> a <strong>{denunciaAConfirmar.nombreValidador}</strong>?
                        </p>
                        <div className="modal-actions">
                            <button className="btn-cancelar" onClick={() => setDenunciaAConfirmar(null)}>Cancelar</button>
                            <button className="btn-confirmar" onClick={confirmarAsignacion} disabled={enviandoAsignacion}>
                                {enviandoAsignacion ? 'Asignando…' : 'Confirmar'}
                            </button>
                        </div>
                    </div>
                </div>
            )}

            <AvisoToast aviso={aviso} />
        </div>
    );
}