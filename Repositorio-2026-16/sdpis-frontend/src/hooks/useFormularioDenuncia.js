import { useState, useEffect } from 'react';

const PRODUCTO_VACIO = {
    nombre: '',
    descripcion: '',
    registroSanitario: '',
    marca: '',
    lote: '',
    fabricante: '',
    paisOrigen: '',
    presentacion: '',
    fechaCompra: '',
    tipoProducto: '',
    motivos: [],
};

const CLAVE = 'sdpis:denuncia-en-curso';

function denuncianteVacio() {
    return {
        tipoTramite: 'confidencial',
        nombreCompleto: '',
        tipoIdentificacion: 'nacional', // 'nacional' | 'extranjero' (solo para validar en el cliente)
        numeroIdentificacion: '',
        direccionDomicilio: '',
        correo: '',
        telefono: '',
    };
}

function crearEstadoInicial() {
    const guardado = sessionStorage.getItem(CLAVE);
    const base = guardado ? JSON.parse(guardado) : null;

    if (base) {
        return { ...base, denunciante: denuncianteVacio() };
    }

    return {
        paso: 1,
        ubicacion: { provincia: '', canton: '', distrito: '', direccionExacta: '' },
        hecho: {
            nombreEstablecimiento: '',
            descripcion: '',
            presentadaOtraInstitucion: '',
            detalleOtraInstitucion: '',
            presentadaMinisterioPrevio: '',
            detallePrevioMinisterio: '',
        },
        productos: [{ ...PRODUCTO_VACIO }],
        denunciante: denuncianteVacio(),
    };
}

const TOTAL_PASOS = 4;

export function useFormularioDenuncia() {
    const [estado, setEstado] = useState(crearEstadoInicial);

    useEffect(() => {
        const { denunciante, ...resto } = estado;
        sessionStorage.setItem(CLAVE, JSON.stringify(resto));
    }, [estado]);

    function empujarPaso(nuevoPaso) {
        history.pushState({ ...history.state, paso: nuevoPaso }, '');
    }

    useEffect(() => {
        if (history.state?.paso === undefined) {
            history.replaceState({ ...history.state, paso: estado.paso }, '');
        }

        function alPopstate(evento) {
            const pasoDestino = evento.state?.paso;
            if (pasoDestino !== undefined) {
                setEstado((prev) => ({ ...prev, paso: pasoDestino }));
            }
        }

        window.addEventListener('popstate', alPopstate);
        return () => window.removeEventListener('popstate', alPopstate);
    }, []);

    function actualizarUbicacion(ubicacion) {
        setEstado((prev) => ({ ...prev, ubicacion }));
    }

    function actualizarHecho(hecho) {
        setEstado((prev) => ({ ...prev, hecho }));
    }

    function actualizarDenunciante(denunciante) {
        setEstado((prev) => ({ ...prev, denunciante }));
    }

    function actualizarProducto(index, productoActualizado) {
        setEstado((prev) => ({
            ...prev,
            productos: prev.productos.map((p, i) => (i === index ? productoActualizado : p)),
        }));
    }

    function agregarProducto() {
        setEstado((prev) => ({ ...prev, productos: [...prev.productos, { ...PRODUCTO_VACIO }] }));
    }

    function eliminarProducto(index) {
        setEstado((prev) => ({ ...prev, productos: prev.productos.filter((_, i) => i !== index) }));
    }

    function irSiguientePaso() {
        const nuevoPaso = Math.min(estado.paso + 1, TOTAL_PASOS);
        empujarPaso(nuevoPaso);
        setEstado((prev) => ({ ...prev, paso: nuevoPaso }));
    }

    function irPasoAnterior() {
        const nuevoPaso = Math.max(estado.paso - 1, 1);
        empujarPaso(nuevoPaso);
        setEstado((prev) => ({ ...prev, paso: nuevoPaso }));
    }

    // Usado desde el paso de revisión (heurística 6: Reconocer antes que
    // recordar) para saltar directamente a la sección que se desea corregir.
    function irAPaso(numero) {
        const nuevoPaso = Math.min(Math.max(numero, 1), TOTAL_PASOS);
        empujarPaso(nuevoPaso);
        setEstado((prev) => ({ ...prev, paso: nuevoPaso }));
    }


    function reiniciarFormulario() {
        sessionStorage.removeItem(CLAVE);
        setEstado(crearEstadoInicial());
    }

    return {
        estado,
        totalPasos: TOTAL_PASOS,
        actualizarUbicacion,
        actualizarHecho,
        actualizarDenunciante,
        actualizarProducto,
        agregarProducto,
        eliminarProducto,
        irSiguientePaso,
        irPasoAnterior,
        irAPaso,
        reiniciarFormulario,
    };
}