import { useEffect, useMemo, useState } from 'react';
import { AvisoToast } from '../../components/comunes/AvisoToast';
import { SelectorBuscable } from '../../components/comunes/SelectorBuscable';
import { useAvisoToast } from '../../hooks/useAvisoToast';
import { useSesion } from '../../context/SesionContext';
import { PERMISOS } from '../../config/permisos';
import { listarDenunciasVisibles } from '../../services/gestionUsuariosService';
import { useConsulta } from './GestionUsuarios/useConsulta';
import {
    obtenerCriteriosTecnicos,
    registrarCriteriosTecnicos,
    actualizarCriterioTecnico,
    actualizarMetadataCriteriosTecnicos,
    obtenerAlertaSanitariaVigente,
    obtenerDetalleDenuncia,
    CRITERIO_DANO_POTENCIAL_SALUD,
    CRITERIO_ESPECIALIZADO_UVC,
} from '../../services/criteriosTecnicosService';
import './CriteriosTecnicosPage.css';

// HU-015: la sesion ya no es simulada. El funcionario y sus permisos salen
// del login (useSesion) y la lista de denuncias del filtro unico por rol
// (GET /denuncias-visibles?filtro=ASIGNADAS):
//   * funcionario   -> solo las denuncias asignadas a el;
//   * jefe/delegado -> las asignadas de su area (solo lectura);
//   * superusuario  -> las de todas las areas (puede modificar criterios).
// El backend vuelve a validar todo con el token.

const OPCIONES_DANO = ['Bajo', 'Medio', 'Alto'];
const OPCIONES_SI_NO = ['No', 'Si'];

const ETIQUETAS_CRITERIO = {
    [CRITERIO_DANO_POTENCIAL_SALUD]: 'Daño potencial a la salud',
    [CRITERIO_ESPECIALIZADO_UVC]: 'Criterio técnico especializado UVC',
};

// Plazo máximo (en días) para atender la denuncia según el nivel de criticidad
// (plazo_dias de nivel_criticidad). Es solo un respaldo de presentación: si la
// API envía criteriosActuales.plazoDias, se usa ese valor.
const PLAZO_DIAS_POR_NIVEL = { ALTO: 60, MEDIO: 90, BAJO: 120 };

const datosIniciales = {
    vinculadoComercioElectronico: '',
    danoPotencialSalud: '',
    requiereCriterioEspecializado: '',
    observacionesTecnicas: '',
};

const metadataInicial = {
    vinculadoComercioElectronico: '',
    observacionesTecnicas: '',
};

const numeroDenuncia = (d) => d.consecutivo ?? `#${d.idDenuncia}`;

