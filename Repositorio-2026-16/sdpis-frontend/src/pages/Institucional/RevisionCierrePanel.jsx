import { useEffect, useState } from 'react';
import { ModalConfirmacion } from '../../components/comunes/ModalConfirmacion';
import { obtenerDetalleDenuncia, obtenerCriteriosTecnicos } from '../../services/criteriosTecnicosService';
import { obtenerHistorial } from '../../services/asignacionEncargadoService';
import { obtenerBitacora } from '../../services/bitacoraService';
import { etiquetaTipoBitacora } from '../../config/tiposBitacora';
import { aprobarCierre, rechazarCierre, obtenerHistorialCierre } from '../../services/resolucionCierreService';
import './RevisionCierrePanel.css';

// HU-017: la jefatura revisa el expediente completo y la resolucion
// propuesta (HU-018) y decide: aprobar (la denuncia pasa a Finalizada) o
// rechazar y devolver para ajustes (vuelve al estado previo a la revision).
// Se abre desde el boton "Revisar solicitud" de la bandeja de solicitudes de
// cierre (APROBAR_CIERRE: jefatura o superusuario; el backend vuelve a
// validar que el jefe sea el del area de la solicitud).
// Reutiliza los endpoints de detalle (HU-010), criterios/criticidad
// (HU-010/011), historial de asignacion (HU-012) y bitacora (HU-016).
//
// props:
// - denuncia: { idDenuncia, consecutivo }
// - solicitud: solicitud de cierre pendiente (SolicitudCierreDto de HU-018)
// - carpetas: { confidencial, noConfidencial } (urls o null)
// - onCancelar: () => void
// - onResuelto: (resolucion) => void  -- ResolucionCierreDto

const LONGITUD_MAXIMA = 2000;
const TAMANO_PAGINA_BITACORA = 100;

const DECISIONES = {
    aprobar: {
        titulo: 'Confirmar aprobación del cierre',
        mensaje: '¿Seguro que desea aprobar la resolución final? La denuncia pasará a estado Finalizada '
            + 'y el caso quedará cerrado formalmente.',
        textoConfirmar: 'Sí, aprobar cierre',
    },
    rechazar: {
        titulo: 'Confirmar rechazo de la resolución',
        mensaje: '¿Seguro que desea rechazar la resolución y devolverla para ajustes? La denuncia volverá '
            + 'al estado que tenía antes de la solicitud de cierre.',
        textoConfirmar: 'Sí, rechazar y devolver',
    },
};

const ETIQUETAS_ESTADO_PREVIO = {
    EN_TIEMPO: 'En tiempo',
    PRONTA_VENCER: 'Próxima a vencer',
    ATRASADA: 'Atrasada',
};

const ETIQUETAS_SOLICITUD = {
    PENDIENTE: 'Pendiente de evaluación',
    APROBADA: 'Aprobada',
    RECHAZADA: 'Rechazada',
};

function formatearFecha(fecha, conHora = true) {
    if (!fecha) return '—';
    try {
        return new Date(fecha).toLocaleString('es-CR', conHora
            ? { dateStyle: 'medium', timeStyle: 'short' }
            : { dateStyle: 'medium' });
    } catch {
        return fecha;
    }
}

function siNo(valor) {
    if (valor === 'S') return 'Sí';
    if (valor === 'N') return 'No';
    return '—';
}

// La bitacora se pagina en el backend (maximo 100 por pagina): se piden
// todas las paginas para revisar el historial completo del caso.
async function obtenerBitacoraCompleta(denunciaId) {
    const entradas = [];
    let pagina = 1;
    for (;;) {
        const resultado = await obtenerBitacora(denunciaId, { pagina, tamano: TAMANO_PAGINA_BITACORA });
        entradas.push(...resultado.items);
        if (entradas.length >= resultado.total || resultado.items.length === 0) return entradas;
        pagina += 1;
    }
}

