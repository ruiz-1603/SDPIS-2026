import { useEffect, useMemo, useState } from 'react';
import { listarBitacoraAdministrativa } from '../../../services/gestionUsuariosService';
import { useSesion } from '../../../context/SesionContext';
import { PERMISOS, CATEGORIAS_BITACORA } from '../../../config/permisos';
import { formatearHora, claveDia, etiquetaDia, aFechaIso, describirCambios, humanizarDetalle } from './utilidades';
import { Alerta } from './Modales';
import { useConsulta } from './useConsulta';
import { SelectorBuscable } from '../../../components/comunes/SelectorBuscable';
import { BotonOrdenFecha } from '../../../components/comunes/BotonOrdenFecha';

const TAMANO_PAGINA = 25;
const ETIQUETA_CATEGORIA = Object.fromEntries(CATEGORIAS_BITACORA.map((c) => [c.valor, c.etiqueta]));

// Periodos rapidos: evitan recorrer paginas y paginas cuando hay muchos datos.
const PERIODOS = [
    { valor: 'hoy', etiqueta: 'Hoy' },
    { valor: '7dias', etiqueta: 'Últimos 7 días' },
    { valor: '30dias', etiqueta: 'Últimos 30 días' },
    { valor: 'mes', etiqueta: 'Este mes' },
    { valor: 'mes-anterior', etiqueta: 'Mes anterior' },
    { valor: 'personalizado', etiqueta: 'Rango de fechas…' },
    { valor: 'todo', etiqueta: 'Todo el historial' },
];

function rangoDePeriodo(periodo, desdePersonal, hastaPersonal) {
    const hoy = new Date();
    const dias = (n) => { const f = new Date(hoy); f.setDate(f.getDate() - n); return f; };
    switch (periodo) {
        case 'hoy': return { desde: aFechaIso(hoy), hasta: aFechaIso(hoy) };
        case '7dias': return { desde: aFechaIso(dias(6)), hasta: aFechaIso(hoy) };
        case '30dias': return { desde: aFechaIso(dias(29)), hasta: aFechaIso(hoy) };
        case 'mes': return { desde: aFechaIso(new Date(hoy.getFullYear(), hoy.getMonth(), 1)), hasta: aFechaIso(hoy) };
        case 'mes-anterior': return {
            desde: aFechaIso(new Date(hoy.getFullYear(), hoy.getMonth() - 1, 1)),
            hasta: aFechaIso(new Date(hoy.getFullYear(), hoy.getMonth(), 0)),
        };
        case 'personalizado': return { desde: desdePersonal || undefined, hasta: hastaPersonal || undefined };
        default: return { desde: undefined, hasta: undefined };
    }
}

