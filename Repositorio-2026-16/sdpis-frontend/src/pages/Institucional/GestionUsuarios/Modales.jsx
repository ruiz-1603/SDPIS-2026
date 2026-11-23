import { useEffect, useId, useMemo, useState } from 'react';
import { createPortal } from 'react-dom';
import {
    crearFuncionario,
    actualizarFuncionario,
    gestionarPermiso,
    gestionarAreaAdicional,
    listarDenunciasFuncionario,
    configurarMaxJefes,
    renunciarSuperusuario,
    crearCredencialTemporal,
} from '../../../services/gestionUsuariosService';
import {
    ROLES, GRUPOS_PERMISO, PERMISOS, TIPOS_AREA_CON_JEFE, CORREO_EDITABLE, MOSTRAR_CREDENCIAL_TEMPORAL,
} from '../../../config/permisos';
import {
    etiquetaRol, etiquetaPermiso, formatearFecha, numeroDenuncia, etiquetaEstadoDenuncia,
} from './utilidades';
import { BotonOrdenFecha } from '../../../components/comunes/BotonOrdenFecha';
import { SelectorBuscable } from '../../../components/comunes/SelectorBuscable';
import { compararFechas } from '../../../utils/ordenarFechas';

const PALABRA_CONFIRMACION = 'CONFIRMAR';
const CORREO_VALIDO = /^[^@\s]+@[^@\s]+\.[^@\s]+$/;

// ======================================================================
// Base: se renderiza en un portal (como InicioSesion) porque el contenedor
// de transicion de vistas anima transform y descentraria el overlay.
// ======================================================================
// variante='peligro': acciones criticas/irreversibles (borde, cabecera y pie en rojo).
export function ModalBase({ titulo, subtitulo, onCerrar, pie, ancho = 'md', ocupado = false, variante = null, children }) {
    const idTitulo = useId();

    useEffect(() => {
        function alPresionar(evento) {
            if (evento.key === 'Escape' && !ocupado) onCerrar();
        }
        document.addEventListener('keydown', alPresionar);
        return () => document.removeEventListener('keydown', alPresionar);
    }, [onCerrar, ocupado]);

    return createPortal(
        <div className="page-gestion-usuarios gu-modal-raiz">
            <div className="gu-modal-overlay" onMouseDown={() => !ocupado && onCerrar()}>
                <div
                    className={`gu-modal gu-modal--${ancho}${variante ? ` gu-modal--${variante}` : ''}`}
                    role="dialog"
                    aria-modal="true"
                    aria-labelledby={idTitulo}
                    onMouseDown={(e) => e.stopPropagation()}
                >
                    <header className="gu-modal__cabecera">
                        <div>
                            <h3 id={idTitulo}>{titulo}</h3>
                            {subtitulo && <p className="gu-modal__subtitulo">{subtitulo}</p>}
                        </div>
                        <button
                            type="button"
                            className="gu-modal__cerrar"
                            onClick={onCerrar}
                            disabled={ocupado}
                            aria-label="Cerrar"
                        >
                            ✕
                        </button>
                    </header>
                    <div className="gu-modal__cuerpo">{children}</div>
                    {pie && <footer className="gu-modal__pie">{pie}</footer>}
                </div>
            </div>
        </div>,
        document.body
    );
}

export function Alerta({ tipo = 'error', children }) {
    if (!children) return null;
    return (
        <div className={`gu-alerta gu-alerta--${tipo}`} role={tipo === 'error' ? 'alert' : 'status'}>
            {children}
        </div>
    );
}

export function Campo({ etiqueta, obligatorio, ayuda, children }) {
    return (
        <label className="gu-campo">
            <span className="gu-campo__etiqueta">
                {etiqueta} {obligatorio && <span className="gu-obligatorio" aria-hidden="true">*</span>}
            </span>
            {children}
            {ayuda && <span className="gu-campo__ayuda">{ayuda}</span>}
        </label>
    );
}

export function BotonesPie({ onCancelar, textoConfirmar, enviando, deshabilitado, formulario, peligro = false }) {
    return (
        <>
            <button type="button" className="gu-btn gu-btn--secundario" onClick={onCancelar} disabled={enviando}>
                Cancelar
            </button>
            <button
                type="submit"
                form={formulario}
                className={`gu-btn ${peligro ? 'gu-btn--peligro' : 'gu-btn--primario'}`}
                disabled={enviando || deshabilitado}
            >
                {enviando ? 'Guardando…' : textoConfirmar}
            </button>
        </>
    );
}

// Opciones de area para un rol: los jefes solo en areas rectoras con cupo.
function opcionesArea(areas, rol, areaActualId) {
    return (areas ?? []).map((a) => {
        const rectora = TIPOS_AREA_CON_JEFE.includes(a.tipo);
        const conCupo = a.jefesActivos < a.maxJefes || a.idArea === areaActualId;
        const habilitada = rol !== ROLES.JEFE_AREA || (rectora && conCupo);
        let detalle = '';
        if (rol === ROLES.JEFE_AREA) {
            detalle = rectora ? ` — jefes ${a.jefesActivos}/${a.maxJefes}` : ' — no admite jefe';
        }
        return { valor: a.idArea, texto: `${a.nombre}${detalle}`, habilitada };
    });
}