export function RevisionCierrePanel({ denuncia, solicitud, carpetas, onCancelar, onResuelto }) {
    const [expediente, setExpediente] = useState(null);
    const [cargando, setCargando] = useState(true);
    const [errorCarga, setErrorCarga] = useState(null);

    const [observaciones, setObservaciones] = useState('');
    const [errorObservaciones, setErrorObservaciones] = useState(null);
    // 'aprobar' | 'rechazar' | null: decision esperando la doble verificacion.
    const [confirmando, setConfirmando] = useState(null);
    const [enviando, setEnviando] = useState(false);
    const [errorEnvio, setErrorEnvio] = useState(null);
    const [bitacoraVisible, setBitacoraVisible] = useState(false);

    // El panel se monta de nuevo para cada solicitud (la bandeja lo cierra al
    // volver a la lista), asi que cargando ya arranca en true.
    useEffect(() => {
        let cancelado = false;

        // allSettled: si una seccion falla (p. ej. historial), el resto se muestra igual.
        Promise.allSettled([
            obtenerDetalleDenuncia(denuncia.idDenuncia),
            obtenerCriteriosTecnicos(denuncia.idDenuncia),
            obtenerHistorial(denuncia.idDenuncia),
            obtenerBitacoraCompleta(denuncia.idDenuncia),
            obtenerHistorialCierre(denuncia.idDenuncia),
        ]).then(([detalle, criterios, historial, bitacora, historialCierre]) => {
            if (cancelado) return;
            const valor = (r) => (r.status === 'fulfilled' ? r.value : null);
            if (detalle.status === 'rejected') {
                setErrorCarga(detalle.reason?.message || 'No fue posible cargar la información de la denuncia.');
            }
            setExpediente({
                detalle: valor(detalle),
                criterios: valor(criterios),
                historial: valor(historial) ?? [],
                historialError: historial.status === 'rejected',
                bitacora: valor(bitacora) ?? [],
                bitacoraError: bitacora.status === 'rejected',
                // Solo las decisiones ya tomadas: la solicitud pendiente se muestra arriba.
                decisiones: (valor(historialCierre) ?? []).filter((h) => h.decision),
                decisionesError: historialCierre.status === 'rejected',
            });
        }).finally(() => {
            if (!cancelado) setCargando(false);
        });

        return () => {
            cancelado = true;
        };
    }, [denuncia.idDenuncia]);

    function cambiarObservaciones(valor) {
        setObservaciones(valor.slice(0, LONGITUD_MAXIMA));
        setErrorObservaciones(null);
    }

    // Primer paso: validar y abrir la doble verificacion.
    function pedirConfirmacion(decision) {
        setErrorEnvio(null);
        if (decision === 'rechazar' && !observaciones.trim()) {
            setErrorObservaciones('Indique las observaciones de la jefatura para rechazar la resolución.');
            return;
        }
        setErrorObservaciones(null);
        setConfirmando(decision);
    }

    // Segundo paso: solo aqui se envia la decision.
    async function confirmarDecision() {
        const decision = confirmando;
        setConfirmando(null);
        setEnviando(true);
        try {
            const texto = observaciones.trim();
            const resolucion = decision === 'aprobar'
                ? await aprobarCierre(solicitud.idSolicitudCierre, { observaciones: texto || null })
                : await rechazarCierre(solicitud.idSolicitudCierre, { observaciones: texto });
            onResuelto?.(resolucion);
        } catch (error) {
            setErrorEnvio(error.message || 'No fue posible registrar la decisión de la jefatura.');
        } finally {
            setEnviando(false);
        }
    }

    const detalle = expediente?.detalle;
    const criterios = expediente?.criterios;
    const modal = confirmando ? DECISIONES[confirmando] : null;

    return (
        <div className="card rc-panel">
            <div className="card-head rc-panel-head">
                <div>
                    <span className="section-divider-label">{denuncia.consecutivo}</span>
                    <h3>Revisar solicitud de cierre</h3>
                    <div className="subtitulo">
                        Revise la información completa del caso antes de aprobar o rechazar la resolución final.
                    </div>
                </div>
                <button type="button" className="btn-secondary" onClick={onCancelar} disabled={enviando}>
                    Volver a las solicitudes
                </button>
            </div>

            <div className="card-body">
                <section className="rc-seccion rc-propuesta" aria-labelledby="rc-tit-propuesta">
                    <h4 id="rc-tit-propuesta">Resolución final propuesta</h4>
                    <dl className="rc-datos">
                        <div><dt>Solicitada por</dt><dd>{solicitud.solicitadoPorNombre}</dd></div>
                        <div><dt>Fecha de la solicitud</dt><dd>{formatearFecha(solicitud.fechaSolicitud)}</dd></div>
                        <div><dt>Asignada a</dt><dd>Jefatura de {solicitud.areaNombre}</dd></div>
                        <div className="rc-dato-ancho"><dt>Resolución final</dt><dd>{solicitud.resolucionFinal}</dd></div>
                        <div className="rc-dato-ancho">
                            <dt>Observaciones finales ({formatearFecha(solicitud.fechaSolicitud, false)})</dt>
                            <dd>{solicitud.observacionesFinales}</dd>
                        </div>
                        <div className="rc-dato-ancho">
                            <dt>Informe de seguimiento y acciones realizadas</dt>
                            <dd>{solicitud.accionesRealizadas}</dd>
                        </div>
                    </dl>
                </section>

                {cargando ? (
                    <div className="rc-cargando">Cargando expediente…</div>
                ) : (
                    <>
                        {errorCarga && <div className="rc-error" role="alert">{errorCarga}</div>}

                        <section className="rc-seccion" aria-labelledby="rc-tit-denuncia">
                            <h4 id="rc-tit-denuncia">Información de la denuncia</h4>
                            {detalle ? (
                                <>
                                    <dl className="rc-datos">
                                        <div><dt>Fecha de registro</dt><dd>{formatearFecha(detalle.fechaCreacion, false)}</dd></div>
                                        <div><dt>Establecimiento</dt><dd>{detalle.hecho?.nombreEstablecimiento ?? '—'}</dd></div>
                                        <div className="rc-dato-ancho"><dt>Descripción del hecho</dt><dd>{detalle.hecho?.descripcionHecho ?? '—'}</dd></div>
                                        <div>
                                            <dt>Ubicación</dt>
                                            <dd>
                                                {detalle.ubicacion
                                                    ? [detalle.ubicacion.provincia, detalle.ubicacion.canton, detalle.ubicacion.distrito]
                                                        .filter(Boolean).join(', ')
                                                    : '—'}
                                            </dd>
                                        </div>
                                        <div><dt>Dirección exacta</dt><dd>{detalle.ubicacion?.direccionExacta || '—'}</dd></div>
                                    </dl>

                                    <h5>Productos denunciados</h5>
                                    {detalle.productos.length === 0 ? (
                                        <p className="rc-vacio">Sin productos registrados.</p>
                                    ) : (
                                        <ul className="rc-lista">
                                            {detalle.productos.map((p) => (
                                                <li key={p.idDetalleProducto}>
                                                    <strong>{p.nombreProducto}</strong>
                                                    {p.tipoProducto && <span> · {p.tipoProducto}</span>}
                                                    {p.marca && <span> · Marca: {p.marca}</span>}
                                                    {p.registroSanitario && <span> · Registro: {p.registroSanitario}</span>}
                                                    <div className="rc-texto-secundario">{p.descripcionProducto}</div>
                                                    {p.motivos.length > 0 && (
                                                        <div className="rc-texto-secundario">Motivos: {p.motivos.join(', ')}</div>
                                                    )}
                                                </li>
                                            ))}
                                        </ul>
                                    )}
                                </>
                            ) : (
                                <p className="rc-vacio">No fue posible mostrar la información de la denuncia.</p>
                            )}
                        </section>

                        <section className="rc-seccion" aria-labelledby="rc-tit-criticidad">
                            <h4 id="rc-tit-criticidad">Criterios técnicos y criticidad</h4>
                            {criterios ? (
                                <dl className="rc-datos">
                                    <div>
                                        <dt>Nivel de criticidad</dt>
                                        <dd>
                                            {criterios.nivelCriticidad ? (
                                                <span className={`badge-criticidad ${criterios.nivelCriticidad.toLowerCase()}`}>
                                                    {criterios.nivelCriticidad}
                                                </span>
                                            ) : '—'}
                                        </dd>
                                    </div>
                                    <div><dt>Fecha límite de atención</dt><dd>{formatearFecha(criterios.fechaLimite, false)}</dd></div>
                                    <div><dt>Daño potencial a la salud</dt><dd>{criterios.danoPotencialSalud ?? '—'}</dd></div>
                                    <div><dt>Criterio especializado UVC</dt><dd>{siNo(criterios.requiereCriterioEspecializado)}</dd></div>
                                    <div><dt>Vinculada a comercio electrónico</dt><dd>{siNo(criterios.vinculadoComercioElectronico)}</dd></div>
                                    <div><dt>N.º de oficio</dt><dd>{criterios.numeroOficio || '—'}</dd></div>
                                    <div className="rc-dato-ancho"><dt>Observaciones técnicas</dt><dd>{criterios.observacionesTecnicas || '—'}</dd></div>
                                </dl>
                            ) : (
                                <p className="rc-vacio">La denuncia no tiene criterios técnicos registrados.</p>
                            )}
                        </section>

                        <section className="rc-seccion" aria-labelledby="rc-tit-asignaciones">
                            <h4 id="rc-tit-asignaciones">Historial de asignaciones</h4>
                            {expediente.historialError ? (
                                <p className="rc-vacio">No fue posible cargar el historial de asignaciones.</p>
                            ) : expediente.historial.length === 0 ? (
                                <p className="rc-vacio">Sin cambios de asignación registrados.</p>
                            ) : (
                                <ul className="rc-lista">
                                    {expediente.historial.map((h) => (
                                        <li key={h.idHistorial}>
                                            <span className="rc-fecha">{formatearFecha(h.fecha)}</span>
                                            <span>{h.motivo || 'Cambio de asignación'}</span>
                                        </li>
                                    ))}
                                </ul>
                            )}
                        </section>

                        <section className="rc-seccion" aria-labelledby="rc-tit-bitacora">
                            <h4 id="rc-tit-bitacora">Cambios, acciones y comentarios (bitácora)</h4>
                            {expediente.bitacoraError ? (
                                <p className="rc-vacio">No fue posible cargar la bitácora.</p>
                            ) : expediente.bitacora.length === 0 ? (
                                <p className="rc-vacio">Sin registros en la bitácora.</p>
                            ) : (
                                <>
                                <button
                                    type="button"
                                    className="btn-secondary rc-boton-bitacora"
                                    aria-expanded={bitacoraVisible}
                                    aria-controls="rc-lista-bitacora"
                                    onClick={() => setBitacoraVisible((v) => !v)}
                                >
                                    {bitacoraVisible ? 'Ocultar bitácora' : 'Desplegar bitácora'}
                                </button>
                                {bitacoraVisible && (
                                <ol id="rc-lista-bitacora" className="rc-lista rc-lista--bitacora">
                                    {expediente.bitacora.map((e) => (
                                        <li key={e.idBitacora}>
                                            <div className="rc-entrada-cabecera">
                                                <span className={`bt-tipo bt-tipo--${e.tipoRegistro}`}>
                                                    {etiquetaTipoBitacora(e.tipoRegistro)}
                                                </span>
                                                <span className="rc-fecha">{formatearFecha(e.fecha)}</span>
                                            </div>
                                            <div>{e.contenido}</div>
                                            <div className="rc-texto-secundario">
                                                {e.funcionarioNombre ?? 'Sistema (acción automática)'}
                                            </div>
                                        </li>
                                    ))}
                                </ol>
                                )}
                                </>
                            )}
                        </section>

                        <section className="rc-seccion" aria-labelledby="rc-tit-carpetas">
                            <h4 id="rc-tit-carpetas">Documentación de respaldo</h4>
                            {carpetas?.confidencial || carpetas?.noConfidencial ? (
                                <ul className="rc-lista">
                                    {carpetas.noConfidencial && (
                                        <li><a href={carpetas.noConfidencial} target="_blank" rel="noreferrer">Carpeta no confidencial</a></li>
                                    )}
                                    {carpetas.confidencial && (
                                        <li><a href={carpetas.confidencial} target="_blank" rel="noreferrer">Carpeta confidencial</a></li>
                                    )}
                                </ul>
                            ) : (
                                <p className="rc-vacio">La denuncia no tiene documentos registrados en su expediente.</p>
                            )}
                        </section>

                        <section className="rc-seccion" aria-labelledby="rc-tit-decisiones">
                            <h4 id="rc-tit-decisiones">Historial de decisiones de cierre</h4>
                            {expediente.decisionesError ? (
                                <p className="rc-vacio">No fue posible cargar el historial de decisiones.</p>
                            ) : expediente.decisiones.length === 0 ? (
                                <p className="rc-vacio">Es la primera solicitud de cierre de esta denuncia.</p>
                            ) : (
                                <ul className="rc-lista rc-lista--decisiones">
                                    {expediente.decisiones.map(({ solicitud: s, estadoPrevio, decision: d }) => (
                                        <li key={s.idSolicitudCierre}>
                                            <div className="rc-entrada-cabecera">
                                                <span className={`rc-decision rc-decision--${d.decision.toLowerCase()}`}>
                                                    {ETIQUETAS_SOLICITUD[d.decision] ?? d.decision}
                                                </span>
                                                <span className="rc-fecha">{formatearFecha(d.fechaDecision)}</span>
                                            </div>
                                            <div className="rc-texto-secundario">
                                                Solicitada por {s.solicitadoPorNombre} el {formatearFecha(s.fechaSolicitud)}
                                                {' · '}decidida por {d.decididoPorNombre}
                                                {d.decision === 'RECHAZADA' && estadoPrevio && (
                                                    <> · volvió a {ETIQUETAS_ESTADO_PREVIO[estadoPrevio] ?? estadoPrevio}</>
                                                )}
                                            </div>
                                            <div><strong>Resolución propuesta:</strong> {s.resolucionFinal}</div>
                                            {d.observacionesJefatura && (
                                                <div><strong>Observaciones de jefatura:</strong> {d.observacionesJefatura}</div>
                                            )}
                                        </li>
                                    ))}
                                </ul>
                            )}
                        </section>
                    </>
                )}

                <div className="rc-decision-form">
                    <h4>Decisión de la jefatura</h4>
                    <p className="rc-texto-secundario">
                        La decisión queda registrada con su usuario, fecha y hora, y el estado de la denuncia cambia
                        automáticamente: al aprobar pasa a Finalizada; al rechazar vuelve al estado que tenía antes de
                        la solicitud de cierre.
                    </p>

                    <div className="rc-campo">
                        <label htmlFor="rc-observaciones">
                            Observaciones de la jefatura <span className="rc-texto-secundario">(obligatorias para rechazar)</span>
                        </label>
                        <textarea
                            id="rc-observaciones"
                            value={observaciones}
                            onChange={(e) => cambiarObservaciones(e.target.value)}
                            maxLength={LONGITUD_MAXIMA}
                            rows={4}
                            placeholder="Observaciones sobre la resolución o ajustes requeridos…"
                            aria-invalid={Boolean(errorObservaciones)}
                            aria-describedby={errorObservaciones ? 'rc-observaciones-error' : undefined}
                        />
                        <div className="rc-campo-pie">
                            {errorObservaciones
                                ? <span id="rc-observaciones-error" className="rc-campo-error">{errorObservaciones}</span>
                                : <span />}
                            <span className="rc-contador">{observaciones.length}/{LONGITUD_MAXIMA}</span>
                        </div>
                    </div>

                    {errorEnvio && <div className="rc-error" role="alert">{errorEnvio}</div>}

                    <div className="rc-decision-pie">
                        <button
                            type="button"
                            className="btn-secondary"
                            onClick={() => pedirConfirmacion('rechazar')}
                            disabled={enviando || cargando}
                        >
                            Rechazar y devolver
                        </button>
                        <button
                            type="button"
                            className="btn-primary"
                            onClick={() => pedirConfirmacion('aprobar')}
                            disabled={enviando || cargando}
                        >
                            {enviando ? 'Registrando…' : 'Aprobar cierre'}
                        </button>
                    </div>
                </div>
            </div>

            <ModalConfirmacion
                visible={Boolean(modal)}
                titulo={modal?.titulo}
                mensaje={modal?.mensaje}
                textoConfirmar={modal?.textoConfirmar}
                onConfirmar={confirmarDecision}
                onCancelar={() => setConfirmando(null)}
            />
        </div>
    );
}