// onDenunciaSeleccionada: el portal reduce el menu lateral al elegir una
// denuncia, para dar mas ancho al detalle (el usuario puede volver a ampliarlo).
export function CriteriosTecnicosPage({ onDenunciaSeleccionada }) {
    const { aviso, mostrarAviso } = useAvisoToast();
    const { sesion, tienePermiso } = useSesion();

    // Usuario autenticado (HU-001) y lo que puede hacer (HU-015).
    const miFuncionarioId = sesion?.usuario?.funcionarioId ?? null;
    const esSuperusuario = tienePermiso(PERMISOS.ADMINISTRAR_SISTEMA);
    const veTodasAreas = tienePermiso(PERMISOS.VER_TODAS_AREAS);
    const veDenunciasDeOtros = veTodasAreas || tienePermiso(PERMISOS.VER_DENUNCIAS_AREA);
    const puedeRegistrarCriterios = tienePermiso(PERMISOS.REGISTRAR_CRITERIOS);

    const [filtroArea, setFiltroArea] = useState('');
    const [filtroFuncionario, setFiltroFuncionario] = useState('');
    const [filtroEstado, setFiltroEstado] = useState('');
    const [denunciaSeleccionada, setDenunciaSeleccionada] = useState(null);

    const [criteriosActuales, setCriteriosActuales] = useState(null); // null = aun no registrados
    const [cargandoCriterios, setCargandoCriterios] = useState(false);
    const [alertaVigente, setAlertaVigente] = useState(null);
    const [detalleDenuncia, setDetalleDenuncia] = useState(null);

    // Formulario de registro inicial (solo cuando no existe evaluacion aun).
    const [datos, setDatos] = useState(datosIniciales);
    const [errores, setErrores] = useState({});
    const [enviando, setEnviando] = useState(false);

    // Edicion de metadata (comercio electronico/observaciones), aparte
    // de los criterios individuales -- correccion de alcance HU-010.
    const [editandoMetadata, setEditandoMetadata] = useState(false);
    const [metadataDatos, setMetadataDatos] = useState(metadataInicial);
    const [guardandoMetadata, setGuardandoMetadata] = useState(false);

    // Edicion de UN criterio individual a la vez.
    const [criterioEnEdicion, setCriterioEnEdicion] = useState(null);
    const [valorEdicionCriterio, setValorEdicionCriterio] = useState('');
    const [guardandoCriterio, setGuardandoCriterio] = useState(false);

    // Panel de justificacion obligatorio: solo aplica cuando se esta
    // EDITANDO un valor que el funcionario validador ya habia registrado
    // (boton "Editar"). Si el criterio no tenia valor previo (boton
    // "Agregar", evaluaciones incompletas de cuando si se permitia eliminar
    // criterios), no se pide justificacion.
    const [esEdicionCriterio, setEsEdicionCriterio] = useState(false);
    const [justificacionCriterio, setJustificacionCriterio] = useState('');
    const [errorJustificacionCriterio, setErrorJustificacionCriterio] = useState('');
    const [justificacionMetadata, setJustificacionMetadata] = useState('');
    const [errorJustificacionMetadata, setErrorJustificacionMetadata] = useState('');

    // Lista segun el rol de la sesion (sin area fija ni sesion simulada).
    const { datos: denunciasDatos, error: errorLista, cargando: cargandoLista } = useConsulta(
        () => listarDenunciasVisibles({
            filtro: 'ASIGNADAS',
            criterios: esSuperusuario ? 'REGISTRADOS' : undefined,
        }),
        [miFuncionarioId, esSuperusuario]
    );
    const denuncias = useMemo(() => denunciasDatos ?? [], [denunciasDatos]);

    useEffect(() => {
        if (!denunciaSeleccionada) return undefined;

        let cancelado = false;
        setCargandoCriterios(true);
        setAlertaVigente(null);
        setDetalleDenuncia(null);
        setEditandoMetadata(false);
        setCriterioEnEdicion(null);

        // El detalle de la denuncia es informativo: si falla se avisa, pero no
        // debe impedir registrar los criterios tecnicos.
        Promise.all([
            obtenerCriteriosTecnicos(denunciaSeleccionada.idDenuncia),
            obtenerAlertaSanitariaVigente(denunciaSeleccionada.idDenuncia).catch(() => null),
            obtenerDetalleDenuncia(denunciaSeleccionada.idDenuncia).catch((err) => {
                if (!cancelado) mostrarAviso(err.message || 'No fue posible consultar la informacion de la denuncia.', 'error');
                return null;
            }),
        ])
            .then(([criterios, alerta, detalle]) => {
                if (cancelado) return;
                setCriteriosActuales(criterios);
                setAlertaVigente(alerta);
                setDetalleDenuncia(detalle);
                setDatos(datosIniciales);
                setErrores({});
            })
            .catch((err) => {
                if (!cancelado) mostrarAviso(err.message || 'No fue posible consultar los criterios tecnicos.', 'error');
            })
            .finally(() => {
                if (!cancelado) setCargandoCriterios(false);
            });

        return () => {
            cancelado = true;
        };
    }, [denunciaSeleccionada, mostrarAviso]);

    // Opciones de filtro tomadas de las propias denuncias visibles.
    const opcionesArea = useMemo(() => valoresUnicos(denuncias, (d) => d.areaActualId, (d) => d.areaNombre), [denuncias]);
    const opcionesFuncionario = useMemo(
        () => valoresUnicos(denuncias.filter((d) => !filtroArea || String(d.areaActualId) === filtroArea),
            (d) => d.funcionarioAsignadoId, (d) => d.funcionarioAsignadoNombre),
        [denuncias, filtroArea]
    );
    const opcionesEstado = useMemo(
        () => valoresUnicos(denuncias, (d) => d.estado, (d) => ESTADOS_DENUNCIA[d.estado] ?? d.estado),
        [denuncias]
    );

    const denunciasFiltradas = useMemo(() => denuncias.filter((d) =>
        (!filtroArea || String(d.areaActualId) === filtroArea)
        && (!filtroFuncionario || String(d.funcionarioAsignadoId) === filtroFuncionario)
        && (!filtroEstado || d.estado === filtroEstado)),
    [denuncias, filtroArea, filtroFuncionario, filtroEstado]);

    // Opciones del selector de denuncia (con buscador). Si la denuncia abierta
    // quedo fuera de los filtros, se agrega para que el selector la muestre.
    const opcionesDenuncia = useMemo(() => {
        const opciones = denunciasFiltradas.map((d) => ({
            valor: d.idDenuncia,
            texto: numeroDenuncia(d),
            detalle: [
                veDenunciasDeOtros ? d.funcionarioAsignadoNombre : (ESTADOS_DENUNCIA[d.estado] ?? d.estado),
                veTodasAreas && !filtroArea ? d.areaNombre : null,
            ].filter(Boolean).join(' · '),
        }));
        if (denunciaSeleccionada && !opciones.some((o) => o.valor === denunciaSeleccionada.idDenuncia)) {
            opciones.unshift({ valor: denunciaSeleccionada.idDenuncia, texto: numeroDenuncia(denunciaSeleccionada) });
        }
        return opciones;
    }, [denunciasFiltradas, denunciaSeleccionada, veDenunciasDeOtros, veTodasAreas, filtroArea]);

    function elegirDenuncia(valor) {
        const denuncia = denuncias.find((d) => String(d.idDenuncia) === valor);
        if (denuncia) seleccionar(denuncia);
    }

    const yaRegistrado = !!criteriosActuales;

    // Registrar: el funcionario asignado a la denuncia (con permiso) o el
    // superusuario. Modificar lo ya registrado: solo superusuario. El resto
    // (jefe de area, delegados de supervision) consulta en solo lectura.
    // Registrar: solo el funcionario asignado a la denuncia (con permiso).
    // Modificar lo ya registrado: solo superusuario. El resto (jefe de area,
    // delegados de supervision) consulta en solo lectura.
    const puedeRegistrarSeleccionada = !!denunciaSeleccionada
        && puedeRegistrarCriterios && denunciaSeleccionada.funcionarioAsignadoId === miFuncionarioId;
    const soloLectura = yaRegistrado
        ? !esSuperusuario && !puedeRegistrarSeleccionada
        : !puedeRegistrarSeleccionada;

    const etiquetaAlcance = veTodasAreas
        ? (opcionesArea.find((a) => String(a.valor) === filtroArea)?.texto ?? 'Todas las áreas')
        : (denuncias[0]?.areaNombre ?? 'Ministerio de Salud');

    function seleccionar(denuncia) {
        if (denuncia.idDenuncia === denunciaSeleccionada?.idDenuncia) return;
        setDenunciaSeleccionada(denuncia);
        onDenunciaSeleccionada?.();
        setCriteriosActuales(null);
        setDatos(datosIniciales);
        setErrores({});
    }

    function onChange(campo, valor) {
        setDatos((prev) => ({ ...prev, [campo]: valor }));
        setErrores((prev) => {
            if (!(campo in prev)) return prev;
            const restantes = { ...prev };
            delete restantes[campo];
            return restantes;
        });
    }

    function validar() {
        const nuevos = {};
        if (datos.vinculadoComercioElectronico !== 'S' && datos.vinculadoComercioElectronico !== 'N') {
            nuevos.vinculadoComercioElectronico = 'Debe indicar si la denuncia esta vinculada a comercio electronico.';
        }
        if (!datos.danoPotencialSalud) {
            nuevos.danoPotencialSalud = 'Debe seleccionar un nivel de dano potencial a la salud.';
        }
        if (!datos.requiereCriterioEspecializado) {
            nuevos.requiereCriterioEspecializado = 'Debe indicar si requiere criterio tecnico especializado de UVC.';
        }
        return nuevos;
    }

    // Registro inicial (POST): solo aplica mientras no existe evaluacion.
    // Una vez registrada, toda modificacion pasa por los handlers de
    // criterio individual / metadata de mas abajo.
    async function manejarRegistro(e) {
        e.preventDefault();
        if (!denunciaSeleccionada) return;

        const erroresFormulario = validar();
        setErrores(erroresFormulario);
        if (Object.keys(erroresFormulario).length > 0) {
            mostrarAviso('Revise los campos marcados en rojo.', 'error');
            return;
        }

        setEnviando(true);
        try {
            const resultado = await registrarCriteriosTecnicos(denunciaSeleccionada.idDenuncia, datos);

            const criterios = await obtenerCriteriosTecnicos(denunciaSeleccionada.idDenuncia);
            setCriteriosActuales(criterios);

            mostrarAviso(
                `Criterios tecnicos registrados correctamente. Puntaje total: ${resultado.puntajeTotal}. ` +
                `Nivel de criticidad: ${resultado.nivelCriticidad}.`,
                'exito'
            );
        } catch (err) {
            mostrarAviso(err.message || 'No fue posible registrar los criterios tecnicos.', 'error');
        } finally {
            setEnviando(false);
        }
    }

    function iniciarEdicionMetadata() {
        setMetadataDatos({
            vinculadoComercioElectronico: criteriosActuales.vinculadoComercioElectronico,
            observacionesTecnicas: criteriosActuales.observacionesTecnicas || '',
        });
        setJustificacionMetadata('');
        setErrorJustificacionMetadata('');
        setEditandoMetadata(true);
    }

    async function guardarMetadata(e) {
        e.preventDefault();
        if (metadataDatos.vinculadoComercioElectronico !== 'S' && metadataDatos.vinculadoComercioElectronico !== 'N') {
            mostrarAviso('Debe indicar si la denuncia esta vinculada a comercio electronico.', 'error');
            return;
        }

        const cambioComercio = criteriosActuales.vinculadoComercioElectronico
            && criteriosActuales.vinculadoComercioElectronico !== metadataDatos.vinculadoComercioElectronico;

        if (cambioComercio && justificacionMetadata.trim().length < 10) {
            setErrorJustificacionMetadata('Debe justificar el cambio (mínimo 10 caracteres) para poder guardarlo.');
            return;
        }

        setGuardandoMetadata(true);
        try {
            await actualizarMetadataCriteriosTecnicos(denunciaSeleccionada.idDenuncia, {
                ...metadataDatos,
                justificacion: cambioComercio ? justificacionMetadata.trim() : null,
            });

            setCriteriosActuales((prev) => ({ ...prev, ...metadataDatos }));
            setEditandoMetadata(false);
            mostrarAviso('Datos generales actualizados correctamente. El cambio quedó registrado en la bitácora.', 'exito');
        } catch (err) {
            mostrarAviso(err.message || 'No fue posible actualizar los datos generales.', 'error');
        } finally {
            setGuardandoMetadata(false);
        }
    }

    function iniciarEdicionCriterio(criterio, valorActual) {
        setCriterioEnEdicion(criterio);
        setValorEdicionCriterio(valorActual || '');
        setEsEdicionCriterio(!!valorActual);
        setJustificacionCriterio('');
        setErrorJustificacionCriterio('');
    }

    function cancelarEdicionCriterio() {
        setCriterioEnEdicion(null);
        setJustificacionCriterio('');
        setErrorJustificacionCriterio('');
    }

    function campoDelCriterio(criterio) {
        return criterio === CRITERIO_DANO_POTENCIAL_SALUD ? 'danoPotencialSalud' : 'requiereCriterioEspecializado';
    }

    async function guardarCriterio(criterio) {
        if (!valorEdicionCriterio) {
            mostrarAviso('Debe seleccionar un valor para el criterio.', 'error');
            return;
        }

        // Sin cambio real no se llama al backend: guardar el mismo valor no
        // debe reiniciar el plazo de la denuncia.
        if (valorEdicionCriterio === criteriosActuales[campoDelCriterio(criterio)]) {
            cancelarEdicionCriterio();
            mostrarAviso(`"${ETIQUETAS_CRITERIO[criterio]}" no cambió: el plazo se mantiene.`, 'aviso');
            return;
        }

        // Panel de justificacion obligatorio: no se permite guardar la edicion
        // de un valor ya registrado sin justificacion (no aplica al "Agregar").
        if (esEdicionCriterio && justificacionCriterio.trim().length < 10) {
            setErrorJustificacionCriterio('Debe justificar el cambio (mínimo 10 caracteres) para poder guardarlo.');
            return;
        }

        setGuardandoCriterio(true);
        try {
            const resultado = await actualizarCriterioTecnico(denunciaSeleccionada.idDenuncia, criterio, {
                valorSeleccionado: valorEdicionCriterio,
                justificacion: esEdicionCriterio ? justificacionCriterio.trim() : null,
            });

            // HU-011: el puntaje puede cambiar el nivel de criticidad calculado
            // automaticamente -- se relee en vez de parchear localmente, para no
            // mostrar un nivel/plazo desactualizado.
            const criteriosFrescos = await obtenerCriteriosTecnicos(denunciaSeleccionada.idDenuncia);
            setCriteriosActuales(criteriosFrescos);
            cancelarEdicionCriterio();

            // Acordado con el PO: modificar un criterio reinicia el plazo desde
            // hoy segun el nuevo nivel (queda registrado en bitacora).
            const avisoPlazo = criteriosFrescos?.fechaLimite
                ? ` Plazo reiniciado: nueva fecha límite ${new Date(criteriosFrescos.fechaLimite).toLocaleDateString('es-CR')}.`
                : '';
            mostrarAviso(
                `"${ETIQUETAS_CRITERIO[criterio]}" actualizado. Puntaje total: ${resultado.puntajeTotal}.${avisoPlazo}`,
                'exito'
            );
        } catch (err) {
            mostrarAviso(err.message || 'No fue posible actualizar el criterio.', 'error');
        } finally {
            setGuardandoCriterio(false);
        }
    }

    function renderFilaCriterio(criterio, opciones, formatearValor) {
        const valorActual = criteriosActuales[campoDelCriterio(criterio)];
        const enEdicion = criterioEnEdicion === criterio;

        return (
            <div className="ct-criterio-fila" key={criterio}>
                <div className="ct-criterio-info">
                    <span className="ct-criterio-nombre">{ETIQUETAS_CRITERIO[criterio]}</span>
                    {enEdicion ? (
                        <select value={valorEdicionCriterio} onChange={(e) => setValorEdicionCriterio(e.target.value)}>
                            <option value="">Seleccione…</option>
                            {opciones.map((v) => (
                                <option key={v} value={v}>{formatearValor(v)}</option>
                            ))}
                        </select>
                    ) : (
                        <span className="ct-criterio-valor">
                            {valorActual ? formatearValor(valorActual) : 'No registrado'}
                        </span>
                    )}
                </div>

                {esSuperusuario && (
                    <div className="ct-criterio-acciones">
                        {enEdicion ? (
                            <>
                                <button type="button" className="btn-secondary" onClick={cancelarEdicionCriterio} disabled={guardandoCriterio}>
                                    Cancelar
                                </button>
                                <button type="button" className="btn-primary" onClick={() => guardarCriterio(criterio)} disabled={guardandoCriterio}>
                                    {guardandoCriterio ? 'Guardando…' : 'Guardar'}
                                </button>
                            </>
                        ) : (
                            // Sin opcion de eliminar: una denuncia siempre debe quedar con
                            // sus criterios registrados. "Agregar" solo aparece en
                            // evaluaciones antiguas a las que se les elimino un criterio
                            // antes de este cambio, para poder completarlas.
                            <button type="button" className="btn-enlace" onClick={() => iniciarEdicionCriterio(criterio, valorActual)}>
                                {valorActual ? 'Editar' : 'Agregar'}
                            </button>
                        )}
                    </div>
                )}

                {/* Panel de justificacion obligatorio: solo al EDITAR un valor ya
            registrado por el funcionario validador, nunca al "Agregar". */}
                {enEdicion && esEdicionCriterio && (
                    <div className="field ct-criterio-justificacion">
                        <label htmlFor={`ct-justificacion-${criterio}`}>
                            Justificación del cambio<span className="req">*</span>
                        </label>
                        <textarea
                            id={`ct-justificacion-${criterio}`}
                            rows={2}
                            maxLength={1000}
                            placeholder="Explique por qué se modifica este criterio ya registrado…"
                            value={justificacionCriterio}
                            onChange={(e) => {
                                setJustificacionCriterio(e.target.value);
                                if (errorJustificacionCriterio) setErrorJustificacionCriterio('');
                            }}
                            disabled={guardandoCriterio}
                        />
                        {errorJustificacionCriterio && (
                            <div className="error-msg" role="alert">{errorJustificacionCriterio}</div>
                        )}
                    </div>
                )}
            </div>
        );
    }

    return (
        <div className="ct-layout">
            <aside className="ct-nota">
                <span className="ct-nota__icono" aria-hidden="true">
                    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                        <circle cx="12" cy="12" r="9" /><path d="M12 11v5" /><path d="M12 7.5h.01" />
                    </svg>
                </span>
                <p>
                    Registre los criterios técnicos de cada denuncia: relación con comercio electrónico, criterio
                    especializado de UVC y nivel de daño potencial a la salud. Todos son obligatorios y el sistema
                    los usa para calcular automáticamente la criticidad. Las observaciones técnicas son opcionales.
                </p>
            </aside>

            <section className="card ct-selector" aria-label="Seleccionar denuncia">
                <div className="ct-selector-cabecera">
                    <div>
                        <span className="section-divider-label">{etiquetaAlcance}</span>
                        <h3>
                            {esSuperusuario
                                ? 'Denuncias con criterios registrados'
                                : veDenunciasDeOtros ? 'Denuncias asignadas' : 'Mis denuncias asignadas'}
                        </h3>
                    </div>
                    <div className="subtitulo">
                        {cargandoLista
                            ? 'Cargando…'
                            : `${denunciasFiltradas.length} de ${denuncias.length} denuncia${denuncias.length === 1 ? '' : 's'}`}
                    </div>
                </div>

                {/* Filtros y selector en una fila: solo se muestran los que aplican al rol. */}
                <div className="ct-selector-fila">
                    {veTodasAreas && (
                        <div className="ct-filtro">
                            <span>Área</span>
                            <SelectorBuscable
                                etiqueta="Área"
                                valor={filtroArea}
                                onCambiar={(v) => { setFiltroArea(v); setFiltroFuncionario(''); }}
                                textoTodos="Todas las áreas"
                                placeholder=" Buscar área…"
                                opciones={opcionesArea}
                            />
                        </div>
                    )}
                    {veDenunciasDeOtros && (
                        <div className="ct-filtro">
                            <span>Funcionario</span>
                            <SelectorBuscable
                                etiqueta="Funcionario"
                                valor={filtroFuncionario}
                                onCambiar={setFiltroFuncionario}
                                textoTodos="Todos los funcionarios"
                                placeholder=" Buscar funcionario por nombre…"
                                opciones={opcionesFuncionario}
                            />
                        </div>
                    )}
                    {(veTodasAreas || veDenunciasDeOtros || opcionesEstado.length > 1) && (
                        <label className="ct-filtro">
                            <span>Estado</span>
                            <select value={filtroEstado} onChange={(e) => setFiltroEstado(e.target.value)}>
                                <option value="">Todos</option>
                                {opcionesEstado.map((o) => <option key={o.valor} value={o.valor}>{o.texto}</option>)}
                            </select>
                        </label>
                    )}
                    <div className="ct-filtro ct-filtro--denuncia">
                        <span>Denuncia</span>
                        <SelectorBuscable
                            etiqueta="Denuncia"
                            valor={denunciaSeleccionada ? String(denunciaSeleccionada.idDenuncia) : ''}
                            onCambiar={elegirDenuncia}
                            placeholder="Buscar por N.º de denuncia…"
                            opciones={opcionesDenuncia}
                            deshabilitado={cargandoLista || opcionesDenuncia.length === 0}
                        />
                    </div>
                </div>

                {errorLista ? (
                    <p className="ct-selector-mensaje" role="alert">{errorLista}</p>
                ) : !cargandoLista && denunciasFiltradas.length === 0 && (
                    <p className="ct-selector-mensaje">
                        {denuncias.length === 0
                            ? (esSuperusuario ? 'No hay denuncias con criterios registrados.' : 'No hay denuncias asignadas.')
                            : 'Ninguna denuncia coincide con los filtros.'}
                    </p>
                )}
            </section>

            <section className="ct-detalle" aria-label="Registro de criterios técnicos">
                {!denunciaSeleccionada ? (
                    <div className="card">
                        <div className="card-body ct-vacio">
                            {esSuperusuario
                                ? 'Seleccione una denuncia para revisar o modificar sus criterios técnicos.'
                                : 'Seleccione una denuncia para registrar sus criterios técnicos.'}
                        </div>                    </div>
                ) : (
                    <div className="card ct-detalle-card">
                        <div className="card-head">
                            <div>
                                <span className="section-divider-label">{numeroDenuncia(denunciaSeleccionada)}</span>
                                <h3>Criterios técnicos de análisis</h3>
                                <div className="subtitulo">
                                    {yaRegistrado
                                        ? `Registrados el ${new Date(criteriosActuales.fechaEvaluacion).toLocaleDateString('es-CR')}`
                                        : 'Aún no se han registrado criterios técnicos para esta denuncia.'}
                                </div>
                            </div>
                        </div>

                        <div className="card-body">
                            {!cargandoCriterios && detalleDenuncia && <DetalleDenuncia detalle={detalleDenuncia} />}

                            {alertaVigente?.tieneAlertaVigente && (
                                <div className="banner-info ct-alerta-vigente">
                                    <span>
                                        Al menos un producto de esta denuncia tiene una alerta sanitaria vigente
                                        ({alertaVigente.numerosAlerta.join(', ')}). Considérelo al definir el daño potencial a la salud.
                                    </span>
                                </div>
                            )}

                            {cargandoCriterios ? (
                                <div className="ct-vacio">Cargando criterios técnicos…</div>
                            ) : soloLectura ? (
                                // Rama de UI completamente separada del formulario de edicion:
                                // no comparte handlers de guardar/agregar con el resto, asi que
                                // quien no puede registrar ni modificar (jefe de area, delegados
                                // de supervision) solo consulta.
                                <CriteriosSoloLectura criteriosActuales={criteriosActuales} />
                            ) : !yaRegistrado ? (
                                <form id="ct-form" onSubmit={manejarRegistro} noValidate>
                                    <div className="field">
                                        <label htmlFor="ct-comercio">
                                            ¿Relación con comercio electrónico?<span className="req">*</span>
                                        </label>
                                        <select
                                            id="ct-comercio"
                                            value={datos.vinculadoComercioElectronico}
                                            onChange={(e) => onChange('vinculadoComercioElectronico', e.target.value)}
                                            aria-invalid={!!errores.vinculadoComercioElectronico}
                                        >
                                            <option value="">Seleccione…</option>
                                            <option value="S">Sí</option>
                                            <option value="N">No</option>
                                        </select>
                                        {errores.vinculadoComercioElectronico && (
                                            <div className="error-msg" role="alert">{errores.vinculadoComercioElectronico}</div>
                                        )}
                                    </div>

                                    <div className="field">
                                        <label htmlFor="ct-especializado">
                                            ¿Requiere criterio técnico especializado de UVC?<span className="req">*</span>
                                        </label>
                                        <select
                                            id="ct-especializado"
                                            value={datos.requiereCriterioEspecializado}
                                            onChange={(e) => onChange('requiereCriterioEspecializado', e.target.value)}
                                            aria-invalid={!!errores.requiereCriterioEspecializado}
                                        >
                                            <option value="">Seleccione…</option>
                                            {OPCIONES_SI_NO.map((v) => (
                                                <option key={v} value={v}>{v === 'Si' ? 'Sí' : v}</option>
                                            ))}
                                        </select>
                                        {errores.requiereCriterioEspecializado && (
                                            <div className="error-msg" role="alert">{errores.requiereCriterioEspecializado}</div>
                                        )}
                                    </div>

                                    <div className="field">
                                        <label htmlFor="ct-dano">
                                            Nivel de daño potencial a la salud<span className="req">*</span>
                                        </label>
                                        <select
                                            id="ct-dano"
                                            value={datos.danoPotencialSalud}
                                            onChange={(e) => onChange('danoPotencialSalud', e.target.value)}
                                            aria-invalid={!!errores.danoPotencialSalud}
                                        >
                                            <option value="">Seleccione…</option>
                                            {OPCIONES_DANO.map((v) => (
                                                <option key={v} value={v}>{v}</option>
                                            ))}
                                        </select>
                                        {errores.danoPotencialSalud && (
                                            <div className="error-msg" role="alert">{errores.danoPotencialSalud}</div>
                                        )}
                                    </div>

                                    <div className="field">
                                        <label htmlFor="ct-observaciones">
                                            Observaciones técnicas<span className="opt">(opcional)</span>
                                        </label>
                                        <textarea
                                            id="ct-observaciones"
                                            rows={4}
                                            maxLength={1000}
                                            value={datos.observacionesTecnicas}
                                            onChange={(e) => onChange('observacionesTecnicas', e.target.value)}
                                        />
                                    </div>
                                </form>
                            ) : (
                                <>
                                    <div className="ct-metadata-section">
                                        <div className="ct-metadata-header">
                                            <h4>Datos generales</h4>
                                            {esSuperusuario && !editandoMetadata && (
                                                <button type="button" className="btn-enlace" onClick={iniciarEdicionMetadata}>Editar</button>
                                            )}
                                        </div>

                                        {editandoMetadata ? (
                                            <form id="ct-metadata-form" onSubmit={guardarMetadata} noValidate>
                                                <div className="field">
                                                    <label htmlFor="ct-meta-comercio">
                                                        ¿Relación con comercio electrónico?<span className="req">*</span>
                                                    </label>
                                                    <select
                                                        id="ct-meta-comercio"
                                                        value={metadataDatos.vinculadoComercioElectronico}
                                                        onChange={(e) => setMetadataDatos((p) => ({ ...p, vinculadoComercioElectronico: e.target.value }))}
                                                    >
                                                        <option value="">Seleccione…</option>
                                                        <option value="S">Sí</option>
                                                        <option value="N">No</option>
                                                    </select>
                                                </div>
                                                {criteriosActuales.vinculadoComercioElectronico
                                                    && criteriosActuales.vinculadoComercioElectronico !== metadataDatos.vinculadoComercioElectronico && (
                                                        <div className="field ct-criterio-justificacion">
                                                            <label htmlFor="ct-meta-justificacion">
                                                                Justificación del cambio<span className="req">*</span>
                                                            </label>
                                                            <textarea
                                                                id="ct-meta-justificacion"
                                                                rows={3}
                                                                value={justificacionMetadata}
                                                                onChange={(e) => {
                                                                    setJustificacionMetadata(e.target.value);
                                                                    if (errorJustificacionMetadata) setErrorJustificacionMetadata('');
                                                                }}
                                                            />
                                                            {errorJustificacionMetadata && (
                                                                <div className="error-msg" role="alert">{errorJustificacionMetadata}</div>
                                                            )}
                                                        </div>
                                                    )}


                                                <div className="field">
                                                    <label htmlFor="ct-meta-observaciones">
                                                        Observaciones técnicas<span className="opt">(opcional)</span>
                                                    </label>
                                                    <textarea
                                                        id="ct-meta-observaciones"
                                                        rows={3}
                                                        maxLength={1000}
                                                        value={metadataDatos.observacionesTecnicas}
                                                        onChange={(e) => setMetadataDatos((p) => ({ ...p, observacionesTecnicas: e.target.value }))}
                                                    />
                                                </div>

                                                <div className="ct-metadata-acciones">
                                                    <button type="button" className="btn-secondary" onClick={() => setEditandoMetadata(false)} disabled={guardandoMetadata}>
                                                        Cancelar
                                                    </button>
                                                    <button type="submit" className="btn-primary" disabled={guardandoMetadata}>
                                                        {guardandoMetadata ? 'Guardando…' : 'Guardar'}
                                                    </button>
                                                </div>
                                            </form>
                                        ) : (
                                            <dl className="ct-metadata-resumen">
                                                <div>
                                                    <dt>Relación con comercio electrónico</dt>
                                                    <dd>{criteriosActuales.vinculadoComercioElectronico === 'S' ? 'Sí' : 'No'}</dd>
                                                </div>
                                                <div>
                                                    <dt>Observaciones técnicas</dt>
                                                    <dd>{criteriosActuales.observacionesTecnicas || '—'}</dd>
                                                </div>
                                            </dl>
                                        )}
                                    </div>

                                    <ResumenCriticidad criteriosActuales={criteriosActuales} />

                                    <div className="ct-criterios-lista">
                                        {renderFilaCriterio(CRITERIO_DANO_POTENCIAL_SALUD, OPCIONES_DANO, (v) => v)}
                                        {renderFilaCriterio(CRITERIO_ESPECIALIZADO_UVC, OPCIONES_SI_NO, (v) => (v === 'Si' ? 'Sí' : v))}
                                    </div>

                                    {!esSuperusuario && (
                                        <div className="banner-info ct-solo-lectura">
                                            <span>Solo un superusuario puede modificar los criterios técnicos ya registrados.</span>
                                        </div>
                                    )}
                                </>
                            )}
                        </div>

                        {!yaRegistrado && !cargandoCriterios && !soloLectura && (
                            <div className="panel-lateral-footer ct-acciones">
                                <button type="submit" form="ct-form" className="btn-primary" disabled={enviando}>
                                    {enviando ? 'Guardando…' : 'Registrar criterios'}
                                </button>
                            </div>
                        )}
                    </div>
                )}
            </section>

            <AvisoToast aviso={aviso} />
        </div>
    );
}

