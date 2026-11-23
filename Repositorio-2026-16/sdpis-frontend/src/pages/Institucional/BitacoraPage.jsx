import { useEffect, useMemo, useState } from 'react';
import { AyudaContextual } from '../../components/comunes/AyudaContextual';
import { AvisoToast } from '../../components/comunes/AvisoToast';
import { SelectorBuscable } from '../../components/comunes/SelectorBuscable';
import { BotonOrdenFecha } from '../../components/comunes/BotonOrdenFecha';
import { compararFechas } from '../../utils/ordenarFechas';
import { useAvisoToast } from '../../hooks/useAvisoToast';
import { useSesion } from '../../context/SesionContext';
import { PERMISOS } from '../../config/permisos';
import { TIPOS_BITACORA, etiquetaTipoBitacora } from '../../config/tiposBitacora';
import {
    obtenerBitacora,
    agregarComentarioBitacora,
    obtenerDenunciasVisibles,
} from '../../services/bitacoraService';
import { obtenerEstadoSolicitudCierre } from '../../services/solicitudCierreService';
import { SolicitudCierrePanel } from './SolicitudCierrePanel';
import './BitacoraPage.css';

const TAMANO_PAGINA = 20;

// HU-016 + HU-015: la sesion ya no es simulada. El backend toma al usuario
// del token y le devuelve solo las denuncias de su alcance:
//   * funcionario   -> las que tiene asignadas;
//   * jefe/delegado -> las de su area;
//   * superusuario  -> todas.
// Aqui los permisos solo deciden que se muestra (p. ej. el buscador por id);
// el backend vuelve a validar cada consulta y cada comentario.

const ETIQUETAS_ESTADO = {
    PENDIENTE_ASIGNACION: 'Pendiente de asignación',
    EN_TIEMPO: 'En tiempo',
    PRONTA_VENCER: 'Próxima a vencer',
    ATRASADA: 'Atrasada',
    EN_REVISION: 'En revisión',
    FINALIZADA: 'Finalizada',
};

// Filtro por estado del plazo (los tres estados "en curso" con plazo vigente).
const FILTROS_PLAZO = [
    { valor: 'EN_TIEMPO', etiqueta: 'En tiempo' },
    { valor: 'PRONTA_VENCER', etiqueta: 'Próxima a vencer' },
    { valor: 'ATRASADA', etiqueta: 'Atrasada' },
];

// "2026-10-23" -> "Octubre de 2026" (se arma a mano para no depender de la zona horaria).
function etiquetaMes(fechaIso) {
    const coincide = /^(\d{4})-(\d{2})/.exec(fechaIso ?? '');
    if (!coincide) return 'Sin fecha de recepción';
    const mes = new Date(Number(coincide[1]), Number(coincide[2]) - 1, 1)
        .toLocaleDateString('es-CR', { month: 'long' });
    return `${mes.charAt(0).toUpperCase()}${mes.slice(1)} de ${coincide[1]}`;
}

// Opciones del filtro por tipo (mismo selector que el resto de filtros).
const OPCIONES_TIPO = TIPOS_BITACORA.map((t) => ({ valor: t.valor, texto: t.etiqueta, detalle: t.resumen }));

function formatearFecha(fechaIso) {
    try {
        return new Date(fechaIso).toLocaleString('es-CR', { dateStyle: 'medium', timeStyle: 'medium' });
    } catch {
        return fechaIso;
    }
}

