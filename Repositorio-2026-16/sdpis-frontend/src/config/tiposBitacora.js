// HU-016: tipos de registro de la bitacora. Deben coincidir con
// TiposBitacora (backend) y con el CHECK de bitacora.tipo_registro.
// Los usan el filtro de Bitacora y los paneles de solicitud y revision de
// cierre, para que todos muestren el mismo nombre de cada tipo.
//
// resumen: texto corto que acompana la opcion del filtro; ayuda: descripcion
// que se muestra bajo el filtro al elegirla, para que se entienda que acciones
// incluye cada tipo.
export const TIPOS_BITACORA = [
    {
        valor: 'registro_denuncia',
        etiqueta: 'Registro de la denuncia',
        resumen: 'recepción del caso',
        ayuda: 'Recepción y registro de la denuncia en el sistema.',
    },
    {
        valor: 'asignacion',
        etiqueta: 'Asignaciones',
        resumen: 'validador y encargado',
        ayuda: 'Asignación y reasignación del validador y del funcionario encargado de la denuncia.',
    },
    {
        valor: 'traslado',
        etiqueta: 'Traslados entre áreas',
        resumen: 'cambios de área y UVC',
        ayuda: 'Reasignación de la denuncia a otra área, traslados a la UVC, confirmación de atención en la UVC y devolución al área rectora.',
    },
    {
        valor: 'evaluacion_tecnica',
        etiqueta: 'Evaluación técnica',
        resumen: 'criterios técnicos',
        ayuda: 'Registro y modificación de los criterios técnicos y de los datos generales de la evaluación.',
    },
    {
        valor: 'criticidad',
        etiqueta: 'Criticidad',
        resumen: 'nivel y fecha límite',
        ayuda: 'Cálculo o recálculo del nivel de criticidad y de la fecha límite de atención, y su eliminación cuando faltan criterios técnicos.',
    },
    {
        valor: 'notificacion',
        etiqueta: 'Alertas y notificaciones',
        resumen: 'avisos del sistema',
        ayuda: 'Avisos que envía el sistema: notificación a la UVC por criticidad alta y alertas de vencimiento del plazo de asignación.',
    },
    {
        valor: 'comentario',
        etiqueta: 'Comentarios',
        resumen: 'comentarios manuales',
        ayuda: 'Comentarios manuales que el funcionario o la jefatura agregan para dar seguimiento al caso.',
    },
    {
        valor: 'solicitud_cierre',
        etiqueta: 'Solicitudes de cierre',
        resumen: 'solicitud y resolución',
        ayuda: 'Envío de la solicitud de cierre a la jefatura y su resolución (aprobada o rechazada).',
    },
    // Aun no se registran: queda listo para cuando se puedan adjuntar archivos.
    {
        valor: 'adjunto',
        etiqueta: 'Archivos adjuntos',
        resumen: 'documentos del caso',
        ayuda: 'Archivos o documentos adjuntados a la denuncia.',
    },
];

export function etiquetaTipoBitacora(tipo) {
    return TIPOS_BITACORA.find((t) => t.valor === tipo)?.etiqueta ?? tipo;
}
