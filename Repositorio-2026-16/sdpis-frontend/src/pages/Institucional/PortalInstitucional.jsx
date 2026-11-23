import { useState, useEffect } from 'react';
import { createPortal } from 'react-dom';
import { useSesion } from '../../context/SesionContext';
import { MENU_INSTITUCIONAL, VISTAS_INSTITUCIONALES, ROLES_ETIQUETA } from '../../config/permisos';
import logoAzulMinisterio from '../../assets/images/logoBlanco_ministerio.png';

// Shell del portal institucional (lado interno del sistema).
// Reutiliza las clases CSS ya definidas en styles/style.css para el portal:
// .app-shell, .sidebar, .topbar-interna, .content-wrap y .footer-institucional.
//
// HU-015: el menu se arma desde config/permisos.js (MENU_INSTITUCIONAL) y
// cada opcion solo aparece si el usuario tiene alguno de los permisos de
// su vista. Grupos sin opciones visibles no se muestran.
//
// props:
// - tituloSeccion: string — título para la miga de pan / topbar
// - onCerrarSesion: () => void — limpia la sesión y vuelve al login
// - onNavegar: (vista) => void — navega entre vistas institucionales
// - vistaActual: string — vista actualmente seleccionada
// - colapsado: boolean — menu lateral reducido a iconos (solo escritorio)
// - onAlternarColapso: () => void — reduce o amplia el menu lateral
// - usuario: UsuarioAutenticadoDto | null — usuario de la sesión (HU-001)
// - children: contenido de la sección institucional activa
export function PortalInstitucional({ tituloSeccion = 'Portal institucional', onCerrarSesion, onNavegar, vistaActual, usuario, colapsado = false, onAlternarColapso, children }) {
  const [sidebarAbiertaMovil, setSidebarAbiertaMovil] = useState(false);
  const [guiaVisual, setGuiaVisual] = useState(null);
  const { tieneAlgunPermiso, permisosCargados, rolEfectivo } = useSesion();

  const clasesSidebar = ['sidebar'];
  if (colapsado) clasesSidebar.push('sidebar--colapsado');
  if (sidebarAbiertaMovil) clasesSidebar.push('abierto');

  function cerrarSidebarMovil() {
    setSidebarAbiertaMovil(false);
  }

  function ocultarGuia() {
    setGuiaVisual(null);
  }

  function mostrarGuia(evento, titulo, categoria, subtitulo) {
    if (!colapsado) return;
    const elemento = evento.currentTarget;
    const rect = elemento.getBoundingClientRect();
    const centroY = rect.top + rect.height / 2;
    const topClamped = Math.max(16, Math.min(window.innerHeight - 20, centroY));
    setGuiaVisual({
      titulo,
      categoria,
      subtitulo,
      top: Math.round(topClamped),
      left: Math.round(rect.right + 12),
    });
  }

  useEffect(() => {
    if (!guiaVisual) return undefined;
    function cerrar() {
      setGuiaVisual(null);
    }
    window.addEventListener('scroll', cerrar, true);
    window.addEventListener('resize', cerrar);
    return () => {
      window.removeEventListener('scroll', cerrar, true);
      window.removeEventListener('resize', cerrar);
    };
  }, [guiaVisual]);

  function navegar(evento, vista) {
    evento.preventDefault();
    ocultarGuia();
    if (onNavegar) onNavegar(vista);
    cerrarSidebarMovil();
  }

  const nombreCompleto = usuario
    ? `${usuario.nombre} ${usuario.apellido1}${usuario.apellido2 ? ` ${usuario.apellido2}` : ''}`
    : 'Funcionario';
  const rol = rolEfectivo ?? usuario?.rol;
  const rolEtiqueta = rol ? (ROLES_ETIQUETA[rol] ?? rol) : '—';
  const inicial = nombreCompleto.trim().charAt(0).toUpperCase() || 'F';

  const gruposVisibles = MENU_INSTITUCIONAL
    .map((grupo) => ({
      ...grupo,
      items: grupo.items.filter((item) => tieneAlgunPermiso(VISTAS_INSTITUCIONALES[item.vista]?.permisos)),
    }))
    .filter((grupo) => grupo.items.length > 0);

  return (
    <div className="app-shell">
      {sidebarAbiertaMovil && (
        <div className="sidebar-overlay" onClick={cerrarSidebarMovil} aria-hidden="true" />
      )}

      <aside className={clasesSidebar.join(' ')}>
        <div className="sidebar-header">
          <img className="sidebar-logo" src={logoAzulMinisterio} alt="Ministerio de Salud de Costa Rica" />
          <button
            type="button"
            className="btn-sidebar-cerrar"
            onClick={cerrarSidebarMovil}
            aria-label="Cerrar menú"
          >
            ✕
          </button>
        </div>

        {/* Boton sobre la linea que separa el menu del contenido (a media altura;
            arriba cuando el menu esta reducido). */}
        {onAlternarColapso && (
          <button
            type="button"
            className="btn-sidebar-colapsar"
            onClick={(e) => {
              ocultarGuia();
              onAlternarColapso(e);
            }}
            onMouseEnter={(e) => colapsado && mostrarGuia(e, 'Ampliar menú', 'Navegación')}
            onMouseLeave={ocultarGuia}
            onFocus={(e) => colapsado && mostrarGuia(e, 'Ampliar menú', 'Navegación')}
            onBlur={ocultarGuia}
            aria-label={colapsado ? 'Ampliar el menú' : 'Reducir el menú'}
            aria-expanded={!colapsado}
            title={colapsado ? undefined : 'Reducir el menú'}
          >
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.6" aria-hidden="true">
              <polyline points={colapsado ? '9 6 15 12 9 18' : '15 6 9 12 15 18'} />
            </svg>
          </button>
        )}

        <nav aria-label="Menú principal">
          <ul className="sidebar-menu">
            {!permisosCargados ? (
              <li className="menu-group-label">Cargando menú…</li>
            ) : (
              <>
                <li className="menu-group-label">Gestión</li>
                <li className="menu-item">
                  <div
                    className="menu-link"
                    onMouseEnter={(e) => colapsado && mostrarGuia(e, 'Denuncias', 'Gestión', 'Próximamente')}
                    onMouseLeave={ocultarGuia}
                    onFocus={(e) => colapsado && mostrarGuia(e, 'Denuncias', 'Gestión', 'Próximamente')}
                    onBlur={ocultarGuia}
                    title={colapsado ? undefined : 'Disponible próximamente'}
                  >
                    <span className="icono" aria-hidden="true">▧</span>
                    <span className="menu-label">Denuncias</span>
                  </div>
                </li>

                {gruposVisibles.map((grupo) => (
                  <MenuGrupo
                    key={grupo.grupo}
                    grupo={grupo}
                    mostrarEtiqueta={grupo.grupo !== 'Gestión'}
                    vistaActual={vistaActual}
                    onNavegar={navegar}
                    colapsado={colapsado}
                    onMostrarGuia={mostrarGuia}
                    onOcultarGuia={ocultarGuia}
                  />
                ))}
              </>
            )}
          </ul>
        </nav>

        <div className="sidebar-footer">
          <div
            className="usuario-card"
            onMouseEnter={(e) => colapsado && mostrarGuia(e, nombreCompleto, 'Perfil de usuario', rolEtiqueta)}
            onMouseLeave={ocultarGuia}
            onFocus={(e) => colapsado && mostrarGuia(e, nombreCompleto, 'Perfil de usuario', rolEtiqueta)}
            onBlur={ocultarGuia}
            tabIndex={colapsado ? 0 : undefined}
          >
            <div className="usuario-avatar" aria-hidden="true">{inicial}</div>
            <div className="usuario-info">
              <div className="usuario-nombre">{nombreCompleto}</div>
              <div className="usuario-rol">{rolEtiqueta}</div>
            </div>
          </div>
          <button
            type="button"
            className="btn-logout"
            onClick={(e) => {
              ocultarGuia();
              onCerrarSesion(e);
            }}
            onMouseEnter={(e) => colapsado && mostrarGuia(e, 'Cerrar sesión', 'Sesión')}
            onMouseLeave={ocultarGuia}
            onFocus={(e) => colapsado && mostrarGuia(e, 'Cerrar sesión', 'Sesión')}
            onBlur={ocultarGuia}
            title={colapsado ? undefined : 'Cerrar sesión'}
            aria-label="Cerrar sesión"
          >
            <span className="logout-icono" aria-hidden="true">⏻</span>
            <span className="logout-texto">Cerrar sesión</span>
          </button>
        </div>
      </aside>

      <div className="main-content">
        <header className="topbar-interna">
          <button
            type="button"
            className="btn-hamburger"
            onClick={() => setSidebarAbiertaMovil((v) => !v)}
            aria-label="Abrir menú"
          >
            ☰
          </button>
          <div className="breadcrumbs">
            <span>Institucional</span>
            <span aria-hidden="true">›</span>
            <span className="actual">{tituloSeccion}</span>
          </div>
        </header>

        <div className="content-wrap" id="disponibilidad">
          {children}
        </div>

        <footer className="footer-institucional">
          <div className="footer-texto">
            Sistema de Denuncias de Productos de Interés Sanitario - Ministerio de Salud, Costa Rica
          </div>
        </footer>
      </div>

      {colapsado && guiaVisual && createPortal(
        <div
          className="sidebar-guia-visual"
          style={{ top: `${guiaVisual.top}px`, left: `${guiaVisual.left}px` }}
          role="tooltip"
          aria-hidden="true"
        >
          <div className="sidebar-guia-visual__flecha" />
          {guiaVisual.categoria && (
            <span className="sidebar-guia-visual__categoria">{guiaVisual.categoria}</span>
          )}
          <div className="sidebar-guia-visual__titulo-fila">
            <span className="sidebar-guia-visual__titulo">{guiaVisual.titulo}</span>
            {guiaVisual.subtitulo && (
              <span className={`sidebar-guia-visual__badge ${guiaVisual.subtitulo === 'Actual' ? 'sidebar-guia-visual__badge--activo' : ''}`}>
                {guiaVisual.subtitulo}
              </span>
            )}
          </div>
        </div>,
        document.body
      )}
    </div>
  );
}

