import { useEffect, useState } from 'react';
import { useNavegacionAnimada } from './hooks/useNavegacionAnimada';
import { useVigilanteInactividad } from './hooks/useVigilanteInactividad';
import { SesionProvider, useSesion } from './context/SesionContext';
import { PaginaPrincipal } from './pages/Publico/PaginaPrincipal/PaginaPrincipal';
import { FormularioDenuncia } from './pages/Publico/FormularioDenuncia/FormularioDenuncia';
import { PortalInstitucional } from './pages/Institucional/PortalInstitucional';
import { GestionDisponibilidadFuncionario } from './pages/Institucional/GestionDisponibilidadFuncionario';
import { AsignacionManualPage } from './pages/Institucional/AsignacionManualPage';
import { AsignacionEncargadoPage } from './pages/Institucional/AsignacionEncargadoPage';
import { CriteriosTecnicosPage } from './pages/Institucional/CriteriosTecnicosPage';
import { BitacoraPage } from './pages/Institucional/BitacoraPage';
import { SolicitudesCierrePage } from './pages/Institucional/SolicitudesCierrePage';
import { InicioSesion } from './pages/Institucional/InicioSesion';
import { GestionUsuariosPage } from './pages/Institucional/GestionUsuarios/GestionUsuariosPage';
import { PlazosAtencionPage } from './pages/Institucional/PlazosAtencionPage';
import { AccesoRestringido } from './pages/Institucional/GestionUsuarios/AccesoRestringido';
import { BienvenidaInstitucional } from './pages/Institucional/BienvenidaInstitucional';
import { VISTAS_INSTITUCIONALES } from './config/permisos';

// Navegación mínima entre las vistas actuales, con una transición animada de
// salida/entrada (ver useNavegacionAnimada). Vistas:
// - 'inicio'        : portal público (PaginaPrincipal)
// - 'denuncia'      : formulario público de denuncia (HU-002)
// - 'institucional' : portal interno (sidebar), requiere sesión activa
// El inicio de sesión (HU-001) no es una vista: se abre como un modal sobre
// la vista actual (InicioSesion), igual que el antiguo modal de ingreso, para
// no cambiar a una pantalla aparte.
// HU-015: cada vista institucional exige alguno de los permisos definidos en
// config/permisos.js (VISTAS_INSTITUCIONALES); si no los tiene se muestra
// "Acceso restringido" aunque llegue por URL o por un enlace viejo.
// El proyecto no tiene un enrutador instalado todavía; cuando se agregue uno
// (p. ej. react-router-dom), solo cambia irA/vista, ya que las páginas no
// conocen cómo está implementada la navegación.
// TODO: instalar react-router-dom para evitar manejar el refrescamiento con condicionales
function Aplicacion() {
  const { vista, transicion, irA } = useNavegacionAnimada('inicio');
  const { sesion, cerrarSesion, restaurarSesion, permisosCargados, tieneAlgunPermiso } = useSesion();
  const [loginAbierto, setLoginAbierto] = useState(false);
  // Menu lateral reducido a iconos: boton de la cabecera del menu y, ademas,
  // al elegir una denuncia (Bitacora, Criterios tecnicos) o un funcionario (Disponibilidad) (mas ancho de trabajo).
  const [menuColapsado, setMenuColapsado] = useState(false);

  // Al cargar, si hay un token guardado lo valida contra el backend; si ya
  // expiró (401 sesion_expirada) se limpia la sesión y se queda en público.
  useEffect(() => {
    if (sesion?.token) {
      restaurarSesion().then((valida) => {
        if (valida && vista === 'inicio') irA('institucional-inicio', true);
      });
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const abrirLogin = () => setLoginAbierto(true);
  const cerrarLogin = () => setLoginAbierto(false);

  // Si la sesión expiró mientras se estaba dentro del portal, se vuelve al
  // sitio público con el modal de ingreso abierto para autenticarse de nuevo.
  useEffect(() => {
    if (!sesion?.token && vista.startsWith('institucional')) {
      irA('inicio', true);
      const temporizador = setTimeout(abrirLogin, 0);
      return () => clearTimeout(temporizador);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [sesion?.token]);

  const cerrarSesionConSalida = (motivo = 'manual') => {
    cerrarSesion(motivo);
    irA('inicio');
    abrirLogin();
  };

  // HU-028: cierre automatico por inactividad. Solo vigila mientras existe
  // sesion; al vencer el limite ejecuta el mismo cierre que el boton manual
  // (revoca el token en el servidor, limpia lo local y vuelve al login),
  // pero el backend lo registra como CIERRE_INACTIVIDAD.
  useVigilanteInactividad(() => cerrarSesionConSalida('inactividad'), Boolean(sesion?.token));

  const configVista = VISTAS_INSTITUCIONALES[vista] ?? VISTAS_INSTITUCIONALES.institucional;
  const vistaPermitida = tieneAlgunPermiso(configVista.permisos);

  function contenidoInstitucional() {
    if (!permisosCargados) return <div className="gu-cargando-vista">Cargando permisos…</div>;
    if (!vistaPermitida) return <AccesoRestringido onVolver={() => irA('institucional-inicio', true)} />;

    switch (vista) {
      case 'institucional-asignaciones':
        return <AsignacionManualPage />;
      case 'institucional-encargado':
        return <AsignacionEncargadoPage />;
      case 'institucional-criterios-tecnicos':
        return <CriteriosTecnicosPage onDenunciaSeleccionada={() => setMenuColapsado(true)} />;
      case 'institucional-bitacora':
        return <BitacoraPage onDenunciaSeleccionada={() => setMenuColapsado(true)} />;
      case 'institucional-solicitudes-cierre':
        return <SolicitudesCierrePage />;
      case 'institucional-gestion-usuarios':
        return <GestionUsuariosPage />;
      case 'institucional-plazos':
        return <PlazosAtencionPage />;
      case 'institucional-inicio':
        return <BienvenidaInstitucional onNavegar={(v) => irA(v, true)} />;
      default:
        return (
          <GestionDisponibilidadFuncionario
            usuarioLogueado={sesion?.usuario?.identificacion ?? null}
            onFuncionarioSeleccionado={() => setMenuColapsado(true)}
          />
        );
    }
  }

  return (
    <div className={`pp-transicion-vista pp-transicion-vista--${transicion}`}>
      {vista === 'denuncia' ? (
        <FormularioDenuncia onVolverInicio={() => irA('inicio')} />
      ) : vista.startsWith('institucional') ? (
        <PortalInstitucional
          tituloSeccion={configVista.titulo}
          onCerrarSesion={() => cerrarSesionConSalida('manual')}
          onNavegar={(v) => { setMenuColapsado(false); irA(v, true); }}
          vistaActual={vista}
          colapsado={menuColapsado}
          onAlternarColapso={() => setMenuColapsado((c) => !c)}
          usuario={sesion?.usuario ?? null}
        >
          {contenidoInstitucional()}
        </PortalInstitucional>
      ) : (
        <PaginaPrincipal
          onIrADenuncia={() => irA('denuncia')}
          onIrALogin={abrirLogin}
        />
      )}

      {loginAbierto && (
        <InicioSesion
          onIngresar={() => {
            cerrarLogin();
            irA('institucional-inicio', true);
          }}
          onCerrar={cerrarLogin}
        />
      )}
    </div>
  );
}

function App() {
  return (
    <SesionProvider>
      <Aplicacion />
    </SesionProvider>
  );
}

export default App;
