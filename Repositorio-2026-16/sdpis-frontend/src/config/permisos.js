// HU-015: codigos de permisos (deben coincidir con
// SDPIS.Api/Services/Autorizacion/Permisos.cs). El frontend NO decide
// quien tiene que permiso: solo oculta lo que el backend ya negaria.
export const PERMISOS = {
    VER_DASHBOARD: 'VER_DASHBOARD',
    VER_DENUNCIAS_PROPIAS: 'VER_DENUNCIAS_PROPIAS',
    VER_DENUNCIAS_AREA: 'VER_DENUNCIAS_AREA',
    VER_TODAS_AREAS: 'VER_TODAS_AREAS',
    VER_LISTA_AREAS: 'VER_LISTA_AREAS',
    VER_FUNCIONARIOS_AREA: 'VER_FUNCIONARIOS_AREA',
    CREAR_FUNCIONARIOS_AREA: 'CREAR_FUNCIONARIOS_AREA',
    DELEGAR_PERMISOS_AREA: 'DELEGAR_PERMISOS_AREA',
    ADMINISTRAR_SISTEMA: 'ADMINISTRAR_SISTEMA',
    ASIGNAR_DENUNCIAS: 'ASIGNAR_DENUNCIAS',
    GESTIONAR_DISPONIBILIDAD: 'GESTIONAR_DISPONIBILIDAD',
    REGISTRAR_CRITERIOS: 'REGISTRAR_CRITERIOS',
    REGISTRAR_SEGUIMIENTO: 'REGISTRAR_SEGUIMIENTO',
    SOLICITAR_CIERRE: 'SOLICITAR_CIERRE',
    APROBAR_CIERRE: 'APROBAR_CIERRE',
    GESTIONAR_CRITERIOS: 'GESTIONAR_CRITERIOS',
    GESTIONAR_PLAZOS: 'GESTIONAR_PLAZOS',
    GESTIONAR_TERRITORIO: 'GESTIONAR_TERRITORIO',
    CONFIRMAR_UVC: 'CONFIRMAR_UVC',
    VER_BITACORA_ADMINISTRATIVA: 'VER_BITACORA_ADMINISTRATIVA',
};

export const ROLES = {
    VALIDADOR: 'validador',
    JEFE_AREA: 'jefe_area',
    SUPERUSUARIO: 'superusuario',
};

export const ROLES_ETIQUETA = {
    superusuario: 'Superusuario',
    jefe_area: 'Jefe de área',
    validador: 'Funcionario',
};

// Rol vigente segun los permisos actuales (el rol del token no cambia hasta
// el proximo login; p. ej. tras renunciar a superusuario). APROBAR_CIERRE
// es del jefe y del superusuario y no es delegable: por eso
// ADMINISTRAR_SISTEMA se revisa primero.
export function derivarRol(listaPermisos) {
    if (!listaPermisos) return null;
    if (listaPermisos.includes(PERMISOS.ADMINISTRAR_SISTEMA)) return ROLES.SUPERUSUARIO;
    if (listaPermisos.includes(PERMISOS.APROBAR_CIERRE)) return ROLES.JEFE_AREA;
    return ROLES.VALIDADOR;
}

export const PERMISOS_ETIQUETA = {
    ASIGNAR_DENUNCIAS: 'Asignar denuncias',
    VER_DENUNCIAS_AREA: 'Ver denuncias del área',
    VER_FUNCIONARIOS_AREA: 'Ver funcionarios del área',
    VER_BITACORA_ADMINISTRATIVA: 'Ver historial administrativo',
    CREAR_FUNCIONARIOS_AREA: 'Crear y editar funcionarios',
    GESTIONAR_DISPONIBILIDAD: 'Gestionar disponibilidad',
    VER_TODAS_AREAS: 'Ver todas las áreas',
    VER_LISTA_AREAS: 'Ver lista de áreas',
    GESTIONAR_CRITERIOS: 'Gestionar reglas de criticidad',
    GESTIONAR_PLAZOS: 'Gestionar plazos',
    GESTIONAR_TERRITORIO: 'Gestionar áreas y territorio',
    CONFIRMAR_UVC: 'Confirmar atención UVC',
};

export const GRUPOS_PERMISO = [
    { id: 'OPERATIVO', etiqueta: 'Operativo' },
    { id: 'SUPERVISION', etiqueta: 'Acceso de supervisión' },
    { id: 'ADMINISTRATIVO', etiqueta: 'Acceso administrativo' },
];

