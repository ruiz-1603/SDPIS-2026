import { useEffect, useId, useState } from 'react';
import { listarPlazos, actualizarPlazo } from '../../services/plazosAtencionService';
import { useConsulta } from './GestionUsuarios/useConsulta';
import { ModalBase, Alerta, Campo, BotonesPie } from './GestionUsuarios/Modales';
import './GestionUsuarios/GestionUsuariosPage.css';
import './PlazosAtencionPage.css';

// HU-014: panel para que el superusuario (GESTIONAR_PLAZOS) defina los dias
// de atencion de cada nivel de criticidad. El plazo corre desde la
// notificacion al funcionario encargado. El cambio aplica a las evaluaciones
// de criticidad futuras; las fechas limite ya calculadas no se modifican.
// El backend valida todo (alto < medio y bajo, 1-365 dias) y deja cada
// cambio en el historial administrativo (categoria Plazos).
const PLAZO_MIN = 1;
const PLAZO_MAX = 365;

const NIVELES = {
    ALTO: { etiqueta: 'Alta', orden: 0 },
    MEDIO: { etiqueta: 'Media', orden: 1 },
    BAJO: { etiqueta: 'Baja', orden: 2 },
};

const etiquetaNivel = (nivel) => NIVELES[nivel]?.etiqueta ?? nivel;
const ordenNivel = (nivel) => NIVELES[nivel]?.orden ?? 99;

