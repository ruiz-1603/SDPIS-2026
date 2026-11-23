import { useCallback, useEffect, useMemo, useState } from 'react';
import { useSesion } from '../../../context/SesionContext';
import { listarAreas, obtenerPermisosDelegables } from '../../../services/gestionUsuariosService';
import { useConsulta } from './useConsulta';
import { detalleAreas } from './utilidades';
import { PERMISOS, ROLES } from '../../../config/permisos';
import { TablaFuncionarios } from './TablaFuncionarios';
import { TablaAreas } from './TablaAreas';
import { HistorialAdministrativo } from './HistorialAdministrativo';
import { ModalRenuncia } from './Modales';
import { AyudaRolesPermisos } from './AyudaRolesPermisos';
import './GestionUsuariosPage.css';

// HU-015: administracion de roles y permisos con vista diferenciada:
//   Jefe de area  -> Funcionarios del area (con sus denuncias) + Historial.
//   Superusuario  -> Areas, Jefes de area, Funcionarios (todas las areas)
//                    + Historial + renuncia a su rol.
//   Delegados     -> solo las pestanas de los permisos que se les otorgaron.
// Las pestanas se arman con los permisos efectivos (GET mis-permisos); el
// backend vuelve a validar todo.
export function GestionUsuariosPage() {
    const { sesion, tienePermiso, refrescarPermisos } = useSesion();

    const esSuper = tienePermiso(PERMISOS.ADMINISTRAR_SISTEMA);
    const veTodas = tienePermiso(PERMISOS.VER_TODAS_AREAS);
    const veListaAreas = tienePermiso(PERMISOS.VER_LISTA_AREAS);
    const veFuncionarios = tienePermiso(PERMISOS.VER_FUNCIONARIOS_AREA);
    const veHistorial = tienePermiso(PERMISOS.VER_BITACORA_ADMINISTRATIVA);
    const puedeDelegar = tienePermiso(PERMISOS.DELEGAR_PERMISOS_AREA);

    const pestanas = useMemo(() => [
        veListaAreas && { id: 'areas', etiqueta: 'Áreas' },
        veFuncionarios && { id: 'funcionarios', etiqueta: veTodas ? 'Funcionarios' : 'Funcionarios del área' },
        veHistorial && { id: 'historial', etiqueta: 'Historial administrativo' },
        // Guia de uso y buenas practicas de seguridad (contenido segun rol).
        { id: 'ayuda', etiqueta: 'Ayuda' },
    ].filter(Boolean), [veListaAreas, veTodas, veFuncionarios, veHistorial]);

    const [pestanaElegida, setPestana] = useState(null);
    const [version, setVersion] = useState(0);
    const [resumenArea, setResumenArea] = useState(null);
    const [aviso, setAviso] = useState(null);
    const [renunciaAbierta, setRenunciaAbierta] = useState(false);

    // Si los permisos cambian (p. ej. tras renunciar) y la pestana elegida ya
    // no existe, se muestra la primera disponible (derivado, sin efecto).
    const pestana = pestanas.some((p) => p.id === pestanaElegida) ? pestanaElegida : pestanas[0]?.id;

    const { datos: areas, error: errorAreas } = useConsulta(
        () => (veListaAreas ? listarAreas() : Promise.resolve([])),
        [veListaAreas, version],
        { mantenerAnterior: true }
    );

    const { datos: catalogo } = useConsulta(
        () => (puedeDelegar ? obtenerPermisosDelegables() : Promise.resolve([])),
        [puedeDelegar]
    );

    useEffect(() => {
        if (!aviso) return undefined;
        const temporizador = setTimeout(() => setAviso(null), 4500);
        return () => clearTimeout(temporizador);
    }, [aviso]);

    const marcarCambio = useCallback(() => setVersion((v) => v + 1), []);

    // Resumen del jefe: se toma de su lista (sin otra consulta al backend).
    const alCargarFuncionarios = useCallback((lista) => {
        if (!veTodas) setResumenArea(lista);
    }, [veTodas]);

    async function alRenunciar() {
        setRenunciaAbierta(false);
        setAviso('Renunció al rol de superusuario. Ahora tiene la vista de jefe de área (en renuncia).');
        await refrescarPermisos();
    }

    const nombreArea = resumenArea?.find((f) => f.idFuncionario === sesion?.usuario?.funcionarioId)?.areaNombre
        ?? resumenArea?.[0]?.areaNombre;

    return (
        <section className="page-gestion-usuarios gu-pagina">
            {/* El titulo ya esta en la miga de pan: aqui una nota a la izquierda y
                los indicadores, compactos, arriba a la derecha. */}
            <header className="gu-masthead">
                <div className="gu-masthead__izquierda">
                <aside className="gu-nota-info">
                    <span className="gu-nota-info__icono" aria-hidden="true">
                        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                            <circle cx="12" cy="12" r="9" /><path d="M12 11v5" /><path d="M12 7.5h.01" />
                        </svg>
                    </span>
                    <div className="gu-nota-info__cuerpo">
                        <p>
                            {veTodas
                                ? 'Administre las áreas, el cupo de jefes y los funcionarios de todo el sistema. Los permisos específicos se otorgan sin cambiar el rol y cada cambio queda en el historial administrativo.'
                                : `Administre los funcionarios de ${nombreArea ?? 'su área'}, sus denuncias y sus permisos específicos. Cada cambio queda registrado en el historial administrativo.`}
                        </p>
                        {esSuper && (
                            <button type="button" className="gu-btn gu-btn--chico gu-btn--peligro-contorno"
                                onClick={() => setRenunciaAbierta(true)}>
                                Renunciar a superusuario
                            </button>
                        )}
                    </div>
                </aside>

                {pestana === 'areas' && (
                    <p className="gu-nota">
                        El cupo de jefes de cada área lo define el superusuario. Los superusuarios en renuncia
                        no ocupan cupo y se muestran aparte.
                    </p>
                )}
                {pestana === 'historial' && (
                    <p className="gu-nota">Registro inmutable: no se puede modificar ni eliminar.</p>
                )}
                </div>

                <Indicadores veTodas={veTodas} areas={areas} resumenArea={resumenArea} />
            </header>

            {aviso && (
                <div className="gu-toast" role="status">
                    <span>{aviso}</span>
                    <button type="button" onClick={() => setAviso(null)} aria-label="Cerrar aviso">✕</button>
                </div>
            )}

            {pestanas.length > 1 && (
                <div className="gu-pestanas" role="tablist" aria-label="Secciones de administración">
                    {pestanas.map((p) => (
                        <button
                            key={p.id}
                            type="button"
                            role="tab"
                            aria-selected={pestana === p.id}
                            className={`gu-pestana ${pestana === p.id ? 'gu-pestana--activa' : ''}`}
                            onClick={() => setPestana(p.id)}
                        >
                            {p.etiqueta}
                        </button>
                    ))}
                </div>
            )}

            <div role="tabpanel">
                {pestana === 'areas' && (
                    <TablaAreas
                        areas={areas}
                        error={errorAreas}
                        onAviso={setAviso}
                        onCambio={marcarCambio}
                    />
                )}
                {pestana === 'funcionarios' && (
                    <TablaFuncionarios
                        key="funcionarios"
                        modo="funcionarios"
                        areas={areas}
                        catalogo={catalogo}
                        onAviso={setAviso}
                        onCargado={alCargarFuncionarios}
                        onCambioDatos={marcarCambio}
                    />
                )}
                {pestana === 'historial' && <HistorialAdministrativo areas={areas} version={version} />}
                {pestana === 'ayuda' && <AyudaRolesPermisos />}
            </div>

            {renunciaAbierta && (
                <ModalRenuncia
                    areas={areas}
                    areaActualId={sesion?.usuario?.areaId}
                    onCerrar={() => setRenunciaAbierta(false)}
                    onConfirmado={alRenunciar}
                />
            )}
        </section>
    );
}

