import { useEffect, useState } from 'react';
import { ModalConfirmacion } from '../../components/comunes/ModalConfirmacion';
import { obtenerDetalleDenuncia, obtenerCriteriosTecnicos } from '../../services/criteriosTecnicosService';
import { obtenerHistorial } from '../../services/asignacionEncargadoService';
import { obtenerBitacora } from '../../services/bitacoraService';
import { etiquetaTipoBitacora } from '../../config/tiposBitacora';
import { solicitarCierreDenuncia } from '../../services/solicitudCierreService';
import './SolicitudCierrePanel.css';

// HU-018: revision del expediente y solicitud de cierre. Se abre desde el
// boton "Solicitar cierre" de la bitacora (solo el funcionario responsable
// asignado lo ve; el backend lo vuelve a validar).
// No duplica consultas: reutiliza los endpoints de detalle (HU-010),
// criterios/criticidad (HU-010/011), historial de asignacion (HU-012) y
// bitacora (HU-016). Adjuntos: el sistema aun no tiene carga de archivos;
// se muestran las carpetas del expediente si estan registradas.
//
// props:
// - denuncia: { idDenuncia, consecutivo }
// - carpetas: { confidencial, noConfidencial } (urls o null)
// - onCancelar: () => void
// - onSolicitado: (solicitud) => void

const LONGITUD_MAXIMA = 2000;
const TAMANO_PAGINA_BITACORA = 100;

const MENSAJE_CONFIRMACION =
    '¿Seguro que desea solicitar el cierre de la denuncia? Una vez enviada, no podrá ser modificada '
    + 'ni se podrá agregar más documentación a la misma, y será evaluada por la jefatura.';

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
// todas las paginas para mostrar el historial completo del caso.
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