// ======================================================================
// Crear / editar funcionario
//   crear : nombre, correo, rol, area, especialidad (alta interna; luego
//           el MSI lo valida por correo institucional).
//   editar: rol (solo superusuario), estado y especialidad.
// ======================================================================
export function ModalFuncionario({ modo, funcionario, areas, areaFija, rolesPermitidos, onCerrar, onGuardado, onCredencial, onAreas }) {
    const esEdicion = modo === 'editar';
    const idFormulario = useId();

    const [nombre, setNombre] = useState(esEdicion ? funcionario.nombre : '');
    const [correo, setCorreo] = useState(esEdicion ? (funcionario.correo ?? '') : '');
    const [bajaConfirmada, setBajaConfirmada] = useState(false);
    const [rol, setRol] = useState(esEdicion ? funcionario.rol : rolesPermitidos[0]);
    const [areaId, setAreaId] = useState(esEdicion ? funcionario.areaId : (areaFija?.id ?? ''));
    const [especialidad, setEspecialidad] = useState(esEdicion ? (funcionario.especialidad ?? '') : '');
    const [activo, setActivo] = useState(esEdicion ? funcionario.activo : true);
    const [error, setError] = useState(null);
    const [enviando, setEnviando] = useState(false);

    const puedeCambiarRol = esEdicion
        ? rolesPermitidos.includes(ROLES.JEFE_AREA) && funcionario.rol !== ROLES.SUPERUSUARIO
        : rolesPermitidos.length > 1;
    const rolesEdicion = [ROLES.VALIDADOR, ROLES.JEFE_AREA];
    const opciones = useMemo(
        () => opcionesArea(areas, rol, esEdicion ? funcionario.areaId : null),
        [areas, rol, esEdicion, funcionario]
    );

    const promoviendoAJefe = esEdicion && funcionario.rol !== ROLES.JEFE_AREA && rol === ROLES.JEFE_AREA;
    const esBaja = esEdicion && funcionario.activo && !activo;
    // Un validador con denuncias activas no se puede dar de baja (el
    // backend lo valida igual; aqui se evita el intento).
    const asignadasActivas = esEdicion ? (funcionario.denunciasAsignadas ?? 0) : 0;
    const bajaBloqueada = esEdicion && funcionario.activo && asignadasActivas > 0;
    const dejandoDeSerJefe = esEdicion && funcionario.rol === ROLES.JEFE_AREA && rol !== ROLES.JEFE_AREA;

    async function guardar(evento) {
        evento.preventDefault();
        setError(null);

        try {
            setEnviando(true);
            if (esEdicion) {
                if (!nombre.trim()) { setError('Indique el nombre del funcionario.'); return; }
                if (CORREO_EDITABLE && !CORREO_VALIDO.test(correo.trim())) {
                    setError('Indique un correo institucional válido.');
                    return;
                }
                if (esBaja && !bajaConfirmada) { setError('Confirme la baja del funcionario.'); return; }

                const cambios = {};
                if (nombre.trim() !== funcionario.nombre) cambios.nombre = nombre.trim();
                if (CORREO_EDITABLE && correo.trim().toLowerCase() !== (funcionario.correo ?? '').toLowerCase()) {
                    cambios.correo = correo.trim();
                }
                if (rol !== funcionario.rol) cambios.rol = rol;
                if (activo !== funcionario.activo) cambios.activo = activo;
                if (especialidad.trim() !== (funcionario.especialidad ?? '')) cambios.especialidad = especialidad.trim();
                if (Object.keys(cambios).length === 0) {
                    setError('No hay cambios para guardar.');
                    return;
                }
                await actualizarFuncionario(funcionario.idFuncionario, cambios);
                onGuardado(esBaja
                    ? `Se dio de baja a ${funcionario.nombre}. Recuerde solicitar al administrador del MSI que deshabilite su cuenta.`
                    : `Se actualizó a ${nombre.trim()}.`);
            } else {
                if (!nombre.trim()) { setError('Indique el nombre del funcionario.'); return; }
                if (!CORREO_VALIDO.test(correo.trim())) { setError('Indique un correo institucional válido.'); return; }
                if (!areaId) { setError('Seleccione el área.'); return; }
                await crearFuncionario({
                    nombre: nombre.trim(),
                    correo: correo.trim(),
                    rol,
                    areaId: Number(areaId),
                    especialidad: especialidad.trim() || null,
                });
                onGuardado(`Se registró a ${nombre.trim()} como ${etiquetaRol(rol).toLowerCase()}.`);
            }
        } catch (e) {
            setError(e.message);
        } finally {
            setEnviando(false);
        }
    }

    return (
        <ModalBase
            titulo={esEdicion ? `Editar funcionario` : 'Nuevo funcionario'}
            subtitulo={esEdicion
                ? `${funcionario.nombre} · ${etiquetaRol(funcionario.rol)} · ${funcionario.areaNombre}`
                : 'Alta interna. Al integrar el MSI, el funcionario se validará por su correo institucional.'}
            onCerrar={onCerrar}
            ocupado={enviando}
            pie={<BotonesPie formulario={idFormulario} onCancelar={onCerrar} enviando={enviando}
                textoConfirmar={esBaja ? 'Dar de baja' : esEdicion ? 'Guardar cambios' : 'Registrar funcionario'}
                peligro={esBaja} deshabilitado={esBaja && !bajaConfirmada} />}
        >
            <form id={idFormulario} className="gu-formulario" onSubmit={guardar} noValidate>
                <Campo etiqueta="Nombre completo" obligatorio
                    ayuda={esEdicion ? 'Corrija aquí errores de digitación del registro.' : undefined}>
                    <input className="gu-input" value={nombre} maxLength={150} autoFocus={!esEdicion}
                        onChange={(e) => setNombre(e.target.value)} />
                </Campo>
                {(!esEdicion || CORREO_EDITABLE) ? (
                    <Campo etiqueta="Correo institucional" obligatorio
                        ayuda={esEdicion
                            ? 'Corrección permitida solo hasta integrar el MSI: luego el correo es la llave de enlace.'
                            : 'Debe ser único; se usará para enlazar con el MSI.'}>
                        <input className="gu-input" type="email" value={correo} maxLength={150}
                            onChange={(e) => setCorreo(e.target.value)} />
                    </Campo>
                ) : (
                    <Campo etiqueta="Correo institucional" ayuda="Lo gestiona el MSI.">
                        <div className="gu-input gu-input--fijo">{funcionario.correo ?? 'Sin correo'}</div>
                    </Campo>
                )}

                <div className="gu-formulario__fila">
                    <Campo etiqueta="Rol" obligatorio>
                        {puedeCambiarRol ? (
                            <select className="gu-input" value={rol} onChange={(e) => setRol(e.target.value)}>
                                {(esEdicion ? rolesEdicion : rolesPermitidos).map((r) => (
                                    <option key={r} value={r}>{etiquetaRol(r)}</option>
                                ))}
                            </select>
                        ) : (
                            <div className="gu-input gu-input--fijo">{etiquetaRol(rol)}</div>
                        )}
                    </Campo>

                    <Campo etiqueta="Área" obligatorio>
                        {esEdicion || areaFija ? (
                            <div className="gu-input gu-input--fijo">
                                {esEdicion ? funcionario.areaNombre : areaFija.nombre}
                            </div>
                        ) : (
                            <select className="gu-input" value={areaId} onChange={(e) => setAreaId(e.target.value)}>
                                <option value="">Seleccione…</option>
                                {opciones.map((o) => (
                                    <option key={o.valor} value={o.valor} disabled={!o.habilitada}>{o.texto}</option>
                                ))}
                            </select>
                        )}
                    </Campo>
                </div>

                <Campo etiqueta="Especialidad" ayuda="Opcional. Se usa en la asignación automática por especialidad.">
                    <input className="gu-input" value={especialidad} maxLength={100}
                        onChange={(e) => setEspecialidad(e.target.value)} />
                </Campo>

                {esEdicion && (
                    <div className="gu-interruptor">
                        <input id={`${idFormulario}-activo`} type="checkbox" checked={activo}
                            disabled={bajaBloqueada}
                            onChange={(e) => { setActivo(e.target.checked); setBajaConfirmada(false); }} />
                        <label htmlFor={`${idFormulario}-activo`}>
                            <strong>{activo ? 'Activo' : 'Inactivo'}</strong>
                            <span>
                                {bajaBloqueada
                                    ? `Tiene ${asignadasActivas} denuncia${asignadasActivas === 1 ? '' : 's'} asignada${asignadasActivas === 1 ? '' : 's'} activa${asignadasActivas === 1 ? '' : 's'}: reasígnelas antes de darlo de baja.`
                                    : 'Un funcionario inactivo no puede operar ni recibir denuncias.'}
                            </span>
                        </label>
                    </div>
                )}

                {esBaja && (
                    <>
                        <Alerta tipo="aviso">
                            <strong>Baja del funcionario.</strong> No se elimina: conserva su historial en las bitácoras.
                            Perderá todos sus permisos delegados y el de asignar (no se devuelven al reactivarlo).
                            {funcionario.rol === ROLES.JEFE_AREA && ' Si es el único jefe activo del área, el sistema no permitirá la baja.'}
                            {' '}Además, el administrador del MSI debe deshabilitar su cuenta para el SDPIS.
                        </Alerta>
                        <div className="gu-interruptor">
                            <input id={`${idFormulario}-baja`} type="checkbox" checked={bajaConfirmada}
                                onChange={(e) => setBajaConfirmada(e.target.checked)} />
                            <label htmlFor={`${idFormulario}-baja`}>
                                <strong>Confirmo la baja de {funcionario.nombre}</strong>
                                <span>La acción queda registrada en el historial administrativo.</span>
                            </label>
                        </div>
                    </>
                )}

                {rol === ROLES.SUPERUSUARIO && !esEdicion && (
                    <Alerta tipo="aviso">Solo se permiten 2 superusuarios activos al mismo tiempo.</Alerta>
                )}
                {(promoviendoAJefe || (!esEdicion && rol === ROLES.JEFE_AREA)) && (
                    <Alerta tipo="aviso">
                        Un jefe de área no puede tener denuncias asignadas: solo tiene denuncias por asignar.
                        Se respeta el cupo de jefes del área y conservará el permiso de asignar de forma permanente.
                    </Alerta>
                )}
                {dejandoDeSerJefe && (
                    <Alerta tipo="aviso">Al dejar de ser jefe pierde el permiso de asignar (se puede volver a delegar).</Alerta>
                )}

                {esEdicion && funcionario.renunciaSuperusuarioFecha && (
                    <Alerta tipo="aviso">
                        <strong>Superusuario en renuncia.</strong> Renunció el {formatearFecha(funcionario.renunciaSuperusuarioFecha)} y
                        quedó como jefe de área sin ocupar cupo. Para retirarle todo el acceso de jefatura cambie su rol a
                        Funcionario; para retirarlo del sistema, dele de baja.
                    </Alerta>
                )}

                {esEdicion && onAreas && funcionario.rol === ROLES.VALIDADOR && (
                    <p className="gu-texto-ayuda gu-texto-ayuda--chico">
                        ¿Debe atender también otras áreas?{' '}
                        <button type="button" className="gu-btn-enlace" onClick={onAreas} disabled={enviando}>
                            Administrar áreas de trabajo
                        </button>
                    </p>
                )}

                {esEdicion && MOSTRAR_CREDENCIAL_TEMPORAL && onCredencial && (
                    <p className="gu-texto-ayuda gu-texto-ayuda--chico">
                        ¿Necesita probar el ingreso de este funcionario antes del MSI?{' '}
                        <button type="button" className="gu-btn-enlace" onClick={onCredencial} disabled={enviando}>
                            Crear credencial local (provisional)
                        </button>
                    </p>
                )}

                <Alerta>{error}</Alerta>
            </form>
        </ModalBase>
    );
}