// HU-015: bitacora ADMINISTRATIVA (separada de la bitacora de denuncias):
// altas/ediciones, permisos, cupos, consultas e inicios de sesion.
// Superusuario: todas las areas; jefe/delegado: solo su area.
// Organizada por dia, con filtro de periodo y busqueda para volumenes grandes.
export function HistorialAdministrativo({ areas, version }) {
    const { tienePermiso } = useSesion();
    const veTodas = tienePermiso(PERMISOS.VER_TODAS_AREAS);

    const [periodo, setPeriodo] = useState('30dias');
    const [desdePersonal, setDesdePersonal] = useState('');
    const [hastaPersonal, setHastaPersonal] = useState('');
    const [areaId, setAreaId] = useState('');
    const [categoria, setCategoria] = useState('');
    const [busqueda, setBusqueda] = useState('');
    const [ordenFecha, setOrdenFecha] = useState('desc');
    const [texto, setTexto] = useState('');
    const [incluirConsultas, setIncluirConsultas] = useState(false);
    const [pagina, setPagina] = useState(1);

    const rango = useMemo(
        () => rangoDePeriodo(periodo, desdePersonal, hastaPersonal),
        [periodo, desdePersonal, hastaPersonal]
    );

    // La busqueda se aplica al dejar de escribir (no una consulta por tecla).
    useEffect(() => {
        const temporizador = setTimeout(() => {
            setTexto(busqueda.trim());
            setPagina(1);
        }, 400);
        return () => clearTimeout(temporizador);
    }, [busqueda]);

    const consulta = {
        areaId: veTodas ? areaId : undefined,
        categoria,
        desde: rango.desde,
        hasta: rango.hasta,
        texto,
        incluirConsultas,
        pagina,
        tamano: TAMANO_PAGINA,
    };
    const { datos: resultado, error } = useConsulta(
        () => listarBitacoraAdministrativa(consulta),
        [consulta, version]
    );

    // Cualquier cambio de filtro vuelve a la primera pagina.
    const conReinicio = (setter) => (valor) => { setter(valor); setPagina(1); };

    const grupos = useMemo(() => {
        const items = [...(resultado?.items ?? [])];
        if (ordenFecha === 'asc') {
            items.sort((a, b) => new Date(a.fecha).getTime() - new Date(b.fecha).getTime());
        } else {
            items.sort((a, b) => new Date(b.fecha).getTime() - new Date(a.fecha).getTime());
        }
        return agruparPorDia(items);
    }, [resultado, ordenFecha]);
    const totalPaginas = resultado ? Math.max(1, Math.ceil(resultado.total / TAMANO_PAGINA)) : 1;
    const columnas = veTodas ? 5 : 4;

    return (
        <div className="gu-panel">
            <div className="gu-toolbar">
                <div className="gu-filtros">
                    <label className="gu-filtro">
                        <span className="gu-filtro__etiqueta">Periodo</span>
                        <select className="gu-select" value={periodo}
                            onChange={(e) => conReinicio(setPeriodo)(e.target.value)}>
                            {PERIODOS.map((p) => <option key={p.valor} value={p.valor}>{p.etiqueta}</option>)}
                        </select>
                    </label>
                    {periodo === 'personalizado' && (
                        <>
                            <label className="gu-filtro">
                                <span className="gu-filtro__etiqueta">Desde</span>
                                <input type="date" className="gu-select gu-select--fecha" value={desdePersonal}
                                    max={hastaPersonal || undefined}
                                    onChange={(e) => conReinicio(setDesdePersonal)(e.target.value)} />
                            </label>
                            <label className="gu-filtro">
                                <span className="gu-filtro__etiqueta">Hasta</span>
                                <input type="date" className="gu-select gu-select--fecha" value={hastaPersonal}
                                    min={desdePersonal || undefined}
                                    onChange={(e) => conReinicio(setHastaPersonal)(e.target.value)} />
                            </label>
                        </>
                    )}
                    <label className="gu-filtro">
                        <span className="gu-filtro__etiqueta">Categoría</span>
                        <select className="gu-select" value={categoria}
                            onChange={(e) => conReinicio(setCategoria)(e.target.value)}>
                            <option value="">Todas</option>
                            {CATEGORIAS_BITACORA.map((c) => (
                                <option key={c.valor} value={c.valor}>{c.etiqueta}</option>
                            ))}
                        </select>
                    </label>
                    {veTodas && (
                        <div className="gu-filtro gu-filtro--buscable">
                            <span className="gu-filtro__etiqueta">Área</span>
                            <SelectorBuscable
                                etiqueta="Área"
                                valor={areaId}
                                onCambiar={conReinicio(setAreaId)}
                                textoTodos="Todas las áreas"
                                placeholder=" Buscar área por nombre o código…"
                                opciones={(areas ?? []).map((a) => ({ valor: a.idArea, texto: a.nombre, detalle: a.codigoArea }))}
                            />
                        </div>
                    )}
                </div>

                <div className="gu-buscar">
                    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
                        <circle cx="11" cy="11" r="7" /><path d="m20 20-3.5-3.5" />
                    </svg>
                    <input type="search" placeholder="Buscar por funcionario o detalle" value={busqueda}
                        maxLength={100} onChange={(e) => setBusqueda(e.target.value)} aria-label="Buscar en el historial" />
                </div>
                <BotonOrdenFecha
                    orden={ordenFecha}
                    alCambiar={setOrdenFecha}
                />
            </div>

            <div className="gu-historial-opciones">
                {categoria === '' && (
                    <label className="gu-casilla">
                        <input type="checkbox" checked={incluirConsultas}
                            onChange={(e) => conReinicio(setIncluirConsultas)(e.target.checked)} />
                        Incluir consultas a listas
                    </label>
                )}
            </div>

            <Alerta>{error}</Alerta>
            {!error && resultado === null && <p className="gu-cargando">Cargando historial…</p>}

            {resultado && (
                <>
                    <p className="gu-resultado">
                        {resultado.total} registro{resultado.total === 1 ? '' : 's'}
                        {totalPaginas > 1 && ` · página ${pagina} de ${totalPaginas}`}
                    </p>

                    {resultado.items.length === 0 ? (
                        <div className="gu-tabla-wrap"><p className="gu-vacio">No hay registros con estos filtros.</p></div>
                    ) : (
                        <div className="gu-tabla-wrap">
                            <table className="gu-tabla gu-tabla--historial tabla-tarjetas">
                                <thead>
                                    <tr>
                                        <th className="gu-col-hora">Hora</th>
                                        <th>Evento</th>
                                        <th>Realizado por</th>
                                        <th>Funcionario afectado</th>
                                        {veTodas && <th>Área</th>}
                                    </tr>
                                </thead>
                                {grupos.map((g) => (
                                    <tbody key={g.clave}>
                                        <tr className="gu-dia">
                                            <th colSpan={columnas} scope="colgroup">
                                                <span className="gu-dia__fecha">{etiquetaDia(g.fecha)}</span>
                                                <span className="gu-dia__conteo">
                                                    {g.items.length} evento{g.items.length === 1 ? '' : 's'}
                                                </span>
                                            </th>
                                        </tr>
                                        {g.items.map((r) => <FilaEvento key={`${r.origen}-${r.idRegistro}`} r={r} veTodas={veTodas} />)}
                                    </tbody>
                                ))}
                            </table>
                        </div>
                    )}

                    {totalPaginas > 1 && (
                        <nav className="gu-paginacion" aria-label="Paginación del historial">
                            <button type="button" className="gu-btn gu-btn--chico gu-btn--contorno"
                                disabled={pagina <= 1} onClick={() => setPagina((p) => p - 1)}>
                                ‹ Más recientes
                            </button>
                            <span>Página {pagina} de {totalPaginas}</span>
                            <button type="button" className="gu-btn gu-btn--chico gu-btn--contorno"
                                disabled={pagina >= totalPaginas} onClick={() => setPagina((p) => p + 1)}>
                                Más antiguos ›
                            </button>
                        </nav>
                    )}
                </>
            )}
        </div>
    );
}