function Indicadores({ veTodas, areas, resumenArea }) {
    let datos = null;

    if (veTodas && areas?.length) {
        const suma = (campo) => areas.reduce((t, a) => t + (a[campo] ?? 0), 0);
        datos = [
            { cifra: areas.length, etiqueta: 'Áreas', detalle: detalleAreas(areas) },
            { cifra: suma('jefesActivos'), etiqueta: 'Jefes activos' },
            { cifra: suma('funcionariosActivos'), etiqueta: 'Funcionarios activos' },
            { cifra: suma('denunciasPorAsignar'), etiqueta: 'Por asignar', destacado: true },
        ];
    } else if (!veTodas && resumenArea) {
        const activos = resumenArea.filter((f) => f.activo);
        const validadores = activos.filter((f) => f.rol === ROLES.VALIDADOR);
        const jefe = activos.find((f) => f.rol === ROLES.JEFE_AREA);
        datos = [
            { cifra: activos.length, etiqueta: 'Funcionarios activos' },
            { cifra: validadores.filter((f) => f.permiteAsignar).length, etiqueta: 'Con permiso de asignar' },
            { cifra: validadores.reduce((t, f) => t + (f.denunciasAsignadas ?? 0), 0), etiqueta: 'Denuncias asignadas' },
            { cifra: jefe?.denunciasPorAsignar ?? 0, etiqueta: 'Por asignar', destacado: true },
        ];
    }

    if (!datos) return <div className="gu-masthead__stats" />;

    return (
        <dl className="gu-masthead__stats">
            {datos.map((d) => (
                <div key={d.etiqueta} className={`gu-stat ${d.destacado ? 'gu-stat--destacado' : ''}`}>
                    {/* dt antes de dd (HTML valido); el CSS pone la cifra arriba. */}
                    <dt>
                        {d.etiqueta}
                        {d.detalle && <span className="gu-stat__detalle">{d.detalle}</span>}
                    </dt>
                    <dd>{d.cifra}</dd>
                </div>
            ))}
        </dl>
    );
}