// ======================================================================
// Permisos especificos (sin heredar el rol completo)
// ======================================================================
export function ModalPermisos({ funcionario, catalogo, puedeNivelJefe, puedeNivelSuperusuario, onCerrar, onCambio }) {
    const [delegados, setDelegados] = useState(() => new Set(funcionario.permisosDelegados ?? []));
    const [permiteAsignar, setPermiteAsignar] = useState(funcionario.permiteAsignar);
    const [procesando, setProcesando] = useState(null);
    const [error, setError] = useState(null);
    const [aviso, setAviso] = useState(null);
    const [pendiente, setPendiente] = useState(null);

    const esJefe = funcionario.rol === ROLES.JEFE_AREA;

    const visibles = (catalogo ?? []).filter((p) =>
        p.nivelDelegante === ROLES.SUPERUSUARIO ? puedeNivelSuperusuario : puedeNivelJefe);

    function estadoDe(permiso) {
        if (esJefe && permiso.nivelDelegante === ROLES.JEFE_AREA) {
            return permiso.codigo === PERMISOS.ASIGNAR_DENUNCIAS ? 'permanente' : 'rol';
        }
        if (permiso.codigo === PERMISOS.ASIGNAR_DENUNCIAS) return permiteAsignar ? 'otorgado' : 'no';
        return delegados.has(permiso.codigo) ? 'otorgado' : 'no';
    }

    // Otorgar pide confirmación antes de aplicar; revocar se aplica directo.
    function solicitarCambio(permiso) {
        if (estadoDe(permiso) === 'no') {
            setError(null);
            setAviso(null);
            setPendiente(permiso);
        } else {
            alternar(permiso);
        }
    }

    async function alternar(permiso) {
        const otorgar = estadoDe(permiso) === 'no';
        setPendiente(null);
        setError(null);
        setAviso(null);
        setProcesando(permiso.codigo);
        try {
            await gestionarPermiso(funcionario.idFuncionario, permiso.codigo, otorgar ? 'OTORGAR' : 'REVOCAR');
            if (permiso.codigo === PERMISOS.ASIGNAR_DENUNCIAS) {
                setPermiteAsignar(otorgar);
            } else {
                setDelegados((actual) => {
                    const nuevo = new Set(actual);
                    if (otorgar) nuevo.add(permiso.codigo); else nuevo.delete(permiso.codigo);
                    return nuevo;
                });
            }
            setAviso(otorgar
                ? `¡Listo! ${funcionario.nombre} ya puede usar el permiso «${etiquetaPermiso(permiso.codigo)}». El cambio rige desde ahora.`
                : `Se retiró el permiso «${etiquetaPermiso(permiso.codigo)}» a ${funcionario.nombre}. El cambio rige desde ahora.`);
            onCambio();
        } catch (e) {
            setError(e.message);
        } finally {
            setProcesando(null);
        }
    }

    return (
        <ModalBase
            titulo="Permisos específicos"
            subtitulo={`${funcionario.nombre} · ${etiquetaRol(funcionario.rol)} · ${funcionario.areaNombre}`}
            onCerrar={onCerrar}
            ancho="lg"
            ocupado={Boolean(procesando)}
            pie={<button type="button" className="gu-btn gu-btn--secundario" onClick={onCerrar}
                disabled={Boolean(procesando)}>Cerrar</button>}
        >
            <p className="gu-texto-ayuda">
                Otorga permisos puntuales sin cambiar el rol: el funcionario sigue siendo {etiquetaRol(funcionario.rol).toLowerCase()} y
                conserva sus denuncias. Cada cambio queda registrado en el historial administrativo y rige de inmediato.
            </p>
            {!funcionario.activo && (
                <Alerta tipo="aviso">El funcionario está inactivo: solo se pueden revocar permisos.</Alerta>
            )}
            <Alerta>{error}</Alerta>
            <Alerta tipo="ok">{aviso}</Alerta>

            {pendiente && (
                <div className="gu-advertencia" role="alertdialog" aria-label="Confirmar permiso">
                    <span className="gu-advertencia__icono" aria-hidden="true">?</span>
                    <div>
                        <strong>
                            ¿Está seguro de otorgar el permiso «{etiquetaPermiso(pendiente.codigo)}» a {funcionario.nombre}?
                        </strong>
                        <p>El permiso rige de inmediato y el cambio queda registrado en el historial administrativo.</p>
                        <button type="button" className="gu-btn gu-btn--primario gu-btn--chico"
                            onClick={() => alternar(pendiente)}>Aceptar</button>{' '}
                        <button type="button" className="gu-btn gu-btn--secundario gu-btn--chico"
                            onClick={() => setPendiente(null)}>Cancelar</button>
                    </div>
                </div>
            )}

            {GRUPOS_PERMISO.map((grupo) => {
                const permisos = visibles.filter((p) => p.grupo === grupo.id);
                if (permisos.length === 0) return null;
                return (
                    <section key={grupo.id} className="gu-permisos-grupo">
                        <h4>{grupo.etiqueta}</h4>
                        <ul className="gu-permisos-lista">
                            {permisos.map((p) => {
                                const estado = estadoDe(p);
                                const fijo = estado === 'rol' || estado === 'permanente';
                                const otorgado = estado !== 'no';
                                return (
                                    <li key={p.codigo} className={`gu-permiso ${otorgado ? 'gu-permiso--activo' : ''}`}>
                                        <div className="gu-permiso__info">
                                            <span className="gu-permiso__nombre">
                                                {etiquetaPermiso(p.codigo)}
                                                {p.nivelDelegante === ROLES.SUPERUSUARIO && (
                                                    <span className="gu-chip gu-chip--navy">Superusuario</span>
                                                )}
                                            </span>
                                            <span className="gu-permiso__descripcion">{p.descripcion}</span>
                                        </div>
                                        {fijo ? (
                                            <span className="gu-chip gu-chip--fijo">
                                                {estado === 'permanente' ? 'Permanente del jefe' : 'Incluido por el rol'}
                                            </span>
                                        ) : (
                                            <button
                                                type="button"
                                                className={`gu-btn gu-btn--chico ${otorgado ? 'gu-btn--contorno' : 'gu-btn--primario'}`}
                                                onClick={() => solicitarCambio(p)}
                                                disabled={Boolean(procesando) || Boolean(pendiente) || (!otorgado && !funcionario.activo)}
                                                aria-label={`${otorgado ? 'Revocar' : 'Otorgar'} ${etiquetaPermiso(p.codigo)}`}
                                            >
                                                {procesando === p.codigo ? '…' : otorgado ? 'Revocar' : 'Otorgar'}
                                            </button>
                                        )}
                                    </li>
                                );
                            })}
                        </ul>
                    </section>
                );
            })}
        </ModalBase>
    );
}

