import { useEffect, useState } from 'react';
import { AvisoToast } from '../../components/comunes/AvisoToast';
import { useAvisoToast } from '../../hooks/useAvisoToast';
import { useSesion } from '../../context/SesionContext';
import { PERMISOS } from '../../config/permisos';
import {
    obtenerEstadoSolicitudCierre,
    obtenerSolicitudesCierrePendientes,
} from '../../services/solicitudCierreService';
import { RevisionCierrePanel } from './RevisionCierrePanel';
import './SolicitudesCierrePage.css';

// HU-018: bandeja de la jefatura con las solicitudes de cierre pendientes.
// Jefe de area: las de su area; superusuario: todas.
// HU-017: desde aqui la jefatura o el superusuario abre "Revisar solicitud"
// y aprueba o rechaza (APROBAR_CIERRE; el backend valida que el jefe sea el
// del area de la solicitud).

const ETIQUETAS_ESTADO = {
    EN_TIEMPO: 'En tiempo',
    PRONTA_VENCER: 'Próxima a vencer',
    ATRASADA: 'Atrasada',
    FINALIZADA: 'Finalizada',
};

function formatearFecha(fecha) {
    try {
        return new Date(fecha).toLocaleString('es-CR', { dateStyle: 'medium', timeStyle: 'short' });
    } catch {
        return fecha;
    }
}

export function SolicitudesCierrePage() {
    const { tienePermiso } = useSesion();
    const { aviso, mostrarAviso } = useAvisoToast();
    const veTodasLasAreas = tienePermiso(PERMISOS.VER_TODAS_AREAS);
    const puedeResolver = tienePermiso(PERMISOS.APROBAR_CIERRE);

    const [solicitudes, setSolicitudes] = useState([]);
    const [cargando, setCargando] = useState(true);
    const [error, setError] = useState(null);

    // HU-017: solicitud abierta en el panel de revision y las carpetas del
    // expediente de su denuncia (llegan aparte; null mientras cargan).
    const [revisando, setRevisando] = useState(null);
    const [carpetas, setCarpetas] = useState(null);

    useEffect(() => {
        let cancelado = false;
        obtenerSolicitudesCierrePendientes()
            .then((datos) => {
                if (!cancelado) setSolicitudes(datos);
            })
            .catch((e) => {
                if (!cancelado) setError(e.message || 'No fue posible cargar las solicitudes de cierre.');
            })
            .finally(() => {
                if (!cancelado) setCargando(false);
            });
        return () => {
            cancelado = true;
        };
    }, []);

    useEffect(() => {
        if (!revisando) return undefined;

        let cancelado = false;
        obtenerEstadoSolicitudCierre(revisando.denunciaId)
            .then((estado) => {
                if (!cancelado) {
                    setCarpetas({
                        confidencial: estado?.carpetaConfidencialUrl,
                        noConfidencial: estado?.carpetaNoConfidencialUrl,
                    });
                }
            })
            .catch(() => {
                // Sin carpetas el panel muestra "sin documentos"; el resto del
                // expediente se carga igual.
                if (!cancelado) setCarpetas(null);
            });

        return () => {
            cancelado = true;
        };
    }, [revisando]);

    function abrirRevision(solicitud) {
        setCarpetas(null);
        setRevisando(solicitud);
    }

    // Tras decidir, la solicitud deja de estar pendiente: se quita de la lista.
    function cierreResuelto(resolucion) {
        const aprobada = resolucion.decision.decision === 'APROBADA';
        const estado = ETIQUETAS_ESTADO[resolucion.estadoDenuncia] ?? resolucion.estadoDenuncia;
        setSolicitudes((lista) => lista.filter((s) => s.idSolicitudCierre !== resolucion.idSolicitudCierre));
        setRevisando(null);
        mostrarAviso(aprobada
            ? `Resolución de ${revisando.consecutivo} aprobada. La denuncia pasó a estado ${estado}.`
            : `Resolución de ${revisando.consecutivo} rechazada y devuelta para ajustes. La denuncia volvió a estado ${estado}.`,
        'exito');
    }

    if (revisando) {
        return (
            <>
                <AvisoToast aviso={aviso} />
                <RevisionCierrePanel
                    denuncia={{ idDenuncia: revisando.denunciaId, consecutivo: revisando.consecutivo }}
                    solicitud={revisando}
                    carpetas={carpetas}
                    onCancelar={() => setRevisando(null)}
                    onResuelto={cierreResuelto}
                />
            </>
        );
    }

    return (
        <div className="card scp-page">
            <AvisoToast aviso={aviso} />

            <div className="card-head">
                <div>
                    <span className="section-divider-label">Jefatura</span>
                    <h3>Solicitudes de cierre pendientes</h3>
                    <div className="subtitulo">
                        {veTodasLasAreas
                            ? 'Solicitudes de cierre de todas las áreas. Revise cada solicitud para aprobar o rechazar la resolución final.'
                            : 'Denuncias de su área cuyo cierre fue solicitado. Revise cada solicitud para aprobar o rechazar la resolución final.'}
                    </div>
                </div>
                {!cargando && !error && (
                    <span className="badge-counter scp-total" aria-label={`${solicitudes.length} pendientes`}>
                        {solicitudes.length}
                    </span>
                )}
            </div>

            <div className="card-body">
                {cargando ? (
                    <div className="scp-vacio">Cargando solicitudes…</div>
                ) : error ? (
                    <div className="scp-error" role="alert">{error}</div>
                ) : solicitudes.length === 0 ? (
                    <div className="scp-vacio">No hay solicitudes de cierre pendientes.</div>
                ) : (
                    <ul className="scp-lista">
                        {solicitudes.map((s) => (
                            <li key={s.idSolicitudCierre} className="scp-item">
                                <div className="scp-item-cabecera">
                                    <div>
                                        <span className="scp-consecutivo">{s.consecutivo}</span>
                                        <span className="badge-estado en-revision">Pendiente de evaluación</span>
                                    </div>
                                    <span className="scp-fecha">Solicitada el {formatearFecha(s.fechaSolicitud)}</span>
                                </div>

                                <dl className="scp-datos">
                                    <div><dt>Solicitada por</dt><dd>{s.solicitadoPorNombre}</dd></div>
                                    <div><dt>Asignada a</dt><dd>Jefatura de {s.areaNombre}</dd></div>
                                    <div className="scp-dato-ancho"><dt>Resolución final</dt><dd>{s.resolucionFinal}</dd></div>
                                    <div className="scp-dato-ancho"><dt>Observaciones finales</dt><dd>{s.observacionesFinales}</dd></div>
                                    <div className="scp-dato-ancho">
                                        <dt>Seguimiento y acciones realizadas</dt>
                                        <dd>{s.accionesRealizadas}</dd>
                                    </div>
                                </dl>

                                {puedeResolver && (
                                    <div className="scp-item-pie">
                                        <button type="button" className="btn-primary" onClick={() => abrirRevision(s)}>
                                            Revisar solicitud
                                        </button>
                                    </div>
                                )}
                            </li>
                        ))}
                    </ul>
                )}
            </div>
        </div>
    );
}