export const CATEGORIAS_BITACORA = [
    { valor: 'FUNCIONARIOS', etiqueta: 'Funcionarios' },
    { valor: 'PERMISOS', etiqueta: 'Permisos' },
    { valor: 'CUPOS', etiqueta: 'Cupos' },
    { valor: 'CONSULTA', etiqueta: 'Consultas' },
    { valor: 'SESION', etiqueta: 'Sesiones' },
    { valor: 'CRITERIOS', etiqueta: 'Criterios' },
    { valor: 'PLAZOS', etiqueta: 'Plazos' },
    { valor: 'GESTION_TERRITORIAL', etiqueta: 'Gestión territorial' },
];

// Tipos de area que admiten jefe de area (igual que el backend):
// rectoras (AR/ARS) y la UVC.
export const TIPOS_AREA_CON_JEFE = ['AR', 'ARS', 'UVC'];

// Filtro de la tabla (solo frontend): jefes que antes eran superusuarios.
export const FILTRO_EN_RENUNCIA = 'en_renuncia';

// TEMPORAL - poner en false (y borrar el modal) al integrar el MSI.
export const MOSTRAR_CREDENCIAL_TEMPORAL = true;

// Correccion del correo institucional de un funcionario ya creado. Poner en
// false al integrar el MSI: el correo es la llave de enlace y lo gestiona el
// MSI (el backend tambien lo bloquea con Autenticacion:Proveedor = MSI).
export const CORREO_EDITABLE = true;

// Vistas del portal: titulo de la miga de pan y permisos que la habilitan
// (basta con uno). Lista vacia = cualquier usuario autenticado.
export const VISTAS_INSTITUCIONALES = {
    'institucional-inicio': { titulo: 'Inicio', permisos: [] },
    'institucional-asignaciones': {
        titulo: 'Asignación de denuncias',
        permisos: [PERMISOS.ASIGNAR_DENUNCIAS],
    },
    'institucional-encargado': {
        titulo: 'Asignación de funcionario encargado',
        permisos: [PERMISOS.ASIGNAR_DENUNCIAS, PERMISOS.CONFIRMAR_UVC],
    },
    institucional: {
        titulo: 'Disponibilidad de funcionarios',
        permisos: [PERMISOS.GESTIONAR_DISPONIBILIDAD],
    },
    'institucional-bitacora': {
        titulo: 'Bitácora de seguimiento',
        permisos: [PERMISOS.REGISTRAR_SEGUIMIENTO],
    },
    // HU-018/017: bandeja de solicitudes de cierre (jefatura: su area;
    // superusuario: todas). Ambos pueden revisarlas y resolverlas.
    'institucional-solicitudes-cierre': {
        titulo: 'Solicitudes de cierre',
        permisos: [PERMISOS.APROBAR_CIERRE, PERMISOS.VER_TODAS_AREAS],
    },
    'institucional-criterios-tecnicos': {
        titulo: 'Criterios técnicos de evaluación',
        permisos: [PERMISOS.REGISTRAR_CRITERIOS, PERMISOS.VER_DENUNCIAS_AREA, PERMISOS.VER_TODAS_AREAS],
    },
    'institucional-gestion-usuarios': {
        titulo: 'Roles y permisos',
        permisos: [PERMISOS.VER_FUNCIONARIOS_AREA, PERMISOS.VER_LISTA_AREAS, PERMISOS.VER_BITACORA_ADMINISTRATIVA],
    },
    'institucional-plazos': {
        titulo: 'Plazos de atención',
        permisos: [PERMISOS.GESTIONAR_PLAZOS],
    },
};

// Menu lateral (orden y agrupacion). Cada item se muestra solo si el
// usuario tiene alguno de los permisos de su vista.
export const MENU_INSTITUCIONAL = [
    {
        grupo: 'Gestión',
        items: [
            { vista: 'institucional-asignaciones', etiqueta: 'Asignaciones', icono: '⇄', ancla: 'asignaciones' },
            { vista: 'institucional-encargado', etiqueta: 'Encargado', icono: '↦', ancla: 'encargado' },
            { vista: 'institucional', etiqueta: 'Disponibilidad de funcionarios', icono: '◷', ancla: 'disponibilidad' },
        ],
    },
    {
        grupo: 'Seguimiento',
        items: [
            { vista: 'institucional-bitacora', etiqueta: 'Bitácora', icono: 'cuaderno', ancla: 'bitacora' },
            { vista: 'institucional-criterios-tecnicos', etiqueta: 'Criticidad', icono: '△', ancla: 'criterios-tecnicos' },
            { vista: 'institucional-solicitudes-cierre', etiqueta: 'Solicitudes de cierre', icono: '☑', ancla: 'solicitudes-cierre' },
        ],
    },
    {
        grupo: 'Administración',
        items: [
            { vista: 'institucional-gestion-usuarios', etiqueta: 'Roles y permisos', icono: '⚿', ancla: 'roles-permisos' },
            { vista: 'institucional-plazos', etiqueta: 'Plazos de atención', icono: '⏱', ancla: 'plazos-atencion' },
        ],
    },
];
