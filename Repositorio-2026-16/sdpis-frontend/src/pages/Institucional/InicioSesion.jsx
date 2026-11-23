import { useEffect, useState } from 'react';
import { createPortal } from 'react-dom';
import { useSesion } from '../../context/SesionContext';
import logoMinisterio from '../../assets/images/logoAzul_Ministerio.png';
import './InicioSesion.css';

// Modal de inicio de sesion (HU-001) segun el mockup acordado: tarjeta de
// 420px con banner navy→azul (logo de fondo tenue + insignia circular),
// campos usuario/correo y contrasena, boton dorado y alerta de error. Se
// muestra como un modal sobre la vista actual (en la misma ventana, igual
// que el antiguo modal de ingreso), no como una vista separada.
export function InicioSesion({ onIngresar, onCerrar }) {
  const { iniciarSesion } = useSesion();
  const [usuario, setUsuario] = useState('');
  const [clave, setClave] = useState('');
  const [verClave, setVerClave] = useState(false);
  const [enviando, setEnviando] = useState(false);
  const [error, setError] = useState(null);

  // Permite cerrar con Escape (heuristica 3: control y libertad del usuario).
  useEffect(() => {
    function manejarTecla(evento) {
      if (evento.key === 'Escape') onCerrar?.();
    }

    document.addEventListener('keydown', manejarTecla);
    return () => document.removeEventListener('keydown', manejarTecla);
  }, [onCerrar]);

  async function manejarEnvio(evento) {
    evento.preventDefault();
    if (enviando) return;

    setError(null);
    setEnviando(true);
    try {
      await iniciarSesion(usuario.trim(), clave);
      onIngresar();
    } catch (e) {
      setError(e.message || 'No fue posible iniciar sesion. Intente nuevamente.');
    } finally {
      setEnviando(false);
    }
  }

  // Se renderiza en un portal (fuera de .pp-transicion-vista) porque ese
  // contenedor anima transform/filter, lo que convierte a cualquier ancestro
  // en el bloque de referencia de los hijos con position:fixed; sin el portal
  // el overlay no se centra respecto a la ventana sino respecto al documento.
  return createPortal(
    <div className="inicio-sesion" role="dialog" aria-modal="true" aria-labelledby="itc-titulo">
      <div className="itc-card">
        <div className="itc-banner">
          <div className="itc-banner-contenido">
            <img className="itc-banner-logo" src={logoMinisterio} alt="Logo del Ministerio de Salud" />
            <h1 className="itc-titulo" id="itc-titulo">Iniciar sesión</h1>
            <div className="itc-sub">Ingrese sus credenciales para continuar</div>
          </div>
          <button type="button" className="itc-cerrar" onClick={onCerrar} aria-label="Cerrar">
            &times;
          </button>
        </div>

        <div className="itc-cuerpo">
          {error && (
            <div className="itc-alert" role="alert">
              {error}
            </div>
          )}

          <form onSubmit={manejarEnvio} noValidate>
            <div className="field">
              <label htmlFor="itc-usuario">Usuario o correo institucional</label>
              <input
                type="text"
                id="itc-usuario"
                value={usuario}
                onChange={(e) => setUsuario(e.target.value)}
                autoComplete="username"
                disabled={enviando}
              />
            </div>

            <div className="field">
              <label htmlFor="itc-clave">Contraseña</label>
              <div className="itc-campo">
                <input
                  type={verClave ? 'text' : 'password'}
                  id="itc-clave"
                  value={clave}
                  onChange={(e) => setClave(e.target.value)}
                  autoComplete="current-password"
                  disabled={enviando}
                />
                <button
                  type="button"
                  className="itc-toggle-clave"
                  onClick={() => setVerClave((v) => !v)}
                  aria-label={verClave ? 'Ocultar contraseña' : 'Mostrar contraseña'}
                  aria-pressed={verClave}
                >
                  {verClave ? (
                    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                      <path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94" />
                      <path d="M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19" />
                      <path d="M14.12 14.12a3 3 0 1 1-4.24-4.24" />
                      <path d="M1 1l22 22" />
                    </svg>
                  ) : (
                    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                      <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8Z" />
                      <circle cx="12" cy="12" r="3" />
                    </svg>
                  )}
                </button>
              </div>
            </div>

            <button type="submit" className="itc-boton" disabled={enviando}>
              {enviando ? 'Ingresando…' : 'Iniciar sesión'}
            </button>
          </form>
        </div>
      </div>
    </div>,
    document.body
  );
}