// El selector de denuncia va arriba de la bitacora (una sola columna, sin
// lista lateral) para que solo haya un scroll vertical: el de la pagina.
// onDenunciaSeleccionada: el portal reduce el menu lateral al elegir una
// denuncia (el usuario puede volver a ampliarlo).
export function BitacoraPage({ onDenunciaSeleccionada }) {
    const { aviso, mostrarAviso } = useAvisoToast();
    const { sesion, tienePermiso } = useSesion();

    // Usuario autenticado (HU-001) y lo que puede ver (HU-015).
    const miFuncionarioId = sesion?.usuario?.funcionarioId ?? null;
    const veDenunciasDeOtros = tienePermiso(PERMISOS.VER_TODAS_AREAS) || tienePermiso(PERMISOS.VER_DENUNCIAS_AREA);
    // Quien ve todas las areas (superusuario) elige primero la carpeta (area).
    const veCarpetasPorArea = tienePermiso(PERMISOS.VER_TODAS_AREAS);

    const [denuncias, setDenuncias] = useState([]);
    const [cargandoLista, setCargandoLista] = useState(true);
    const [idManual, setIdManual] = useState('');
    // Carpeta (area) elegida; '' = todas las areas.
    const [carpetaAbierta, setCarpetaAbierta] = useState('');

    const [denunciaSeleccionada, setDenunciaSeleccionada] = useState(null);
    // Filtro de la lista de denuncias por estado del plazo ('' = todas).
    const [filtroPlazo, setFiltroPlazo] = useState('');

    const [filtroTipo, setFiltroTipo] = useState('');
    const [pagina, setPagina] = useState(1);

    const [entradas, setEntradas] = useState([]);
    const [ordenFecha, setOrdenFecha] = useState('asc');
    const [totalEntradas, setTotalEntradas] = useState(0);
    const [cargandoBitacora, setCargandoBitacora] = useState(false);
    const [errorAcceso, setErrorAcceso] = useState(null);

    const [comentario, setComentario] = useState('');
    const [enviandoComentario, setEnviandoComentario] = useState(false);

    // Se incrementa cada vez que hay que forzar un refetch de la pagina actual
    // (p. ej. tras agregar un comentario cuando la pagina calculada resulta
    // ser la misma en la que ya estabamos: setPagina(mismoValor) no dispara
    // el efecto de abajo porque React descarta la actualizacion de estado).
    const [version, setVersion] = useState(0);

    // HU-018: estado de la solicitud de cierre de la denuncia abierta y si se
    // esta mostrando la revision del expediente para solicitarlo.
    const [estadoCierre, setEstadoCierre] = useState(null);
    const [mostrandoCierre, setMostrandoCierre] = useState(false);
    const cierrePendiente = Boolean(estadoCierre?.solicitudPendiente);
    // Con el cierre en revision, el funcionario ya no comenta; la jefatura si.
    const comentariosBloqueados = cierrePendiente && !veDenunciasDeOtros;
    // HU-017: la jefatura aprueba o rechaza desde "Solicitudes de cierre";
    // aqui solo se le indica donde hacerlo.
    const puedeResolverCierre = tienePermiso(PERMISOS.APROBAR_CIERRE);

    async function cargarDenuncias() {
        setCargandoLista(true);
        try {
            setDenuncias(await obtenerDenunciasVisibles());
        } catch (error) {
            console.error('Error cargando denuncias visibles:', error);
            mostrarAviso(error.message || 'No fue posible cargar la lista de denuncias.', 'error');
        } finally {
            setCargandoLista(false);
        }
    }

    // Se carga al entrar y si cambia el usuario de la sesion.
    useEffect(() => {
        // eslint-disable-next-line react-hooks/set-state-in-effect
        cargarDenuncias();
        setDenunciaSeleccionada(null);
        setCarpetaAbierta('');
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [miFuncionarioId]);

    // Carpetas por area (solo superusuario), ordenadas por nombre.
    const carpetas = useMemo(() => {
        const porArea = new Map();
        denuncias.forEach((d) => {
            const clave = String(d.areaId ?? 'sin-area');
            if (!porArea.has(clave)) {
                porArea.set(clave, { valor: clave, texto: d.areaNombre ?? 'Sin área', total: 0 });
            }
            porArea.get(clave).total += 1;
        });
        return [...porArea.values()]
            .sort((a, b) => a.texto.localeCompare(b.texto, 'es'))
            .map((c) => ({ valor: c.valor, texto: c.texto, detalle: `${c.total}` }));
    }, [denuncias]);

    // Opciones del selector de denuncia: las de la carpeta elegida (o todas).
    // Si la denuncia abierta quedo fuera (p. ej. "Ir a denuncia por N.º"), se
    // agrega para que el selector la muestre.
    const opcionesDenuncia = useMemo(() => {
        // `denuncias` ya viene filtrada por el backend segun el alcance del rol;
        // aqui solo se acota por carpeta (area) y por estado del plazo.
        const visibles = denuncias
            .filter((d) => !(veCarpetasPorArea && carpetaAbierta) || String(d.areaId ?? 'sin-area') === carpetaAbierta)
            .filter((d) => !filtroPlazo || d.estado === filtroPlazo);
        // Mas recientes primero y agrupadas por mes de recepcion (las del mismo
        // mes quedan contiguas, que es lo que necesita el selector).
        const opciones = [...visibles]
            .sort((a, b) => (b.fechaCreacionIso ?? '').localeCompare(a.fechaCreacionIso ?? ''))
            .map((d) => ({
                valor: d.idDenuncia,
                texto: d.consecutivo,
                detalle: ETIQUETAS_ESTADO[d.estado] ?? d.estado,
                grupo: etiquetaMes(d.fechaCreacionIso),
            }));
        if (denunciaSeleccionada && !opciones.some((o) => o.valor === denunciaSeleccionada.idDenuncia)) {
            opciones.unshift({ valor: denunciaSeleccionada.idDenuncia, texto: denunciaSeleccionada.consecutivo });
        }
        return opciones;
    }, [denuncias, veCarpetasPorArea, carpetaAbierta, filtroPlazo, denunciaSeleccionada]);

    function elegirDenuncia(valor) {
        const denuncia = denuncias.find((d) => String(d.idDenuncia) === valor);
        if (denuncia) seleccionar(denuncia);
    }

    useEffect(() => {
        if (!denunciaSeleccionada) return undefined;

        let cancelado = false;
        setCargandoBitacora(true);
        setErrorAcceso(null);

        obtenerBitacora(denunciaSeleccionada.idDenuncia, {
            tipo: filtroTipo || undefined,
            pagina,
            tamano: TAMANO_PAGINA,
        })
            .then((resultado) => {
                if (cancelado) return;
                setEntradas(resultado.items);
                setTotalEntradas(resultado.total);
            })
            .catch((error) => {
                if (cancelado) return;
                setEntradas([]);
                setTotalEntradas(0);
                setErrorAcceso(error.message || 'No fue posible consultar la bitácora de esta denuncia.');
            })
            .finally(() => {
                if (!cancelado) setCargandoBitacora(false);
            });

        return () => {
            cancelado = true;
        };
    }, [denunciaSeleccionada, filtroTipo, pagina, version]);

    // HU-018: el backend decide si el usuario puede solicitar el cierre
    // (responsable asignado + denuncia en curso); aqui solo se muestra.
    useEffect(() => {
        if (!denunciaSeleccionada) return undefined;

        let cancelado = false;
        obtenerEstadoSolicitudCierre(denunciaSeleccionada.idDenuncia)
            .then((estado) => {
                if (!cancelado) setEstadoCierre(estado);
            })
            .catch(() => {
                // Sin estado no se ofrece el boton; el error de acceso ya lo
                // muestra la consulta de la bitacora.
                if (!cancelado) setEstadoCierre(null);
            });

        return () => {
            cancelado = true;
        };
    }, [denunciaSeleccionada, version]);

    function cierreSolicitado() {
        setMostrandoCierre(false);
        mostrarAviso('Solicitud de cierre enviada a la jefatura. La denuncia quedó en revisión.', 'exito');
        cargarDenuncias();
        setFiltroTipo('');
        setPagina(1);
        setVersion((v) => v + 1);
    }

    function seleccionar(denuncia) {
        setDenunciaSeleccionada(denuncia);
        setMostrandoCierre(false);
        setEstadoCierre(null);
        onDenunciaSeleccionada?.();
        setFiltroTipo('');
        setPagina(1);
        setComentario('');
    }

    function irADenunciaManual(evento) {
        evento.preventDefault();
        const texto = idManual.trim();
        if (!texto) {
            mostrarAviso('Indique el número de denuncia.', 'error');
            return;
        }
        // Se busca en la lista que el backend ya filtro por alcance (asi no se
        // consulta una denuncia inexistente). Acepta el consecutivo completo o
        // solo su numero secuencial (el ultimo bloque: "7" -> ABAN-2026-000007).
        // El id interno de la base de datos no se usa: el usuario no lo ve.
        const minuscula = texto.toLowerCase();
        let coincidencias = denuncias.filter((d) => d.consecutivo.toLowerCase() === minuscula);
        if (coincidencias.length === 0 && /^\d+$/.test(texto)) {
            coincidencias = denuncias.filter((d) => Number(d.consecutivo.split('-').pop()) === Number(texto));
        }
        if (coincidencias.length === 0) {
            mostrarAviso(`No hay una denuncia "${texto}" disponible para usted. Revise el número e intente de nuevo.`, 'error');
            return;
        }
        if (coincidencias.length > 1) {
            mostrarAviso(
                `Hay varias denuncias con el N.º ${texto}: ${coincidencias.map((d) => d.consecutivo).join(', ')}. Escriba el consecutivo completo.`,
                'error',
            );
            return;
        }
        const encontrada = coincidencias[0];
        seleccionar(encontrada);
        setIdManual('');
    }

    function cambiarFiltroTipo(valor) {
        setFiltroTipo(valor);
        setPagina(1);
    }

    const totalPaginas = Math.max(1, Math.ceil(totalEntradas / TAMANO_PAGINA));

    const entradasOrdenadas = useMemo(() => {
        if (!entradas || entradas.length === 0) return [];
        return [...entradas].sort((a, b) => compararFechas(a.fecha, b.fecha, ordenFecha));
    }, [entradas, ordenFecha]);

    async function enviarComentario(evento) {
        evento.preventDefault();
        if (!denunciaSeleccionada) return;

        const texto = comentario.trim();
        if (!texto) {
            mostrarAviso('El comentario no puede estar vacío.', 'error');
            return;
        }

        setEnviandoComentario(true);
        try {
            await agregarComentarioBitacora(denunciaSeleccionada.idDenuncia, { contenido: texto });
            setComentario('');
            mostrarAviso('Comentario agregado a la bitácora.', 'exito');

            // El comentario se guarda con tipo 'comentario': si el filtro activo lo
            // excluiria, se limpia para no dar la impresion de que no se guardo.
            if (filtroTipo && filtroTipo !== 'comentario') {
                setFiltroTipo('');
            }
            // Se recarga la ultima pagina para mostrar el comentario recien creado
            // (orden cronologico ascendente: el mas reciente queda al final).
            // setPagina no alcanza por si sola: si la pagina calculada es igual a
            // la actual, React descarta la actualizacion y el efecto no se vuelve
            // a ejecutar -- version fuerza el refetch en todos los casos.
            const totalConNuevo = totalEntradas + 1;
            setPagina(Math.max(1, Math.ceil(totalConNuevo / TAMANO_PAGINA)));
            setVersion((v) => v + 1);
        } catch (error) {
            mostrarAviso(error.message || 'No fue posible agregar el comentario.', 'error');
        } finally {
            setEnviandoComentario(false);
        }
    }

    return (
        <div className="bt-layout">
            <AvisoToast aviso={aviso} />

            <aside className="bt-nota">
                <span className="bt-nota__icono" aria-hidden="true">
                    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                        <circle cx="12" cy="12" r="9" /><path d="M12 11v5" /><path d="M12 7.5h.01" />
                    </svg>
                </span>
                <p>Espacio de seguimiento y trazabilidad de las denuncias: consulte el historial de cada caso y agregue comentarios.</p>
            </aside>

            <section className="card bt-selector" aria-label="Seleccionar denuncia">
                <div className="bt-selector-fila">
                    {veCarpetasPorArea && (
                        <div className="bt-selector-campo">
                            <span className="bt-selector-etiqueta">
                                <svg width="15" height="15" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
                                    <path d="M3 6a2 2 0 0 1 2-2h4.2a2 2 0 0 1 1.5.7L12 6h7a2 2 0 0 1 2 2v9a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z" />
                                </svg>
                                Carpeta (área)
                            </span>
                            <SelectorBuscable
                                etiqueta="Área"
                                valor={carpetaAbierta}
                                onCambiar={setCarpetaAbierta}
                                textoTodos="Todas las áreas"
                                placeholder="Buscar área por nombre…"
                                opciones={carpetas}
                                deshabilitado={cargandoLista}
                            />
                        </div>
                    )}

                    <div className="bt-selector-campo">
                        <span className="bt-selector-etiqueta">
                            {veDenunciasDeOtros ? 'Denuncia' : 'Mis denuncias asignadas'}
                            {!cargandoLista && (
                                <span className="bt-selector-cuenta">
                                    {opcionesDenuncia.length} disponible{opcionesDenuncia.length === 1 ? '' : 's'}
                                </span>
                            )}
                        </span>
                        <SelectorBuscable
                            etiqueta="Denuncia"
                            valor={denunciaSeleccionada ? String(denunciaSeleccionada.idDenuncia) : ''}
                            onCambiar={elegirDenuncia}
                            placeholder="Buscar por N.º de denuncia…"
                            titulo="Seleccionar denuncia (agrupadas por mes de recepción)"
                            opciones={opcionesDenuncia}
                            deshabilitado={cargandoLista || opcionesDenuncia.length === 0}
                        />
                    </div>

                    <div className="bt-selector-campo">
                        <label className="bt-selector-etiqueta" htmlFor="bt-filtro-plazo">Estado del plazo</label>
                        <select id="bt-filtro-plazo" value={filtroPlazo} onChange={(e) => setFiltroPlazo(e.target.value)}>
                            <option value="">Todos los estados</option>
                            {FILTROS_PLAZO.map((f) => (
                                <option key={f.valor} value={f.valor}>{f.etiqueta}</option>
                            ))}
                        </select>
                    </div>

                    {/* Buscador por id interno de base de datos: solo quien ve denuncias
              de otros (jefe de area, superusuario o delegado). El backend
              igual responde 403 si la denuncia esta fuera de su alcance. */}
                    {veDenunciasDeOtros && (
                        <form className="bt-ir-manual" onSubmit={irADenunciaManual}>
                            <div className="bt-ir-manual-etiqueta">
                                <label htmlFor="bt-id-manual">Ir a denuncia por N.º</label>
                                <AyudaContextual
                                    etiqueta="Ayuda sobre el número de denuncia"
                                    texto="Escriba el número de la denuncia, o su consecutivo completo, tal como aparece en el listado. Ejemplo: 7 o ABAN-2026-000007. Si varias áreas tienen el mismo número, se le pedirá el consecutivo completo. Solo se encuentran denuncias a las que usted tiene acceso."
                                />
                            </div>
                            <div className="bt-ir-manual-controles">
                                <input
                                    id="bt-id-manual"
                                    type="text"
                                    placeholder="Ej. 7 o ABAN-2026-000007"
                                    value={idManual}
                                    onChange={(e) => setIdManual(e.target.value)}
                                />
                                <button type="submit" className="btn-secondary">Ir</button>
                            </div>
                        </form>
                    )}
                </div>
                {!cargandoLista && denuncias.length === 0 && (
                    <p className="bt-selector-vacio">
                        {veDenunciasDeOtros ? 'No hay denuncias disponibles.' : 'No tiene denuncias asignadas.'}
                    </p>
                )}
                {!cargandoLista && denuncias.length > 0 && opcionesDenuncia.length === 0 && (
                    <p className="bt-selector-vacio">No hay denuncias que coincidan con los filtros elegidos.</p>
                )}
            </section>

            <section className="bt-detalle" aria-label="Bitácora de la denuncia">
                {!denunciaSeleccionada ? (
                    <div className="card">
                        <div className="card-body bt-vacio">Seleccione una denuncia para ver su bitácora.</div>
                    </div>
                ) : mostrandoCierre ? (
                    <SolicitudCierrePanel
                        denuncia={denunciaSeleccionada}
                        carpetas={{
                            confidencial: estadoCierre?.carpetaConfidencialUrl,
                            noConfidencial: estadoCierre?.carpetaNoConfidencialUrl,
                        }}
                        onCancelar={() => setMostrandoCierre(false)}
                        onSolicitado={cierreSolicitado}
                    />
                ) : (
                    <div className="card bt-detalle-card">
                        <div className="card-head bt-detalle-head">
                            <div>
                                <span className="section-divider-label">{denunciaSeleccionada.consecutivo}</span>
                                <h3>Bitácora de seguimiento</h3>
                                <div className="subtitulo">
                                    Registro cronológico e inmutable: no se pueden editar ni eliminar entradas.
                                </div>
                            </div>
                            {/* HU-018: solo el funcionario responsable asignado (lo decide el backend). */}
                            {tienePermiso(PERMISOS.SOLICITAR_CIERRE) && estadoCierre?.puedeSolicitar && !errorAcceso && (
                                <button type="button" className="btn-primary" onClick={() => setMostrandoCierre(true)}>
                                    Solicitar cierre
                                </button>
                            )}
                        </div>

                        <div className="card-body">
                            {cierrePendiente && (
                                <div className="sc-aviso-revision" role="status">
                                    <strong>Cierre en revisión.</strong>{' '}
                                    {estadoCierre.solicitudPendiente.solicitadoPorNombre} solicitó el cierre el{' '}
                                    {formatearFecha(estadoCierre.solicitudPendiente.fechaSolicitud)}. Está asignada a la
                                    jefatura de {estadoCierre.solicitudPendiente.areaNombre} para su evaluación.
                                    {puedeResolverCierre && ' Puede aprobarla o rechazarla desde Solicitudes de cierre.'}
                                </div>
                            )}

                            <div className="bt-filtro">
                                <div className="bt-selector-campo bt-filtro-tipo">
                                    <span className="bt-selector-etiqueta">Tipo de acción</span>
                                    <SelectorBuscable
                                        etiqueta="Tipo de acción"
                                        valor={filtroTipo}
                                        onCambiar={cambiarFiltroTipo}
                                        textoTodos="Todos los tipos"
                                        placeholder="Buscar tipo de acción…"
                                        opciones={OPCIONES_TIPO}
                                    />
                                </div>
                                <BotonOrdenFecha
                                    orden={ordenFecha}
                                    alCambiar={setOrdenFecha}
                                    etiqueta="Fecha"
                                />
                            </div>
                            <p className="bt-filtro-ayuda" aria-live="polite">
                                {TIPOS_BITACORA.find((t) => t.valor === filtroTipo)?.ayuda
                                    ?? 'Se muestran todos los registros de la bitácora. Elija un tipo para ver solo ese grupo de acciones.'}
                            </p>

                            {cargandoBitacora ? (
                                <div className="bt-sin-resultados">Cargando bitácora…</div>
                            ) : errorAcceso ? (
                                <div className="bt-error-acceso" role="alert">{errorAcceso}</div>
                            ) : entradas.length === 0 ? (
                                <div className="bt-sin-resultados">No hay registros para el filtro seleccionado.</div>
                            ) : (
                                <>
                                    <ol className="bt-linea-tiempo">
                                        {entradasOrdenadas.map((entrada) => (
                                            <li key={entrada.idBitacora} className="bt-entrada">
                                                <div className="bt-entrada-cabecera">
                                                    <span className={`bt-tipo bt-tipo--${entrada.tipoRegistro}`}>
                                                        {etiquetaTipoBitacora(entrada.tipoRegistro)}
                                                    </span>
                                                    <span className="bt-fecha">{formatearFecha(entrada.fecha)}</span>
                                                </div>
                                                <p className="bt-contenido">{entrada.contenido}</p>
                                                <div className="bt-entrada-meta">
                                                    <span>{entrada.funcionarioNombre ?? 'Sistema (acción automática)'}</span>
                                                    {entrada.areaOrigenNombre && <span>· {entrada.areaOrigenNombre}</span>}
                                                </div>
                                            </li>
                                        ))}
                                    </ol>

                                    <div className="bt-paginacion">
                                        <button
                                            type="button"
                                            className="btn-secondary"
                                            onClick={() => setPagina((p) => Math.max(1, p - 1))}
                                            disabled={pagina <= 1}
                                        >
                                            Anterior
                                        </button>
                                        <span>Página {pagina} de {totalPaginas} · {totalEntradas} registro{totalEntradas === 1 ? '' : 's'}</span>
                                        <button
                                            type="button"
                                            className="btn-secondary"
                                            onClick={() => setPagina((p) => Math.min(totalPaginas, p + 1))}
                                            disabled={pagina >= totalPaginas}
                                        >
                                            Siguiente
                                        </button>
                                    </div>
                                </>
                            )}

                            {!errorAcceso && comentariosBloqueados && (
                                <p className="bt-selector-vacio">
                                    La solicitud de cierre ya fue enviada: no se pueden agregar más comentarios.
                                </p>
                            )}

                            {!errorAcceso && !comentariosBloqueados && (
                                <form className="bt-form-comentario" onSubmit={enviarComentario}>
                                    <label htmlFor="bt-comentario">
                                        Agregar comentario manual{' '}
                                        <span className="bt-comentario-ayuda">(Opcional)</span>
                                    </label>
                                    <textarea
                                        id="bt-comentario"
                                        value={comentario}
                                        onChange={(e) => setComentario(e.target.value.slice(0, 2000))}
                                        maxLength={2000}
                                        rows={3}
                                        placeholder="Detalle o contexto de la acción que no queda en los demás campos."
                                    />
                                    <div className="bt-form-comentario-pie">
                                        <span className="bt-contador">{comentario.length}/2000</span>
                                        <button type="submit" className="btn-primary" disabled={enviandoComentario || !comentario.trim()}>
                                            {enviandoComentario ? 'Guardando…' : 'Agregar comentario'}
                                        </button>
                                    </div>
                                </form>
                            )}
                        </div>
                    </div>
                )}
            </section>
        </div>
    );
}