// Iconos dibujados (los demas del menu son glifos de texto). Se referencian
// por nombre desde config/permisos.js (campo icono).
const ICONOS_SVG = {
  cuaderno: (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8"
      strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      <path d="M6 3h11a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H6z" />
      <line x1="6" y1="3" x2="6" y2="21" />
      <line x1="10" y1="8" x2="16" y2="8" />
      <line x1="10" y1="12" x2="16" y2="12" />
      <line x1="10" y1="16" x2="14" y2="16" />
      <line x1="3" y1="7" x2="6" y2="7" />
      <line x1="3" y1="12" x2="6" y2="12" />
      <line x1="3" y1="17" x2="6" y2="17" />
    </svg>
  ),
};

function MenuGrupo({ grupo, mostrarEtiqueta, vistaActual, onNavegar, colapsado, onMostrarGuia, onOcultarGuia }) {
  return (
    <>
      {mostrarEtiqueta && <li className="menu-group-label">{grupo.grupo}</li>}
      {grupo.items.map((item) => {
        const activo = vistaActual === item.vista;
        return (
          <li key={item.vista} className={`menu-item ${activo ? 'active' : ''}`}>
            <a
              className="menu-link"
              href={`#${item.ancla}`}
              title={colapsado ? undefined : item.etiqueta}
              onClick={(e) => {
                if (onOcultarGuia) onOcultarGuia();
                onNavegar(e, item.vista);
              }}
              onMouseEnter={(e) => colapsado && onMostrarGuia && onMostrarGuia(e, item.etiqueta, grupo.grupo, activo ? 'Actual' : undefined)}
              onMouseLeave={onOcultarGuia}
              onFocus={(e) => colapsado && onMostrarGuia && onMostrarGuia(e, item.etiqueta, grupo.grupo, activo ? 'Actual' : undefined)}
              onBlur={onOcultarGuia}
              aria-current={activo ? 'page' : undefined}
            >
              <span className="icono" aria-hidden="true">{ICONOS_SVG[item.icono] ?? item.icono}</span>
              <span className="menu-label">{item.etiqueta}</span>
            </a>
          </li>
        );
      })}
    </>
  );
}
