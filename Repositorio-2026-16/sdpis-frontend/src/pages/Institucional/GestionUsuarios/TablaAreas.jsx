import { useMemo, useState } from 'react';
import { useSesion } from '../../../context/SesionContext';
import { PERMISOS, TIPOS_AREA_CON_JEFE } from '../../../config/permisos';
import { SelectorBuscable } from '../../../components/comunes/SelectorBuscable';
import { Alerta, ModalCupo } from './Modales';
import { detalleAreas } from './utilidades';

// HU-015: lista de areas (exclusiva del superusuario / VER_LISTA_AREAS).
// Muestra el cupo de jefes, funcionarios activos y denuncias por asignar.
// Para ver los funcionarios o jefes de un area se usan las pestanas, con su
// filtro de area (no se duplican aqui como botones).
export function TablaAreas({ areas, error, onAviso, onCambio }) {
    const { tienePermiso } = useSesion();
    const puedeCupo = tienePermiso(PERMISOS.ADMINISTRAR_SISTEMA);
    const [areaFiltro, setAreaFiltro] = useState('');
    const [areaCupo, setAreaCupo] = useState(null);

    const filtradas = useMemo(() => {
        return (areas ?? []).filter((a) => areaFiltro === '' || String(a.idArea) === areaFiltro);
    }, [areas, areaFiltro]);

    return (
        <div className="gu-panel">
            <div className="gu-toolbar">
                <div className="gu-filtro gu-filtro--buscable">
                    <span className="gu-filtro__etiqueta">Área</span>
                    <SelectorBuscable
                        etiqueta="Área"
                        valor={areaFiltro}
                        onCambiar={setAreaFiltro}
                        textoTodos="Todas las áreas"
                        placeholder=" Buscar área por nombre o código…"
                        opciones={(areas ?? []).map((a) => ({ valor: a.idArea, texto: a.nombre, detalle: a.codigoArea }))}
                    />
                </div>
            </div>

            <Alerta>{error}</Alerta>
            {!error && areas === null && <p className="gu-cargando">Cargando áreas…</p>}

            {areas && (
                <>
                    <p className="gu-resultado">{filtradas.length} de {areas.length} áreas{detalleAreas(areas) ? ` (${detalleAreas(areas)})` : ''}</p>
                    <div className="gu-tabla-wrap">
                        <table className="gu-tabla tabla-tarjetas">
                            <thead>
                                <tr>
                                    <th>Área</th>
                                    <th>Región</th>
                                    <th>Jefes / cupo</th>
                                    <th>Funcionarios activos</th>
                                    <th>Denuncias</th>
                                    <th className="gu-col-acciones">Acciones</th>
                                </tr>
                            </thead>
                            <tbody>
                                {filtradas.length === 0 ? (
                                    <tr><td colSpan={6} className="gu-vacio">No hay áreas que coincidan con el filtro.</td></tr>
                                ) : filtradas.map((a) => {
                                    const rectora = TIPOS_AREA_CON_JEFE.includes(a.tipo);
                                    const lleno = rectora && a.jefesActivos >= a.maxJefes;
                                    return (
                                        <tr key={a.idArea}>
                                            <td data-label="Área">
                                                <span className="gu-celda-fuerte gu-bloque">{a.nombre}</span>
                                                <span className="gu-celda-tenue">
                                                    {a.codigoArea}{a.tipo && a.tipo !== a.codigoArea ? ` · ${a.tipo}` : ''}
                                                </span>
                                            </td>
                                                <td className="gu-celda-tenue" data-label="Región">{a.regionNombre ?? (a.tipo === 'UVC' ? 'UVC' : '—')}</td>
                                            <td data-label="Jefes / cupo">
                                                {rectora ? (
                                                    <div className="gu-cupo">
                                                        <span className={`gu-cupo__texto ${lleno ? 'gu-cupo__texto--lleno' : ''}`}>
                                                            {a.jefesActivos} / {a.maxJefes}
                                                        </span>
                                                        <span className="gu-cupo__barra" aria-hidden="true">
                                                            <span style={{ width: `${Math.min(100, (a.jefesActivos / a.maxJefes) * 100)}%` }} />
                                                        </span>
                                                    </div>
                                                ) : (
                                                    <span className="gu-celda-tenue">No aplica</span>
                                                )}
                                                {a.jefesEnRenuncia > 0 && (
                                                    <span className="gu-celda-tenue gu-bloque">
                                                        + {a.jefesEnRenuncia} en renuncia
                                                    </span>
                                                )}
                                            </td>
                                            <td data-label="Funcionarios activos">{a.funcionariosActivos}</td>
                                            <td data-label="Denuncias">
                                                <span className={`gu-carga gu-bloque ${a.denunciasPorAsignar > 0 ? 'gu-carga--pendiente' : ''}`}>
                                                    <strong>{a.denunciasPorAsignar}</strong> por asignar
                                                </span>
                                                <span className="gu-celda-tenue">{a.denunciasActivas} activas</span>
                                            </td>
                                            <td className="gu-col-acciones" data-label="">
                                                <div className="gu-acciones">
                                                    {rectora && puedeCupo && (
                                                        <button type="button" className="gu-btn gu-btn--chico gu-btn--primario"
                                                            onClick={() => setAreaCupo(a)}>
                                                            Editar cupo
                                                        </button>
                                                    )}
                                                </div>
                                            </td>
                                        </tr>
                                    );
                                })}
                            </tbody>
                        </table>
                    </div>
                </>
            )}

            {areaCupo && (
                <ModalCupo
                    area={areaCupo}
                    onCerrar={() => setAreaCupo(null)}
                    onGuardado={(mensaje) => { setAreaCupo(null); onAviso?.(mensaje); onCambio?.(); }}
                />
            )}
        </div>
    );
}