export function PlazosAtencionPage() {
    const [version, setVersion] = useState(0);
    const [aviso, setAviso] = useState(null);
    const [nivelEditado, setNivelEditado] = useState(null);

    const { datos: plazos, error } = useConsulta(() => listarPlazos(), [version], { mantenerAnterior: true });

    useEffect(() => {
        if (!aviso) return undefined;
        const temporizador = setTimeout(() => setAviso(null), 4500);
        return () => clearTimeout(temporizador);
    }, [aviso]);

    const ordenados = [...(plazos ?? [])].sort((a, b) => ordenNivel(a.nivel) - ordenNivel(b.nivel));

    return (
        <section className="page-gestion-usuarios gu-pagina">
            <aside className="plazos-info" aria-label="Cómo funcionan los plazos de atención">
                <span className="plazos-info__icono" aria-hidden="true">
                    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                        <circle cx="12" cy="12" r="9" /><path d="M12 11v5" /><path d="M12 7.5h.01" />
                    </svg>
                </span>
                <div className="plazos-info__cuerpo">
                    <h2 className="plazos-info__titulo">Cómo funcionan los plazos de atención</h2>
                    <ul className="plazos-info__lista">
                        <li>Defina los días máximos de atención según el nivel de criticidad.</li>
                        <li>El plazo comienza a correr desde la notificación al funcionario encargado.</li>
                        <li>El plazo de criticidad alta debe ser menor que el de media y baja.</li>
                        <li>Los cambios aplican a las evaluaciones que se calculen de ahora en adelante; las fechas límite ya calculadas no se modifican.</li>
                        <li>Cada cambio queda registrado en el historial administrativo.</li>
                    </ul>
                </div>
            </aside>

            {aviso && (
                <div className="gu-toast" role="status">
                    <span>{aviso}</span>
                    <button type="button" onClick={() => setAviso(null)} aria-label="Cerrar aviso">✕</button>
                </div>
            )}

            <div className="gu-panel">
                <Alerta>{error}</Alerta>
                {!error && plazos === null && <p className="gu-cargando">Cargando plazos…</p>}

                {plazos && (
                    <div className="gu-tabla-wrap">
                        <table className="gu-tabla tabla-tarjetas">
                            <thead>
                                <tr>
                                    <th>Nivel de criticidad</th>
                                    <th>Puntaje</th>
                                    <th>Plazo de atención</th>
                                    <th className="gu-col-acciones">Acciones</th>
                                </tr>
                            </thead>
                            <tbody>
                                {ordenados.map((p) => (
                                    <tr key={p.nivel} className={`fila-criticidad--${p.nivel.toLowerCase()}`}>
                                        <td data-label="Nivel de criticidad">
                                            <span className={`plazo-badge-nivel plazo-badge-nivel--${p.nivel.toLowerCase()}`}>
                                                {etiquetaNivel(p.nivel)}
                                            </span>
                                        </td>
                                        <td className="gu-celda-tenue" data-label="Puntaje">{p.puntajeMin} – {p.puntajeMax}</td>
                                        <td data-label="Plazo de atención"><strong>{p.plazoDias}</strong> días</td>
                                        <td className="gu-col-acciones" data-label="">
                                            <div className="gu-acciones">
                                                <button type="button" className="gu-btn gu-btn--chico gu-btn--primario"
                                                    onClick={() => setNivelEditado(p)}>
                                                    Editar plazo
                                                </button>
                                            </div>
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>
                )}
            </div>

            {nivelEditado && (
                <ModalPlazo
                    plazo={nivelEditado}
                    plazos={plazos}
                    onCerrar={() => setNivelEditado(null)}
                    onGuardado={(mensaje) => {
                        setNivelEditado(null);
                        setAviso(mensaje);
                        setVersion((v) => v + 1);
                    }}
                />
            )}
        </section>
    );
}

function ModalPlazo({ plazo, plazos, onCerrar, onGuardado }) {
    const idFormulario = useId();
    const [valor, setValor] = useState(String(plazo.plazoDias));
    const [error, setError] = useState(null);
    const [enviando, setEnviando] = useState(false);

    // Plazo vigente de los otros niveles, para validar el orden antes de enviar.
    const otro = (nivel) => plazos.find((p) => p.nivel === nivel)?.plazoDias;

    function validarOrden(numero) {
        const alto = plazo.nivel === 'ALTO' ? numero : otro('ALTO');
        const medio = plazo.nivel === 'MEDIO' ? numero : otro('MEDIO');
        const bajo = plazo.nivel === 'BAJO' ? numero : otro('BAJO');
        if (alto >= medio || alto >= bajo) {
            return 'El plazo de criticidad alta debe ser menor que el de media y baja.';
        }
        return null;
    }

    async function guardar(evento) {
        evento.preventDefault();
        const numero = Number(valor);
        if (!Number.isInteger(numero) || numero < PLAZO_MIN || numero > PLAZO_MAX) {
            setError(`Indique un número entero de días entre ${PLAZO_MIN} y ${PLAZO_MAX}.`);
            return;
        }
        if (numero === plazo.plazoDias) { onCerrar(); return; }
        const errorOrden = validarOrden(numero);
        if (errorOrden) { setError(errorOrden); return; }

        setError(null);
        setEnviando(true);
        try {
            await actualizarPlazo(plazo.nivel, numero);
            onGuardado(`Plazo de criticidad ${etiquetaNivel(plazo.nivel).toLowerCase()}: ${numero} días.`);
        } catch (e) {
            setError(e.message);
        } finally {
            setEnviando(false);
        }
    }

    return (
        <ModalBase
            titulo="Plazo de atención"
            subtitulo={
                <span className={`plazo-badge-nivel plazo-badge-nivel--${plazo.nivel.toLowerCase()}`} style={{ marginTop: '6px' }}>
                    Criticidad {etiquetaNivel(plazo.nivel)}
                </span>
            }
            onCerrar={onCerrar}
            ancho="sm"
            ocupado={enviando}
            pie={<BotonesPie formulario={idFormulario} onCancelar={onCerrar} enviando={enviando} textoConfirmar="Guardar plazo" />}
        >
            <form id={idFormulario} className="gu-formulario" onSubmit={guardar} noValidate>
                <Campo etiqueta="Días máximos de atención" obligatorio
                    ayuda={`Plazo actual: ${plazo.plazoDias} días. Valor entre ${PLAZO_MIN} y ${PLAZO_MAX}.`}>
                    <input className="gu-input gu-input--numero" type="text" inputMode="numeric"
                        pattern="[0-9]*" maxLength={String(PLAZO_MAX).length} autoComplete="off"
                        value={valor} autoFocus
                        onChange={(e) => setValor(e.target.value.replace(/\D/g, '').slice(0, String(PLAZO_MAX).length))} />
                </Campo>
                <Alerta>{error}</Alerta>
            </form>
        </ModalBase>
    );
}