// ======================================================================
// Denuncias de un funcionario
//   validador -> denuncias ASIGNADAS
//   jefe      -> denuncias POR ASIGNAR de su area (nunca tiene asignadas)
// ======================================================================
// Areas de trabajo de un funcionario: su area base (fija) mas las areas
// adicionales que decide el superusuario. Cada accion se guarda al instante.
export function ModalAreasTrabajo({ funcionario, areas, onCerrar, onCambio }) {
    const [adicionales, setAdicionales] = useState(() => funcionario.areasAdicionales ?? []);
    const [areaNueva, setAreaNueva] = useState('');
    const [procesando, setProcesando] = useState(false);
    const [error, setError] = useState(null);
    const [aviso, setAviso] = useState(null);

    const disponibles = (areas ?? []).filter((a) =>
        a.idArea !== funcionario.areaId && !adicionales.some((x) => x.idArea === a.idArea));
    const elegida = disponibles.find((a) => String(a.idArea) === String(areaNueva));

    async function cambiar(accion, area) {
        setError(null);
        setAviso(null);
        setProcesando(true);
        try {
            await gestionarAreaAdicional(funcionario.idFuncionario, Number(area.idArea), accion);
            if (accion === 'OTORGAR') {
                setAdicionales((actual) => [...actual, { idArea: area.idArea, nombre: area.nombre }]
                    .sort((a, b) => a.nombre.localeCompare(b.nombre, 'es')));
                setAreaNueva('');
                setAviso(`Ahora también trabaja en ${area.nombre}.`);
            } else {
                setAdicionales((actual) => actual.filter((x) => x.idArea !== area.idArea));
                setAviso(`Ya no trabaja en ${area.nombre}.`);
            }
            onCambio();
        } catch (e) {
            setError(e.message);
        } finally {
            setProcesando(false);
        }
    }

    return (
        <ModalBase
            titulo="Áreas de trabajo"
            subtitulo={`${funcionario.nombre} · ${etiquetaRol(funcionario.rol)}`}
            onCerrar={onCerrar}
            ocupado={procesando}
            pie={<button type="button" className="gu-btn gu-btn--secundario" onClick={onCerrar}
                disabled={procesando}>Listo</button>}
        >
            <p className="gu-texto-ayuda">
                Además de su área base, el funcionario puede atender denuncias de otras áreas. Los cambios se
                aplican al instante y quedan en el historial administrativo.
            </p>
            <Alerta>{error}</Alerta>
            <Alerta tipo="ok">{aviso}</Alerta>

            <ul className="gu-areas-trabajo">
                <li className="gu-area-fila">
                    <span className="gu-area-fila__nombre">{funcionario.areaNombre}</span>
                    <span className="gu-chip gu-chip--fijo">Área base</span>
                </li>
                {adicionales.map((a) => (
                    <li key={a.idArea} className="gu-area-fila gu-area-fila--adicional">
                        <span className="gu-area-fila__nombre">{a.nombre}</span>
                        <button type="button" className="gu-btn gu-btn--chico gu-btn--contorno"
                            onClick={() => cambiar('REVOCAR', a)} disabled={procesando}
                            aria-label={`Quitar ${a.nombre}`}>
                            Quitar
                        </button>
                    </li>
                ))}
            </ul>

            {funcionario.activo ? (
                <div className="gu-area-agregar">
                    <span className="gu-filtro__etiqueta">Agregar otra área</span>
                    <div className="gu-area-agregar__fila">
                        <div className="gu-area-agregar__selector">
                            <SelectorBuscable
                                etiqueta="Área"
                                valor={areaNueva}
                                onCambiar={setAreaNueva}
                                textoTodos="Seleccione un área"
                                placeholder=" Buscar área por nombre o código…"
                                opciones={disponibles.map((a) => ({ valor: a.idArea, texto: a.nombre, detalle: a.codigoArea }))}
                                deshabilitado={procesando}
                            />
                        </div>
                        <button type="button" className="gu-btn gu-btn--primario"
                            onClick={() => cambiar('OTORGAR', elegida)} disabled={procesando || !elegida}>
                            Agregar
                        </button>
                    </div>
                </div>
            ) : (
                <Alerta tipo="aviso">El funcionario está inactivo: solo se pueden quitar áreas.</Alerta>
            )}
        </ModalBase>
    );
}

