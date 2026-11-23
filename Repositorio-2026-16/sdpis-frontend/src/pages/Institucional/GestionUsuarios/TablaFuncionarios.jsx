import { useCallback, useEffect, useMemo, useState } from 'react';
import { listarFuncionarios } from '../../../services/gestionUsuariosService';
import { useConsulta } from './useConsulta';
import { SelectorBuscable } from '../../../components/comunes/SelectorBuscable';
import { useSesion } from '../../../context/SesionContext';
import { PERMISOS, ROLES, FILTRO_EN_RENUNCIA } from '../../../config/permisos';
import { etiquetaRol, etiquetaPermiso, normalizarTexto, formatearFecha } from './utilidades';
import {
    Alerta, ModalFuncionario, ModalPermisos, ModalAreasTrabajo, ModalDenuncias, ModalCredencialTemporal,
} from './Modales';

// HU-015: lista de funcionarios.
//   modo 'funcionarios': jefe -> su area; superusuario -> cualquier area.
//   modo 'jefes'       : lista de jefes de area (solo superusuario).
// Validadores muestran sus denuncias ASIGNADAS; jefes, sus denuncias
// POR ASIGNAR (un jefe nunca tiene denuncias asignadas).
export function TablaFuncionarios({ modo, areas, areaInicial, catalogo, onAviso, onCargado, onCambioDatos }) {
    const { sesion, tienePermiso } = useSesion();
    const miId = sesion?.usuario?.funcionarioId;
    const esSuper = tienePermiso(PERMISOS.ADMINISTRAR_SISTEMA);
    const veTodas = tienePermiso(PERMISOS.VER_TODAS_AREAS);
    const puedeCrear = tienePermiso(PERMISOS.CREAR_FUNCIONARIOS_AREA);
    const puedeDelegar = tienePermiso(PERMISOS.DELEGAR_PERMISOS_AREA);
    const esModoJefes = modo === 'jefes';

    const [areaId, setAreaId] = useState(areaInicial ?? '');
    const [rol, setRol] = useState(esModoJefes ? ROLES.JEFE_AREA : '');
    const [estado, setEstado] = useState('activos');
    const [busqueda, setBusqueda] = useState('');
    const [recarga, setRecarga] = useState(0);
    const [modal, setModal] = useState(null); // { tipo, funcionario? }

    // areaInicial solo fija el valor de arranque: el padre monta la tabla con
    // una "key" por pestana, asi que al venir desde Areas se crea de nuevo.
    const { datos: funcionarios, error } = useConsulta(
        () => listarFuncionarios({
            areaId: veTodas ? areaId : undefined,
            rol: rol === FILTRO_EN_RENUNCIA ? ROLES.JEFE_AREA : (rol || undefined),
        }),
        [areaId, rol, recarga, veTodas],
        { mantenerAnterior: true }
    );

    // El resumen del encabezado solo se calcula con la lista completa.
    useEffect(() => {
        if (funcionarios && !rol) onCargado?.(funcionarios);
        // onCargado se omite a proposito: es un callback del padre.
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [funcionarios, rol]);

    const recargar = useCallback(() => {
        setRecarga((n) => n + 1);
        onCambioDatos?.();
    }, [onCambioDatos]);

    const filtrados = useMemo(() => {
        const texto = normalizarTexto(busqueda.trim());
        return (funcionarios ?? []).filter((f) => {
            if (rol === FILTRO_EN_RENUNCIA && !f.renunciaSuperusuarioFecha) return false;
            if (estado === 'activos' && !f.activo) return false;
            if (estado === 'inactivos' && f.activo) return false;
            if (!texto) return true;
            return [f.nombre, f.correo, f.especialidad, f.areaNombre, ...(f.areasAdicionales ?? []).map((a) => a.nombre)]
                .some((v) => normalizarTexto(v).includes(texto));
        });
    }, [funcionarios, busqueda, estado]);

    // Mi area (jefe): la tomo de mi propia fila para el alta.
    const miArea = useMemo(() => {
        const yo = (funcionarios ?? []).find((f) => f.idFuncionario === miId);
        return yo ? { id: yo.areaId, nombre: yo.areaNombre } : { id: sesion?.usuario?.areaId, nombre: 'Mi área' };
    }, [funcionarios, miId, sesion]);

    const rolesCreables = esSuper
        ? [ROLES.VALIDADOR, ROLES.JEFE_AREA, ROLES.SUPERUSUARIO]
        : [ROLES.VALIDADOR];

    // Mismas reglas que el backend, solo para no mostrar botones inutiles.
    // Un funcionario con varias areas aparece tambien en la lista del jefe de su area
    // adicional, pero solo se administra desde su area base (o por un superusuario).
    const esDeMiArea = (f) => esSuper || f.areaId === miArea.id;
    const puedeEditar = (f) => f.idFuncionario !== miId && f.rol !== ROLES.SUPERUSUARIO
        && puedeCrear && esDeMiArea(f) && (esSuper || f.rol === ROLES.VALIDADOR);
    // Solo el superusuario decide en cuantas areas trabaja un funcionario.
    const puedeAreas = (f) => esSuper && f.rol === ROLES.VALIDADOR;
    const puedePermisos = (f) => f.idFuncionario !== miId && f.rol !== ROLES.SUPERUSUARIO && puedeDelegar && esDeMiArea(f);

    const cerrarModal = () => setModal(null);
    const alGuardar = (mensaje) => {
        setModal(null);
        onAviso?.(mensaje);
        recargar();
    };

    return (
        <div className="gu-panel">
            <div className="gu-toolbar">
                <div className="gu-filtros">
                    {veTodas && (
                        <div className="gu-filtro gu-filtro--buscable">
                            <span className="gu-filtro__etiqueta">Área</span>
                            <SelectorBuscable
                                etiqueta="Área"
                                valor={areaId}
                                onCambiar={setAreaId}
                                textoTodos="Todas las áreas"
                                placeholder=" Buscar área por nombre o código…"
                                opciones={(areas ?? []).map((a) => ({ valor: a.idArea, texto: a.nombre, detalle: a.codigoArea }))}
                            />
                        </div>
                    )}
                    {!esModoJefes && (
                        <label className="gu-filtro">
                            <span className="gu-filtro__etiqueta">Rol</span>
                            <select className="gu-select" value={rol} onChange={(e) => setRol(e.target.value)}>
                                <option value="">Todos</option>
                                <option value={ROLES.VALIDADOR}>Funcionarios</option>
                                <option value={ROLES.JEFE_AREA}>Jefes de área</option>
                                {veTodas && <option value={ROLES.SUPERUSUARIO}>Superusuarios</option>}
                                {veTodas && <option value={FILTRO_EN_RENUNCIA}>Superusuarios en renuncia</option>}
                            </select>
                        </label>
                    )}
                    <label className="gu-filtro">
                        <span className="gu-filtro__etiqueta">Estado</span>
                        <select className="gu-select" value={estado} onChange={(e) => setEstado(e.target.value)}>
                            <option value="activos">Activos</option>
                            <option value="inactivos">Inactivos</option>
                            <option value="todos">Todos</option>
                        </select>
                    </label>
                </div>

                <div className="gu-buscar">
                    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
                        <circle cx="11" cy="11" r="7" /><path d="m20 20-3.5-3.5" />
                    </svg>
                    <input
                        type="search"
                        placeholder="Buscar por nombre, correo o especialidad"
                        value={busqueda}
                        onChange={(e) => setBusqueda(e.target.value)}
                        aria-label="Buscar funcionario"
                    />
                </div>

                {puedeCrear && (
                    <button type="button" className="gu-btn gu-btn--primario"
                        onClick={() => setModal({ tipo: 'crear' })}>
                        + {esModoJefes ? 'Nuevo jefe de área' : 'Nuevo funcionario'}
                    </button>
                )}
            </div>

            <Alerta>{error}</Alerta>

            {!error && funcionarios === null && <p className="gu-cargando">Cargando funcionarios…</p>}

            {funcionarios && (
                <>
                    <p className="gu-resultado">
                        {filtrados.length} de {funcionarios.length} funcionario{funcionarios.length === 1 ? '' : 's'}
                    </p>
                    <div className="gu-tabla-wrap">
                        <table className="gu-tabla tabla-tarjetas">
                            <thead>
                                <tr>
                                    <th>Funcionario</th>
                                    <th>Rol y permisos</th>
                                    {veTodas && <th>Área</th>}
                                    <th>{esModoJefes ? 'Por asignar' : 'Carga'}</th>
                                    <th>Estado</th>
                                    <th className="gu-col-acciones">Acciones</th>
                                </tr>
                            </thead>
                            <tbody>
                                {filtrados.length === 0 ? (
                                    <tr>
                                        <td colSpan={veTodas ? 6 : 5} className="gu-vacio">
                                            No hay funcionarios que coincidan con los filtros.
                                        </td>
                                    </tr>
                                ) : filtrados.map((f) => (
                                    <tr key={f.idFuncionario} className={f.activo ? '' : 'gu-fila-inactiva'}>
                                        <td data-label="Funcionario">
                                            <div className="gu-persona">
                                                <span className="gu-persona__avatar" aria-hidden="true">
                                                    {f.nombre.trim().charAt(0).toUpperCase()}
                                                </span>
                                                <div>
                                                    <span className="gu-celda-fuerte">
                                                        {f.nombre}
                                                        {f.idFuncionario === miId && <span className="gu-chip gu-chip--tenue">Usted</span>}
                                                    </span>
                                                    <span className="gu-celda-tenue gu-bloque">{f.correo ?? 'Sin correo'}</span>
                                                    {f.especialidad && (
                                                        <span className="gu-celda-tenue gu-bloque">Especialidad: {f.especialidad}</span>
                                                    )}
                                                </div>
                                            </div>
                                        </td>
                                        <td data-label="Rol y permisos">
                                            <span className={`gu-rol gu-rol--${f.rol}`}>{etiquetaRol(f.rol)}</span>
                                            {f.renunciaSuperusuarioFecha && (
                                                <span className="gu-chip gu-chip--renuncia gu-bloque-chip"
                                                    title="Ex superusuario: no ocupa cupo de jefes. Puede retirarle el rol de jefe o darlo de baja desde Editar.">
                                                    Superusuario en renuncia · {formatearFecha(f.renunciaSuperusuarioFecha)}
                                                </span>
                                            )}
                                            <div className="gu-chips">
                                                {f.rol === ROLES.VALIDADOR && f.permiteAsignar && (
                                                    <span className="gu-chip gu-chip--dorado">Asignar denuncias</span>
                                                )}
                                                {f.rol !== ROLES.SUPERUSUARIO && f.permisosDelegados.map((p) => (
                                                    <span key={p} className="gu-chip">{etiquetaPermiso(p)}</span>
                                                ))}
                                                {f.rol === ROLES.VALIDADOR && !esSuper && f.areaId !== miArea.id && (
                                                    <span className="gu-chip gu-chip--area"
                                                        title="Su área base es otra; un superusuario le otorgó trabajar también en esta.">
                                                        Área base: {f.areaNombre}
                                                    </span>
                                                )}
                                                {f.rol === ROLES.VALIDADOR && !esSuper && f.areaId === miArea.id
                                                    && (f.areasAdicionales ?? []).length > 0 && (
                                                    <span className="gu-chip gu-chip--area"
                                                        title="Un superusuario le otorgó trabajar también en estas áreas.">
                                                        También en: {f.areasAdicionales.map((a) => a.nombre).join(', ')}
                                                    </span>
                                                )}
                                            </div>
                                        </td>
                                        {veTodas && (
                                            <td data-label="Área">
                                                <span className="gu-area">{f.areaNombre}</span>
                                                {(f.areasAdicionales ?? []).length > 0 && (
                                                    <span className="gu-celda-tenue gu-bloque">
                                                        + {f.areasAdicionales.length === 1 ? 'Área adicional' : `${f.areasAdicionales.length} áreas adicionales`}:{' '}
                                                        {f.areasAdicionales.map((a) => a.nombre).join(', ')}
                                                    </span>
                                                )}
                                            </td>
                                        )}
                                        <td data-label={esModoJefes ? 'Por asignar' : 'Carga'}><Carga funcionario={f} /></td>
                                        <td data-label="Estado">
                                            <span className={`gu-estado ${f.activo ? 'gu-estado--activo' : 'gu-estado--inactivo'}`}>
                                                {f.activo ? 'Activo' : 'Inactivo'}
                                            </span>
                                        </td>
                                        <td className="gu-col-acciones" data-label="">
                                            <div className="gu-acciones">
                                                {f.rol !== ROLES.SUPERUSUARIO && (
                                                    <button type="button" className="gu-btn gu-btn--chico gu-btn--contorno"
                                                        onClick={() => setModal({ tipo: 'denuncias', funcionario: f })}>
                                                        {f.rol === ROLES.JEFE_AREA ? 'Por asignar' : 'Denuncias'}
                                                    </button>
                                                )}
                                                {puedeEditar(f) && (
                                                    <button type="button" className="gu-btn gu-btn--chico gu-btn--contorno"
                                                        onClick={() => setModal({ tipo: 'editar', funcionario: f })}>
                                                        Editar
                                                    </button>
                                                )}
                                                {puedeAreas(f) && (
                                                    <button type="button" className="gu-btn gu-btn--chico gu-btn--contorno"
                                                        onClick={() => setModal({ tipo: 'areas', funcionario: f })}>
                                                        Áreas{(f.areasAdicionales ?? []).length > 0 ? ` (${f.areasAdicionales.length + 1})` : ''}
                                                    </button>
                                                )}
                                                {puedePermisos(f) && (
                                                    <button type="button" className="gu-btn gu-btn--chico gu-btn--primario"
                                                        onClick={() => setModal({ tipo: 'permisos', funcionario: f })}>
                                                        Permisos
                                                    </button>
                                                )}
                                            </div>
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>
                </>
            )}

            {modal?.tipo === 'crear' && (
                <ModalFuncionario
                    modo="crear"
                    areas={veTodas ? areas : null}
                    areaFija={veTodas ? null : miArea}
                    rolesPermitidos={esModoJefes ? [ROLES.JEFE_AREA] : rolesCreables}
                    onCerrar={cerrarModal}
                    onGuardado={alGuardar}
                />
            )}
            {modal?.tipo === 'editar' && (
                <ModalFuncionario
                    modo="editar"
                    funcionario={modal.funcionario}
                    areas={areas}
                    rolesPermitidos={rolesCreables}
                    onCerrar={cerrarModal}
                    onGuardado={alGuardar}
                    onCredencial={() => setModal({ tipo: 'credencial', funcionario: modal.funcionario })}
                    onAreas={esSuper ? () => setModal({ tipo: 'areas', funcionario: modal.funcionario }) : undefined}
                />
            )}
            {modal?.tipo === 'permisos' && (
                <ModalPermisos
                    funcionario={modal.funcionario}
                    catalogo={catalogo}
                    puedeNivelJefe={esSuper || (puedeDelegar && modal.funcionario.areaId === miArea.id)}
                    puedeNivelSuperusuario={esSuper}
                    onCerrar={cerrarModal}
                    onCambio={recargar}
                />
            )}
            {modal?.tipo === 'areas' && (
                <ModalAreasTrabajo funcionario={modal.funcionario} areas={areas}
                    onCerrar={cerrarModal} onCambio={recargar} />
            )}
            {modal?.tipo === 'denuncias' && (
                <ModalDenuncias funcionario={modal.funcionario} onCerrar={cerrarModal} />
            )}
            {modal?.tipo === 'credencial' && (
                <ModalCredencialTemporal funcionario={modal.funcionario} onCerrar={cerrarModal} onGuardado={alGuardar} />
            )}
        </div>
    );
}

function Carga({ funcionario: f }) {
    if (f.rol === ROLES.JEFE_AREA) {
        return (
            <span className={`gu-carga ${f.denunciasPorAsignar > 0 ? 'gu-carga--pendiente' : ''}`}>
                <strong>{f.denunciasPorAsignar ?? 0}</strong> por asignar
            </span>
        );
    }
    if (f.rol === ROLES.VALIDADOR) {
        return (
            <span className="gu-carga">
                <strong>{f.denunciasAsignadas ?? 0}</strong> asignada{f.denunciasAsignadas === 1 ? '' : 's'}
            </span>
        );
    }
    return <span className="gu-celda-tenue">—</span>;
}
