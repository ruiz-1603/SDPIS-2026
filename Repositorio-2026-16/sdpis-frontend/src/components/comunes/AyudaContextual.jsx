import { useEffect, useId, useLayoutEffect, useRef, useState } from 'react';
import { createPortal } from 'react-dom';

const ANCHO_PANEL = 220;
const MARGEN_VIEWPORT = 12;

// etiqueta: string — texto accesible del botón (ej. "Ayuda sobre tipo de producto")
// texto: string — contenido explicativo mostrado en el panel de ayuda
//
// Botón "?" de ayuda contextual (patrón "disclosure"). El panel se renderiza
// en un portal sobre document.body y se posiciona con las coordenadas reales
// del botón (getBoundingClientRect). Esto es necesario porque contenedores
// como .producto-block y .card usan overflow:hidden para recortar sus
// esquinas redondeadas — si el panel fuera position:absolute dentro de ese
// árbol, quedaría recortado por ese mismo overflow en vez de flotar sobre
// el contenido.
export function AyudaContextual({ etiqueta, texto }) {
    const [abierto, setAbierto] = useState(false);
    const [posicion, setPosicion] = useState(null);
    const botonRef = useRef(null);
    const panelRef = useRef(null);
    const idPanel = useId();

    useLayoutEffect(() => {
        if (!abierto || !botonRef.current) return;

        const rectBoton = botonRef.current.getBoundingClientRect();
        const maxLeft = window.innerWidth - ANCHO_PANEL - MARGEN_VIEWPORT;
        const left = Math.min(rectBoton.left, Math.max(MARGEN_VIEWPORT, maxLeft));

        setPosicion({ top: rectBoton.bottom + 6, left });
    }, [abierto]);

    useEffect(() => {
        if (!abierto) return undefined;

        function cerrar() {
            setAbierto(false);
        }

        function manejarTecla(evento) {
            if (evento.key === 'Escape') cerrar();
        }

        function manejarClicFuera(evento) {
            const dentroDelBoton = botonRef.current?.contains(evento.target);
            const dentroDelPanel = panelRef.current?.contains(evento.target);
            if (!dentroDelBoton && !dentroDelPanel) cerrar();
        }

        document.addEventListener('keydown', manejarTecla);
        document.addEventListener('mousedown', manejarClicFuera);
        // Si la página se desplaza o cambia de tamaño, el panel podría quedar
        // desalineado del botón (es position:fixed vía portal); cerrarlo es
        // más simple y confiable que recalcular su posición en cada scroll.
        window.addEventListener('scroll', cerrar, true);
        window.addEventListener('resize', cerrar);
        return () => {
            document.removeEventListener('keydown', manejarTecla);
            document.removeEventListener('mousedown', manejarClicFuera);
            window.removeEventListener('scroll', cerrar, true);
            window.removeEventListener('resize', cerrar);
        };
    }, [abierto]);

    return (
        <>
            <button
                ref={botonRef}
                type="button"
                className="ayuda-contextual__boton"
                aria-expanded={abierto}
                aria-describedby={abierto ? idPanel : undefined}
                aria-label={etiqueta}
                onClick={() => setAbierto((valor) => !valor)}
            >
                ?
            </button>

            {abierto &&
                posicion &&
                createPortal(
                    <span
                        ref={panelRef}
                        id={idPanel}
                        role="note"
                        className="ayuda-contextual__panel"
                        style={{ top: posicion.top, left: posicion.left }}
                    >
                        {texto}
                    </span>,
                    document.body
                )}
        </>
    );
}