// [{ valor, texto }] sin repetidos y ordenado por texto (para los filtros).
function valoresUnicos(lista, obtenerValor, obtenerTexto) {
    const mapa = new Map();
    lista.forEach((item) => {
        const valor = obtenerValor(item);
        if (valor !== null && valor !== undefined && !mapa.has(valor)) {
            mapa.set(valor, obtenerTexto(item) ?? String(valor));
        }
    });
    return [...mapa.entries()]
        .map(([valor, texto]) => ({ valor, texto }))
        .sort((a, b) => a.texto.localeCompare(b.texto, 'es'));
}

function CriteriosSoloLectura({ criteriosActuales }) {
    if (!criteriosActuales) {
        return <div className="ct-vacio">Aún no se han registrado criterios técnicos para esta denuncia.</div>;
    }

    return (
        <>
            <div className="ct-metadata-section">
                <div className="ct-metadata-header">
                    <h4>Datos generales</h4>
                </div>
                <dl className="ct-metadata-resumen">
                    <div>
                        <dt>Relación con comercio electrónico</dt>
                        <dd>{criteriosActuales.vinculadoComercioElectronico === 'S' ? 'Sí' : 'No'}</dd>
                    </div>
                    <div>
                        <dt>Observaciones técnicas</dt>
                        <dd>{criteriosActuales.observacionesTecnicas || '—'}</dd>
                    </div>
                </dl>
            </div>

            <ResumenCriticidad criteriosActuales={criteriosActuales} soloLectura />

            <div className="ct-criterios-lista">
                <div className="ct-criterio-fila">
                    <div className="ct-criterio-info">
                        <span className="ct-criterio-nombre">Daño potencial a la salud</span>
                        <span className="ct-criterio-valor">{criteriosActuales.danoPotencialSalud || 'No registrado'}</span>
                    </div>
                </div>
                <div className="ct-criterio-fila">
                    <div className="ct-criterio-info">
                        <span className="ct-criterio-nombre">Criterio técnico especializado UVC</span>
                        <span className="ct-criterio-valor">
                            {criteriosActuales.requiereCriterioEspecializado === 'Si'
                                ? 'Sí'
                                : criteriosActuales.requiereCriterioEspecializado || 'No registrado'}
                        </span>
                    </div>
                </div>
            </div>

            <div className="banner-info ct-solo-lectura">
                <span>
                    Vista de solo lectura: puede consultar el expediente, pero los criterios los registra el funcionario
                    asignado y solo un superusuario puede modificarlos.
                </span>
            </div>
        </>
    );
}