export function ModalDenuncias({ funcionario, onCerrar }) {
    const [denuncias, setDenuncias] = useState(null);
    const [ordenFecha, setOrdenFecha] = useState('desc');
    const [error, setError] = useState(null);
    const esJefe = funcionario.rol === ROLES.JEFE_AREA;

    useEffect(() => {
        let vigente = true;
        listarDenunciasFuncionario(funcionario.idFuncionario)
            .then((lista) => { if (vigente) setDenuncias(lista); })
            .catch((e) => { if (vigente) setError(e.message); });
        return () => { vigente = false; };
    }, [funcionario.idFuncionario]);

    const denunciasOrdenadas = useMemo(() => {
        if (!denuncias) return [];
        return [...denuncias].sort((a, b) => compararFechas(a.fechaCreacion, b.fechaCreacion, ordenFecha));
    }, [denuncias, ordenFecha]);

    return (
        <ModalBase
            titulo={esJefe ? 'Denuncias por asignar' : 'Denuncias asignadas'}
            subtitulo={`${funcionario.nombre} · ${funcionario.areaNombre}`}
            onCerrar={onCerrar}
            ancho="lg"
            pie={<button type="button" className="gu-btn gu-btn--secundario" onClick={onCerrar}>Cerrar</button>}
        >
            {esJefe && (
                <p className="gu-texto-ayuda">
                    El jefe de área no lleva denuncias asignadas: estas son las denuncias de su área pendientes de asignar.
                </p>
            )}
            <Alerta>{error}</Alerta>
            {!error && denuncias === null && <p className="gu-cargando">Cargando denuncias…</p>}
            {denuncias && denuncias.length === 0 && (
                <p className="gu-vacio">{esJefe ? 'No hay denuncias pendientes de asignar.' : 'No tiene denuncias asignadas.'}</p>
            )}
            {denuncias && denuncias.length > 0 && (
                <>
                    <div style={{ display: 'flex', justifyContent: 'flex-end', marginBottom: '0.75rem' }}>
                        <BotonOrdenFecha
                            orden={ordenFecha}
                            alCambiar={setOrdenFecha}
                            etiqueta="Fecha"
                        />
                    </div>
                    <div className="gu-tabla-wrap gu-tabla-wrap--plana">
                        <table className="gu-tabla">
                            <thead>
                                <tr>
                                    <th>N.º de denuncia</th>
                                    <th>Estado</th>
                                    <th
                                        className="th-ordenable"
                                        onClick={() => setOrdenFecha(o => (o === 'asc' ? 'desc' : 'asc'))}
                                        title={`Ordenar por fecha de registro (${ordenFecha === 'asc' ? 'más recientes primero' : 'más antiguas primero'})`}
                                    >
                                        Fecha de registro {ordenFecha === 'asc' ? '↑' : '↓'}
                                    </th>
                                    <th>Área actual</th>
                                </tr>
                            </thead>
                            <tbody>
                                {denunciasOrdenadas.map((d) => (
                                    <tr key={d.idDenuncia}>
                                        <td className="gu-celda-fuerte">{numeroDenuncia(d)}</td>
                                        <td><span className="gu-estado-denuncia">{etiquetaEstadoDenuncia(d.estado)}</span></td>
                                        <td className="gu-celda-tenue">{formatearFecha(d.fechaCreacion)}</td>
                                        <td>{d.areaNombre}</td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>
                </>
            )}
            {denuncias && denuncias.length > 0 && (
                <p className="gu-resultado">{denuncias.length} denuncia{denuncias.length === 1 ? '' : 's'}</p>
            )}
        </ModalBase>
    );
}

// ======================================================================
// Cupo de jefes por area (superusuario)
// ======================================================================
export function ModalCupo({ area, onCerrar, onGuardado }) {
    const idFormulario = useId();
    const minimo = Math.max(1, area.jefesActivos);
    const [valor, setValor] = useState(String(area.maxJefes));
    const [error, setError] = useState(null);
    const [enviando, setEnviando] = useState(false);

    async function guardar(evento) {
        evento.preventDefault();
        const numero = Number(valor);
        if (!Number.isInteger(numero) || numero < minimo || numero > 999) {
            setError(`Indique un número entero entre ${minimo} y 999.`);
            return;
        }
        if (numero === area.maxJefes) { onCerrar(); return; }
        setError(null);
        setEnviando(true);
        try {
            await configurarMaxJefes(area.idArea, numero);
            onGuardado(`Cupo de jefes de ${area.nombre}: ${numero}.`);
        } catch (e) {
            setError(e.message);
        } finally {
            setEnviando(false);
        }
    }

    return (
        <ModalBase
            titulo="Cupo de jefes de área"
            subtitulo={area.nombre}
            onCerrar={onCerrar}
            ancho="sm"
            ocupado={enviando}
            pie={<BotonesPie formulario={idFormulario} onCancelar={onCerrar} enviando={enviando} textoConfirmar="Guardar cupo" />}
        >
            <form id={idFormulario} className="gu-formulario" onSubmit={guardar} noValidate>
                <Campo etiqueta="Máximo de jefes activos" obligatorio
                    ayuda={`Actualmente hay ${area.jefesActivos} jefe${area.jefesActivos === 1 ? '' : 's'} activo${area.jefesActivos === 1 ? '' : 's'}. El cupo no puede ser menor.`}>
                    <input className="gu-input gu-input--numero" type="number" min={minimo} max={999} step={1}
                        value={valor} autoFocus onChange={(e) => setValor(e.target.value)} />
                </Campo>
                <Alerta>{error}</Alerta>
            </form>
        </ModalBase>
    );
}

// ======================================================================
// Renuncia del superusuario: libera su plaza (maximo 2 superusuarios) y
// queda como jefe de area "en renuncia", sin ocupar cupo de jefes. Por
// defecto se queda en su area actual (normalmente la UVC).
// ======================================================================
export function ModalRenuncia({ areas, areaActualId, onCerrar, onConfirmado }) {
    const idFormulario = useId();
    const [palabra, setPalabra] = useState('');
    const [error, setError] = useState(null);
    const [enviando, setEnviando] = useState(false);

    const areaActual = (areas ?? []).find((a) => a.idArea === areaActualId);
    const confirmada = palabra === PALABRA_CONFIRMACION;

    async function confirmar(evento) {
        evento.preventDefault();
        if (!confirmada) return;
        setError(null);
        setEnviando(true);
        try {
            await renunciarSuperusuario();
            onConfirmado();
        } catch (e) {
            setError(e.message);
            setEnviando(false);
        }
    }

    return (
        <ModalBase
            titulo="Renunciar al rol de superusuario"
            subtitulo="Esta acción es importante y no se puede deshacer por su cuenta."
            onCerrar={onCerrar}
            ocupado={enviando}
            variante="peligro"
            pie={<BotonesPie formulario={idFormulario} onCancelar={onCerrar} enviando={enviando}
                textoConfirmar="Renunciar al rol" deshabilitado={!confirmada} peligro />}
        >
            <form id={idFormulario} className="gu-formulario" onSubmit={confirmar} noValidate>
                <div className="gu-advertencia" role="alert">
                    <span className="gu-advertencia__icono" aria-hidden="true">!</span>
                    <div>
                        <strong>Va a perder todos los privilegios de superusuario.</strong>
                        <p>
                            Dejará de administrar el sistema (áreas, cupos, permisos, plazos y reglas de criticidad)
                            y no podrá recuperar el rol por sí mismo: otro superusuario tendría que registrarlo de nuevo.
                        </p>
                    </div>
                </div>
                <p className="gu-texto-ayuda">
                    Quedará como <strong>jefe de área en renuncia</strong> en {areaActual ? <strong>{areaActual.nombre}</strong> : 'su área actual'}: conserva la jefatura de esa área, pero
                    <strong> no ocupa cupo de jefes</strong>. El otro superusuario podrá después retirarle el rol de jefe
                    o darle de baja. Debe quedar al menos otro superusuario activo y no puede tener denuncias asignadas.
                </p>
                <Campo etiqueta={`Para continuar, escriba ${PALABRA_CONFIRMACION}`} obligatorio>
                    <input className="gu-input gu-input--confirmacion" type="text" value={palabra}
                        autoComplete="off" autoCapitalize="characters" spellCheck={false}
                        placeholder={PALABRA_CONFIRMACION} aria-invalid={palabra !== '' && !confirmada}
                        onChange={(e) => setPalabra(e.target.value.trim().toUpperCase())} />
                </Campo>
                <Alerta>{error}</Alerta>
            </form>
        </ModalBase>
    );
}

// ======================================================================
// TEMPORAL - ELIMINAR AL INTEGRAR EL MSI.
// Credencial local para iniciar sesion con un funcionario recien creado.
// ======================================================================
export function ModalCredencialTemporal({ funcionario, onCerrar, onGuardado }) {
    const idFormulario = useId();
    const [datos, setDatos] = useState({ identificacion: '', nombre: '', apellido1: '', apellido2: '', clave: '' });
    const [verClave, setVerClave] = useState(false);
    const [error, setError] = useState(null);
    const [enviando, setEnviando] = useState(false);

    const cambiar = (campo) => (e) => setDatos((d) => ({ ...d, [campo]: e.target.value }));

    async function guardar(evento) {
        evento.preventDefault();
        setError(null);
        setEnviando(true);
        try {
            await crearCredencialTemporal(funcionario.idFuncionario, {
                identificacion: datos.identificacion.trim(),
                nombre: datos.nombre.trim(),
                apellido1: datos.apellido1.trim(),
                apellido2: datos.apellido2.trim() || null,
                clave: datos.clave,
            });
            onGuardado(`Credencial local creada para ${funcionario.nombre}.`);
        } catch (e) {
            setError(e.message);
        } finally {
            setEnviando(false);
        }
    }

    return (
        <ModalBase
            titulo="Credencial local (provisional)"
            subtitulo={`${funcionario.nombre} · ${funcionario.correo ?? 'sin correo'}`}
            onCerrar={onCerrar}
            ocupado={enviando}
            pie={<BotonesPie formulario={idFormulario} onCancelar={onCerrar} enviando={enviando} textoConfirmar="Crear credencial" />}
        >
            <form id={idFormulario} className="gu-formulario" onSubmit={guardar} noValidate>
                <Alerta tipo="aviso">
                    Solo para pruebas mientras se integra el MSI. Al integrarlo, las credenciales las gestionará el MSI.
                </Alerta>
                <Campo etiqueta="Cédula / identificación" obligatorio>
                    <input className="gu-input" value={datos.identificacion} maxLength={30} autoFocus
                        onChange={cambiar('identificacion')} />
                </Campo>
                <div className="gu-formulario__fila">
                    <Campo etiqueta="Nombre" obligatorio>
                        <input className="gu-input" value={datos.nombre} maxLength={150} onChange={cambiar('nombre')} />
                    </Campo>
                    <Campo etiqueta="Primer apellido" obligatorio>
                        <input className="gu-input" value={datos.apellido1} maxLength={150} onChange={cambiar('apellido1')} />
                    </Campo>
                </div>
                <Campo etiqueta="Segundo apellido">
                    <input className="gu-input" value={datos.apellido2} maxLength={150} onChange={cambiar('apellido2')} />
                </Campo>
                <Campo etiqueta="Clave temporal" obligatorio
                    ayuda="Mínimo 12 caracteres, con mayúscula, minúscula, número y símbolo.">
                    <div className="gu-clave">
                        <input className="gu-input" type={verClave ? 'text' : 'password'} value={datos.clave}
                            autoComplete="new-password" onChange={cambiar('clave')} />
                        <button type="button" className="gu-btn-enlace" onClick={() => setVerClave((v) => !v)}>
                            {verClave ? 'Ocultar' : 'Mostrar'}
                        </button>
                    </div>
                </Campo>
                <Alerta>{error}</Alerta>
            </form>
        </ModalBase>
    );
}