export function SolicitudCierrePanel({ denuncia, carpetas, onCancelar, onSolicitado }) {
    const [expediente, setExpediente] = useState(null);
    const [cargando, setCargando] = useState(true);
    const [errorCarga, setErrorCarga] = useState(null);

    const [formulario, setFormulario] = useState({
        resolucionFinal: '',
        observacionesFinales: '',
        accionesRealizadas: '',
    });
    const [errores, setErrores] = useState({});
    const [confirmando, setConfirmando] = useState(false);
    const [enviando, setEnviando] = useState(false);
    const [errorEnvio, setErrorEnvio] = useState(null);
    const [bitacoraVisible, setBitacoraVisible] = useState(false);

    // El panel se monta de nuevo para cada denuncia (la bitacora lo cierra al
    // cambiar de denuncia), asi que cargando ya arranca en true.
    useEffect(() => {
        let cancelado = false;

        // allSettled: si una seccion falla (p. ej. historial), el resto se muestra igual.
        Promise.allSettled([
            obtenerDetalleDenuncia(denuncia.idDenuncia),
            obtenerCriteriosTecnicos(denuncia.idDenuncia),
            obtenerHistorial(denuncia.idDenuncia),
            obtenerBitacoraCompleta(denuncia.idDenuncia),
        ]).then(([detalle, criterios, historial, bitacora]) => {
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
            });
        }).finally(() => {
            if (!cancelado) setCargando(false);
        });

        return () => {
            cancelado = true;
        };
    }, [denuncia.idDenuncia]);

    function cambiarCampo(campo, valor) {
        setFormulario((f) => ({ ...f, [campo]: valor.slice(0, LONGITUD_MAXIMA) }));
        setErrores((e) => ({ ...e, [campo]: undefined }));
    }

    function validar() {
        const nuevos = {};
        if (!formulario.resolucionFinal.trim()) nuevos.resolucionFinal = 'Indique la resolución final del caso.';
        if (!formulario.observacionesFinales.trim()) nuevos.observacionesFinales = 'Indique las observaciones finales.';
        if (!formulario.accionesRealizadas.trim()) {
            nuevos.accionesRealizadas = 'Indique la información de seguimiento y las acciones realizadas.';
        }
        setErrores(nuevos);
        return Object.keys(nuevos).length === 0;
    }

    // Primer paso: validar y abrir la doble verificacion.
    function pedirConfirmacion(evento) {
        evento.preventDefault();
        setErrorEnvio(null);
        if (validar()) setConfirmando(true);
    }

    // Segundo paso: solo aqui se dispara la solicitud real.
    async function confirmarSolicitud() {
        setConfirmando(false);
        setEnviando(true);
        try {
            const solicitud = await solicitarCierreDenuncia(denuncia.idDenuncia, {
                resolucionFinal: formulario.resolucionFinal.trim(),
                observacionesFinales: formulario.observacionesFinales.trim(),
                accionesRealizadas: formulario.accionesRealizadas.trim(),
            });
            onSolicitado?.(solicitud);
        } catch (error) {
            setErrorEnvio(error.message || 'No fue posible enviar la solicitud de cierre.');
        } finally {
            setEnviando(false);
        }
    }

    const detalle = expediente?.detalle;
    const criterios = expediente?.criterios;

    return (
        <div className="card sc-panel">
            <div className="card-head sc-panel-head">
                <div>
                    <span className="section-divider-label">{denuncia.consecutivo}</span>
                    <h3>Solicitar cierre de la denuncia</h3>
                    <div className="subtitulo">
                        Revise toda la información del caso antes de enviar la solicitud a la jefatura de su área.
                    </div>
                </div>
                <button type="button" className="btn-secondary" onClick={onCancelar} disabled={enviando}>
                    Volver a la bitácora
                </button>
            </div>

            <div className="card-body">
                {cargando ? (
                    <div className="sc-cargando">Cargando expediente…</div>
                ) : (
                    <>
                        {errorCarga && <div className="sc-error" role="alert">{errorCarga}</div>}

                        <section className="sc-seccion" aria-labelledby="sc-tit-denuncia">
                            <h4 id="sc-tit-denuncia">Información de la denuncia</h4>
                            {detalle ? (
                                <>
                                    <dl className="sc-datos">
                                        <div><dt>Fecha de registro</dt><dd>{formatearFecha(detalle.fechaCreacion, false)}</dd></div>
                                        <div><dt>Establecimiento</dt><dd>{detalle.hecho?.nombreEstablecimiento ?? '—'}</dd></div>
                                        <div className="sc-dato-ancho"><dt>Descripción del hecho</dt><dd>{detalle.hecho?.descripcionHecho ?? '—'}</dd></div>
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
                                        <p className="sc-vacio">Sin productos registrados.</p>
                                    ) : (
                                        <ul className="sc-lista">
                                            {detalle.productos.map((p) => (
                                                <li key={p.idDetalleProducto}>
                                                    <strong>{p.nombreProducto}</strong>
                                                    {p.tipoProducto && <span> · {p.tipoProducto}</span>}
                                                    {p.marca && <span> · Marca: {p.marca}</span>}
                                                    {p.registroSanitario && <span> · Registro: {p.registroSanitario}</span>}
                                                    <div className="sc-texto-secundario">{p.descripcionProducto}</div>
                                                    {p.motivos.length > 0 && (
                                                        <div className="sc-texto-secundario">Motivos: {p.motivos.join(', ')}</div>
                                                    )}
                                                </li>
                                            ))}
                                        </ul>
                                    )}
                                </>
                            ) : (
                                <p className="sc-vacio">No fue posible mostrar la información de la denuncia.</p>
                            )}
                        </section>

                        <section className="sc-seccion" aria-labelledby="sc-tit-criticidad">
                            <h4 id="sc-tit-criticidad">Criterios técnicos y criticidad</h4>
                            {criterios ? (
                                <dl className="sc-datos">
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
                                    <div className="sc-dato-ancho"><dt>Observaciones técnicas</dt><dd>{criterios.observacionesTecnicas || '—'}</dd></div>
                                </dl>
                            ) : (
                                <p className="sc-vacio">La denuncia no tiene criterios técnicos registrados.</p>
                            )}
                        </section>

                        <section className="sc-seccion" aria-labelledby="sc-tit-asignaciones">
                            <h4 id="sc-tit-asignaciones">Historial de asignaciones</h4>
                            {expediente.historialError ? (
                                <p className="sc-vacio">No fue posible cargar el historial de asignaciones.</p>
                            ) : expediente.historial.length === 0 ? (
                                <p className="sc-vacio">Sin cambios de asignación registrados.</p>
                            ) : (
                                <ul className="sc-lista">
                                    {expediente.historial.map((h) => (
                                        <li key={h.idHistorial}>
                                            <span className="sc-fecha">{formatearFecha(h.fecha)}</span>
                                            <span>{h.motivo || 'Cambio de asignación'}</span>
                                        </li>
                                    ))}
                                </ul>
                            )}
                        </section>

                        <section className="sc-seccion" aria-labelledby="sc-tit-bitacora">
                            <h4 id="sc-tit-bitacora">Cambios, acciones y comentarios (bitácora)</h4>
                            {expediente.bitacoraError ? (
                                <p className="sc-vacio">No fue posible cargar la bitácora.</p>
                            ) : expediente.bitacora.length === 0 ? (
                                <p className="sc-vacio">Sin registros en la bitácora.</p>
                            ) : (
                                <>
                                <button
                                    type="button"
                                    className="btn-secondary sc-boton-bitacora"
                                    aria-expanded={bitacoraVisible}
                                    aria-controls="sc-lista-bitacora"
                                    onClick={() => setBitacoraVisible((v) => !v)}
                                >
                                    {bitacoraVisible ? 'Ocultar bitácora' : 'Desplegar bitácora'}
                                </button>
                                {bitacoraVisible && (
                                <ol id="sc-lista-bitacora" className="sc-lista sc-lista--bitacora">
                                    {expediente.bitacora.map((e) => (
                                        <li key={e.idBitacora}>
                                            <div className="sc-entrada-cabecera">
                                                <span className={`bt-tipo bt-tipo--${e.tipoRegistro}`}>
                                                    {etiquetaTipoBitacora(e.tipoRegistro)}
                                                </span>
                                                <span className="sc-fecha">{formatearFecha(e.fecha)}</span>
                                            </div>
                                            <div>{e.contenido}</div>
                                            <div className="sc-texto-secundario">
                                                {e.funcionarioNombre ?? 'Sistema (acción automática)'}
                                            </div>
                                        </li>
                                    ))}
                                </ol>
                                )}
                                </>
                            )}
                        </section>

                        <section className="sc-seccion" aria-labelledby="sc-tit-carpetas">
                            <h4 id="sc-tit-carpetas">Archivos adjuntos y evidencias</h4>
                            {carpetas?.confidencial || carpetas?.noConfidencial ? (
                                <ul className="sc-lista">
                                    {carpetas.noConfidencial && (
                                        <li><a href={carpetas.noConfidencial} target="_blank" rel="noreferrer">Carpeta no confidencial</a></li>
                                    )}
                                    {carpetas.confidencial && (
                                        <li><a href={carpetas.confidencial} target="_blank" rel="noreferrer">Carpeta confidencial</a></li>
                                    )}
                                </ul>
                            ) : (
                                <p className="sc-vacio">La denuncia no tiene documentos registrados en su expediente.</p>
                            )}
                        </section>
                    </>
                )}

                <form className="sc-formulario" onSubmit={pedirConfirmacion} noValidate>
                    <h4>Solicitud de cierre</h4>
                    <p className="sc-texto-secundario">
                        La fecha de las observaciones finales se registra automáticamente al enviar la solicitud.
                    </p>

                    <CampoTexto
                        id="sc-resolucion"
                        etiqueta="Resolución final del caso"
                        valor={formulario.resolucionFinal}
                        error={errores.resolucionFinal}
                        onCambiar={(v) => cambiarCampo('resolucionFinal', v)}
                        placeholder="Resultado final de la atención de la denuncia…"
                    />
                    <CampoTexto
                        id="sc-observaciones"
                        etiqueta="Observaciones finales"
                        valor={formulario.observacionesFinales}
                        error={errores.observacionesFinales}
                        onCambiar={(v) => cambiarCampo('observacionesFinales', v)}
                        placeholder="Observaciones para la jefatura…"
                    />
                    <CampoTexto
                        id="sc-acciones"
                        etiqueta="Información de seguimiento y acciones realizadas"
                        valor={formulario.accionesRealizadas}
                        error={errores.accionesRealizadas}
                        onCambiar={(v) => cambiarCampo('accionesRealizadas', v)}
                        placeholder="Inspecciones, comunicaciones, verificaciones y demás acciones del caso…"
                    />

                    {errorEnvio && <div className="sc-error" role="alert">{errorEnvio}</div>}

                    <div className="sc-formulario-pie">
                        <button type="button" className="btn-secondary" onClick={onCancelar} disabled={enviando}>
                            Cancelar
                        </button>
                        <button type="submit" className="btn-primary" disabled={enviando || cargando}>
                            {enviando ? 'Enviando…' : 'Solicitar cierre'}
                        </button>
                    </div>
                </form>
            </div>

            <ModalConfirmacion
                visible={confirmando}
                titulo="Confirmar solicitud de cierre"
                mensaje={MENSAJE_CONFIRMACION}
                textoConfirmar="Sí, solicitar cierre"
                onConfirmar={confirmarSolicitud}
                onCancelar={() => setConfirmando(false)}
            />
        </div>
    );
}

function CampoTexto({ id, etiqueta, valor, error, onCambiar, placeholder }) {
    return (
        <div className="sc-campo">
            <label htmlFor={id}>{etiqueta} <span aria-hidden="true">*</span></label>
            <textarea
                id={id}
                value={valor}
                onChange={(e) => onCambiar(e.target.value)}
                maxLength={LONGITUD_MAXIMA}
                rows={3}
                placeholder={placeholder}
                aria-invalid={Boolean(error)}
                aria-describedby={error ? `${id}-error` : undefined}
            />
            <div className="sc-campo-pie">
                {error ? <span id={`${id}-error`} className="sc-campo-error">{error}</span> : <span />}
                <span className="sc-contador">{valor.length}/{LONGITUD_MAXIMA}</span>
            </div>
        </div>
    );
}