function ResumenCriticidad({ criteriosActuales, soloLectura = false }) {
    if (!criteriosActuales) return null;

    const nivel = criteriosActuales.nivelCriticidad;
    const plazoDias = criteriosActuales.plazoDias ?? (nivel ? PLAZO_DIAS_POR_NIVEL[nivel] : undefined);

    let mensajePendiente = null;
    if (!nivel) {
        const faltantes = [
            !criteriosActuales.danoPotencialSalud && ETIQUETAS_CRITERIO[CRITERIO_DANO_POTENCIAL_SALUD],
            !criteriosActuales.requiereCriterioEspecializado && ETIQUETAS_CRITERIO[CRITERIO_ESPECIALIZADO_UVC],
        ].filter(Boolean);

        mensajePendiente = faltantes.length === 0
            ? 'Criticidad pendiente de cálculo'
            : `Criticidad pendiente: falta ${soloLectura ? '' : 'completar '}${faltantes.join(' y ')}`;
    }

    return (
        <section
            className={`ct-resumen-criticidad${nivel ? ` ct-resumen-criticidad--${nivel.toLowerCase()}` : ''}`}
            aria-label="Resumen de criticidad y plazos"
        >
            <div className="ct-puntaje-total">
                <span className="ct-resumen-etiqueta">
                    Puntaje de criticidad de la denuncia al responder los criterios
                </span>
                <strong>{criteriosActuales.puntajeTotal ?? 0}</strong>
            </div>

            {mensajePendiente ? (
                <div className="ct-criticidad-pendiente" role="status">
                    {mensajePendiente}
                </div>
            ) : (
                <dl className="ct-resumen-lista">
                    <div className="ct-resumen-fila">
                        <dt>Nivel de criticidad</dt>
                        <dd>
                            <span className={`badge-criticidad ${nivel.toLowerCase()}`}>{nivel}</span>
                        </dd>
                    </div>
                    {plazoDias !== undefined && (
                        <div className="ct-resumen-fila">
                            <dt>Plazo máximo de días para atender la denuncia</dt>
                            <dd className="ct-plazo-dias">
                                <strong>{plazoDias}</strong> días
                            </dd>
                        </div>
                    )}
                    {criteriosActuales.fechaLimite && (
                        <div className="ct-resumen-fila">
                            <dt>Fecha máxima para finalizar la denuncia</dt>
                            <dd className="ct-fecha-limite">
                                {new Date(criteriosActuales.fechaLimite).toLocaleDateString('es-CR')}
                            </dd>
                        </div>
                    )}
                </dl>
            )}
        </section>
    );
}

