import { createContext, useContext, useMemo, useState, useCallback, useEffect } from 'react';
import {
    iniciarSesion as iniciarSesionService,
    obtenerSesionActual,
    cerrarSesion as cerrarSesionService,
    guardarSesionLocal,
    leerSesionLocal,
} from '../services/autenticacionService';
import { obtenerMisPermisos } from '../services/gestionUsuariosService';
import { EVENTO_SESION_INVALIDA } from '../services/apiCliente';
import { derivarRol } from '../config/permisos';

// Sesion = { token, usuario, expiraEn }. El token vive en sessionStorage
// (decision de Fase 1): sobrevive al refresco de la pestana pero se pierde
// al cerrarla. El contexto centraliza el estado para que cualquier pagina
// institucional lea el usuario sin prop-drilling.
//
// HU-015: los permisos NO van en el token. Se piden al backend
// (GET /gestion-usuarios/mis-permisos) al iniciar sesion, al recargar y
// cuando una accion los cambia (refrescarPermisos), para que una revocacion
// rija de inmediato. permisos = null mientras cargan.
const SesionContext = createContext(null);

export function SesionProvider({ children }) {
    const [sesion, setSesion] = useState(() => leerSesionLocal());
    // Permisos junto al token para el que se pidieron: si el token cambia
    // (otro login, cierre de sesion), los anteriores dejan de valer solos,
    // sin tener que limpiarlos dentro de un efecto.
    const [permisosPorToken, setPermisosPorToken] = useState({ token: null, datos: null });

    const refrescarPermisos = useCallback(async () => {
        const token = leerSesionLocal()?.token ?? null;
        let datos;
        try {
            datos = await obtenerMisPermisos();
        } catch {
            // Sin permisos: el menu queda vacio y las vistas muestran
            // "acceso restringido"; un 401 ademas cierra la sesion.
            datos = { permisos: [], alcance: 'NINGUNA', error: true };
        }
        setPermisosPorToken({ token, datos });
        return datos;
    }, []);

    useEffect(() => {
        if (sesion?.token) refrescarPermisos();
    }, [sesion?.token, refrescarPermisos]);

    const permisos = sesion?.token && permisosPorToken.token === sesion.token ? permisosPorToken.datos : null;

    // Cualquier 401 de solicitarApi (token vencido o revocado) limpia la
    // sesion; App detecta la falta de token y abre el login.
    useEffect(() => {
        async function alInvalidarse() {
            await cerrarSesionService();
            setSesion(null);
        }
        window.addEventListener(EVENTO_SESION_INVALIDA, alInvalidarse);
        return () => window.removeEventListener(EVENTO_SESION_INVALIDA, alInvalidarse);
    }, []);

    const iniciarSesion = useCallback(async (codigoUsuario, clave) => {
        const sesionNueva = await iniciarSesionService(codigoUsuario, clave);
        guardarSesionLocal(sesionNueva);
        setSesion(sesionNueva);
        return sesionNueva;
    }, []);

    // Valida el token guardado al cargar la app; si expiro (401), limpia.
    const restaurarSesion = useCallback(async () => {
        const guardada = leerSesionLocal();
        if (!guardada?.token) return null;
        try {
            const usuario = await obtenerSesionActual(guardada.token);
            const restaurada = { ...guardada, usuario };
            guardarSesionLocal(restaurada);
            setSesion(restaurada);
            return restaurada;
        } catch {
            cerrarSesionService();
            setSesion(null);
            return null;
        }
    }, []);

    // HU-028: cierre de sesion. Revoca el JWT en el servidor (POST /logout)
    // y despues limpia el estado local y sessionStorage. Espera al backend
    // como buena practica (asi el token queda invalidado de verdad antes de
    // seguir), aunque el servicio nunca lanza.
    const cerrarSesion = useCallback(async (motivo = 'manual') => {
        await cerrarSesionService(motivo);
        setSesion(null);
    }, []);

    const tienePermiso = useCallback(
        (codigo) => Boolean(permisos?.permisos?.includes(codigo)),
        [permisos]
    );

    // Lista vacia = no exige permiso.
    const tieneAlgunPermiso = useCallback(
        (codigos) => !codigos?.length || codigos.some((c) => permisos?.permisos?.includes(c)),
        [permisos]
    );

    const valor = useMemo(
        () => ({
            sesion,
            iniciarSesion,
            restaurarSesion,
            cerrarSesion,
            permisos: permisos?.permisos ?? [],
            alcance: permisos?.alcance ?? 'NINGUNA',
            permisosCargados: permisos !== null,
            rolEfectivo: permisos?.permisos?.length
                ? derivarRol(permisos.permisos)
                : sesion?.usuario?.rol ?? null,
            tienePermiso,
            tieneAlgunPermiso,
            refrescarPermisos,
        }),
        [sesion, iniciarSesion, restaurarSesion, cerrarSesion, permisos, tienePermiso, tieneAlgunPermiso, refrescarPermisos]
    );

    return <SesionContext.Provider value={valor}>{children}</SesionContext.Provider>;
}

// eslint-disable-next-line react-refresh/only-export-components -- archivo de contexto: exporta hook + provider
export function useSesion() {
    const contexto = useContext(SesionContext);
    if (!contexto) throw new Error('useSesion debe usarse dentro de <SesionProvider>.');
    return contexto;
}