function FilaEvento({ r, veTodas }) {
    const cambios = describirCambios(r.valorAnterior, r.valorNuevo);
    const esAlerta = r.tipoEvento === 'INTENTO_FALLIDO';

    return (
        <tr className={esAlerta ? 'gu-evento--alerta' : ''}>
            <td className="gu-celda-tenue gu-nowrap gu-col-hora" data-label="Hora">{formatearHora(r.fecha)}</td>
            <td data-label="Evento">
                <span className={`gu-categoria gu-categoria--${r.categoria.toLowerCase()}`}>
                    {ETIQUETA_CATEGORIA[r.categoria] ?? r.categoria}
                </span>
                {esAlerta && <span className="gu-categoria gu-categoria--alerta">Intento fallido</span>}
                <span className="gu-bloque gu-evento__detalle">{humanizarDetalle(r.detalle)}</span>
                {cambios.length > 0 && (
                    <ul className="gu-cambios">
                        {cambios.map((c) => (
                            <li key={c.etiqueta}>
                                <span className="gu-cambios__campo">{c.etiqueta}:</span>{' '}
                                {c.antes !== undefined && (
                                    <><span className="gu-cambio__antes">{c.antes}</span><span aria-label="cambió a"> → </span></>
                                )}
                                <span className="gu-cambio__despues">{c.despues}</span>
                            </li>
                        ))}
                    </ul>
                )}
                {r.detalleTecnico && <span className="gu-bloque gu-evento__tecnico">{r.detalleTecnico}</span>}
            </td>
            <td data-label="Realizado por">{r.realizadoPorNombre ?? <span className="gu-celda-tenue">Sistema / no registrado</span>}</td>
            <td data-label="Funcionario afectado">{r.funcionarioAfectadoNombre ?? <span className="gu-celda-tenue">—</span>}</td>
            {veTodas && <td className="gu-celda-tenue" data-label="Área">{r.areaNombre ?? 'General'}</td>}
        </tr>
    );
}

// Los items ya vienen ordenados del mas reciente al mas antiguo.
function agruparPorDia(items) {
    const grupos = [];
    items.forEach((item) => {
        const clave = claveDia(item.fecha);
        let grupo = grupos[grupos.length - 1];
        if (!grupo || grupo.clave !== clave) {
            grupo = { clave, fecha: item.fecha, items: [] };
            grupos.push(grupo);
        }
        grupo.items.push(item);
    });
    return grupos;
}