const ESTADOS_DENUNCIA = {
    PENDIENTE_ASIGNACION: 'Pendiente de asignación',
    EN_TIEMPO: 'En tiempo',
    PRONTA_VENCER: 'Pronta a vencer',
    ATRASADA: 'Atrasada',
    EN_REVISION: 'En revisión',
    FINALIZADA: 'Finalizada',
};

function formatearSiNo(valor) {
    if (valor === 'S') return 'Sí';
    if (valor === 'N') return 'No';
    return '—';
}

function formatearFecha(valor) {
    return valor ? new Date(valor).toLocaleDateString('es-CR') : '—';
}


function DetalleDenuncia({ detalle }) {
    const { hecho, ubicacion, productos } = detalle;
    const ubicacionTexto = ubicacion
        ? [ubicacion.provincia, ubicacion.canton, ubicacion.distrito].filter(Boolean).join(', ')
        : null;

    return (
        <details className="ct-detalle-denuncia" open>
            <summary>
                <h4>Información de la denuncia</h4>
                <span className="ct-detalle-denuncia-resumen">
                    Recibida el {formatearFecha(detalle.fechaCreacion)} · {ESTADOS_DENUNCIA[detalle.estado] ?? detalle.estado}
                </span>
            </summary>

            <div className="ct-detalle-denuncia-cuerpo">
                <section aria-label="Hecho denunciado">
                    <h5>Hecho denunciado</h5>
                    {hecho ? (
                        <dl className="ct-metadata-resumen">
                            <div>
                                <dt>Establecimiento</dt>
                                <dd>{hecho.nombreEstablecimiento || '—'}</dd>
                            </div>
                            <div>
                                <dt>Descripción del hecho</dt>
                                <dd className="ct-texto-largo">{hecho.descripcionHecho || '—'}</dd>
                            </div>
                            <div>
                                <dt>¿Denunciado en otra institución?</dt>
                                <dd>
                                    {formatearSiNo(hecho.denunciadoOtraInstitucion)}
                                    {hecho.denunciadoOtraInstitucion === 'S' && hecho.detalleOtraInstitucion && ` — ${hecho.detalleOtraInstitucion}`}
                                </dd>
                            </div>
                            <div>
                                <dt>¿Denunciado previamente al Ministerio de Salud?</dt>
                                <dd>
                                    {formatearSiNo(hecho.denunciadoPreviamenteMinsalud)}
                                    {hecho.denunciadoPreviamenteMinsalud === 'S' && hecho.detallePrevioMinsalud && ` — ${hecho.detallePrevioMinsalud}`}
                                </dd>
                            </div>
                        </dl>
                    ) : (
                        <p className="ct-vacio">No se registró información del hecho.</p>
                    )}
                </section>

                <section aria-label="Ubicación">
                    <h5>Ubicación</h5>
                    {ubicacion ? (
                        <dl className="ct-metadata-resumen">
                            <div>
                                <dt>Provincia, cantón y distrito</dt>
                                <dd>{ubicacionTexto || '—'}</dd>
                            </div>
                            <div>
                                <dt>Dirección exacta</dt>
                                <dd>{ubicacion.direccionExacta || '—'}</dd>
                            </div>
                        </dl>
                    ) : (
                        <p className="ct-vacio">No se registró la ubicación.</p>
                    )}
                </section>

                <section aria-label="Productos denunciados">
                    <h5>Productos denunciados ({productos.length})</h5>
                    {productos.length === 0 ? (
                        <p className="ct-vacio">No se registraron productos.</p>
                    ) : (
                        <div className="ct-detalle-productos">
                            {productos.map((p, indice) => (
                                <article className="ct-detalle-producto" key={p.idDetalleProducto}>
                                    <div className="ct-detalle-producto-titulo">
                                        <strong>{productos.length > 1 ? `${indice + 1}. ` : ''}{p.nombreProducto}</strong>
                                        {p.tipoProducto && <span className="ct-detalle-chip">{p.tipoProducto}</span>}
                                    </div>

                                    <dl className="ct-metadata-resumen">
                                        <div>
                                            <dt>Motivos de la denuncia</dt>
                                            <dd>
                                                {p.motivos.length > 0 ? (
                                                    <ul className="ct-detalle-motivos">
                                                        {p.motivos.map((m) => <li key={m}>{m}</li>)}
                                                    </ul>
                                                ) : '—'}
                                            </dd>
                                        </div>
                                        <div>
                                            <dt>Descripción del producto</dt>
                                            <dd className="ct-texto-largo">{p.descripcionProducto || '—'}</dd>
                                        </div>
                                    </dl>

                                    <dl className="ct-detalle-producto-datos">
                                        <div><dt>Marca</dt><dd>{p.marca || '—'}</dd></div>
                                        <div><dt>Registro sanitario</dt><dd>{p.registroSanitario || '—'}</dd></div>
                                        <div><dt>Lote</dt><dd>{p.numeroLote || '—'}</dd></div>
                                        <div><dt>Fabricante</dt><dd>{p.fabricante || '—'}</dd></div>
                                        <div><dt>País de origen</dt><dd>{p.paisOrigen || '—'}</dd></div>
                                        <div><dt>Presentación</dt><dd>{p.presentacion || '—'}</dd></div>
                                        <div><dt>Fecha de compra</dt><dd>{formatearFecha(p.fechaCompra)}</dd></div>
                                    </dl>
                                </article>
                            ))}
                        </div>
                    )}
                </section>
            </div>
        </details>
    );
}