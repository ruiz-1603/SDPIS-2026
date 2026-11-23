import { useEffect, useRef } from 'react';

// Limite de inactividad para el cierre automatico de sesion (HU-028).
// Se usa 15 minutos: menos que la vida del JWT (60 min), asi el cierre por
// inactividad siempre se dispara antes de que el token expire por sí mismo.
export const MINUTOS_INACTIVIDAD = 15;
export const LIMITE_INACTIVIDAD_MS = MINUTOS_INACTIVIDAD * 60 * 1000;

const EVENTOS_ACTIVIDAD = ['click', 'keydown', 'mousemove', 'scroll', 'touchstart'];

// Vigila la actividad del usuario mientras la sesion esta activa. Cada evento
// reinicia un temporizador; si pasa el limite sin actividad, invoca la accion
// (p. ej. cerrar la sesion y volver al login). No usa la accion en deps
// (con un ref siempre llama a la ultima version sin re-armar los listeners).
export function useVigilanteInactividad(accion, activo = false) {
    const accionRef = useRef(accion);

    useEffect(() => {
        accionRef.current = accion;
    });

    useEffect(() => {
        if (!activo) return undefined;

        let temporizador;

        function reiniciar() {
            window.clearTimeout(temporizador);
            temporizador = window.setTimeout(() => accionRef.current(), LIMITE_INACTIVIDAD_MS);
        }

        EVENTOS_ACTIVIDAD.forEach((nombre) =>
            window.addEventListener(nombre, reiniciar, { passive: true })
        );
        reiniciar();

        return () => {
            window.clearTimeout(temporizador);
            EVENTOS_ACTIVIDAD.forEach((nombre) => window.removeEventListener(nombre, reiniciar));
        };
    }, [activo]);
}