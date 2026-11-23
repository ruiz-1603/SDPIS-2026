import { useCallback, useEffect, useRef, useState } from 'react';

const DURACION_TRANSICION_MS = 420;

export function useNavegacionAnimada(vistaInicial) {
    const [vista, setVista] = useState(() => history.state?.vista ?? vistaInicial);
    const [transicion, setTransicion] = useState('entrada');
    const temporizador = useRef(null);

    const irA = useCallback((nuevaVista, sinAnimacion = false) => {
        clearTimeout(temporizador.current);

        if (sinAnimacion) {
            setVista(nuevaVista);
            setTransicion('entrada');
            history.pushState({ vista: nuevaVista }, '');
            return;
        }

        setTransicion('salida');
        temporizador.current = setTimeout(() => {
            setVista(nuevaVista);
            setTransicion('entrada');
            history.pushState({ vista: nuevaVista }, '');
        }, DURACION_TRANSICION_MS);
    }, []);

    useEffect(() => {
        if (history.state?.vista === undefined) {
            history.replaceState({ vista: vistaInicial }, '');
        }

        function alPopstate(evento) {
            clearTimeout(temporizador.current);
            setTransicion('salida');
            temporizador.current = setTimeout(() => {
                setVista(evento.state?.vista ?? vistaInicial);
                setTransicion('entrada');
            }, DURACION_TRANSICION_MS);
        }

        window.addEventListener('popstate', alPopstate);
        return () => window.removeEventListener('popstate', alPopstate);
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);

    return { vista, transicion, irA };
}