import { normalizarCamposVacios } from '../utils/normalizarCamposVacios';

const URL_API_DENUNCIAS = 'http://localhost:5290/api/denuncias';
const USAR_MOCK = false;

const MOTIVOS_MAP = {
    'Adulteración': 1,
    'Calidad': 2,
    'Etiquetado': 3,
    'Falsificación': 4,
    'Indicaciones no Aprobadas': 5,
    'Intoxicación': 6,
    'Irregularidades en el registro': 7,
    'Producto vencido': 8,
    'Publicidad': 9,
    'Seguridad': 10,
    'Sin Registro Sanitario': 11,
};

const TIPOS_PRODUCTO_MAP = {
    'Alimentos': 1,
    'Cannabis/Cáñamo': 2,
    'Cosméticos': 3,
    'Equipos y materiales biomédicos (EMB)': 4,
    'Medicamentos': 5,
    'Plaguicidas': 6,
    'Productos Higiénicos': 7,
    'Productos Naturales': 8,
    'Productos Químicos': 9,
    'Suplementos': 10,
    'Tintas para Tatuaje': 11,
    'Líquidos para vapeo': 12,
    'Materias Primas': 13,
};

function mapearFormularioARequest(datosFormulario, obtenerIdCanton, obtenerIdDistrito) {
    const { ubicacion, hecho, denunciante, productos } = datosFormulario;
    const esAnonima = denunciante.tipoTramite === 'anonima';
    //const esConfidencial = denunciante.tipoTramite === 'confidencial';

    return {
        hecho: {
            denunciadoOtraInstitucion: hecho.presentadaOtraInstitucion === 'Sí' ? 'S' : 'N',
            detalleOtraInstitucion: hecho.detalleOtraInstitucion,
            denunciadoPreviamenteMinsalud: hecho.presentadaMinisterioPrevio === 'Sí' ? 'S' : 'N',
            detallePrevioMinsalud: hecho.detallePrevioMinisterio,
            nombreEstablecimiento: hecho.nombreEstablecimiento,
            descripcionHecho: hecho.descripcion,
        },
        ubicacion: {
            cantonId: obtenerIdCanton(ubicacion.provincia, ubicacion.canton),
            distritoId: obtenerIdDistrito(ubicacion.provincia, ubicacion.canton, ubicacion.distrito),
            direccionExacta: ubicacion.direccionExacta,
        },
        privacidad: {
            esAnonima: esAnonima ? 'S' : 'N',
            denunciante: esAnonima
                ? null
                : {
                    nombre: denunciante.nombreCompleto,
                    numeroIdentificacion: denunciante.numeroIdentificacion,
                    correo: denunciante.correo,
                    telefono: denunciante.telefono,
                    direccionDomicilio: denunciante.direccionDomicilio,
                },
        },
        productos: productos.map((p) => ({
            tipoProductoId: TIPOS_PRODUCTO_MAP[p.tipoProducto] || null,
            nombreProductoTexto: p.nombre,
            registroSanitarioIngresado: p.registroSanitario,
            descripcionProducto: p.descripcion,
            marcaIngresada: p.marca,
            numeroLote: p.lote,
            fabricanteIngresado: p.fabricante,
            paisOrigen: p.paisOrigen,
            presentacion: p.presentacion,
            fechaCompra: p.fechaCompra || null,
            motivos: p.motivos.map((m) => MOTIVOS_MAP[m]).filter((id) => id !== undefined),
        })),
    };
}

export async function registrarDenuncia(datosFormulario, obtenerIdCanton, obtenerIdDistrito) {
    const payload = normalizarCamposVacios(
        mapearFormularioARequest(datosFormulario, obtenerIdCanton, obtenerIdDistrito)
    );

    if (USAR_MOCK) {
        await new Promise((resolve) => setTimeout(resolve, 600));
        return { numeroDenuncia: `SDPIS-${Date.now()}` };
    }

    const respuesta = await fetch(URL_API_DENUNCIAS, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload),
    });

    if (!respuesta.ok) {
        const errorBody = await respuesta.json().catch(() => null);
        console.error('Error del backend:', errorBody);
        throw new Error('No fue posible registrar la denuncia');
    }

    return respuesta.json();
}