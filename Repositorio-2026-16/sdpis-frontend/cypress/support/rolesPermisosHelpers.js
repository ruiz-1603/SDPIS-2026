// HU-015: datos simulados y utilidades para las pruebas funcionales de
// "Roles y permisos". El backend se simula con cy.intercept: las pruebas
// verifican lo que la interfaz muestra/oculta y lo que envía al backend
// segun el rol de la sesion. Las reglas del backend (cupos, ultimo jefe,
// maximo de superusuarios...) ya estan cubiertas por las pruebas unitarias
// de SDPIS.Api.Tests; aqui se simula su respuesta (422/403) para comprobar
// que la interfaz la comunica correctamente.

// ---------------------------------------------------------------------
// Permisos efectivos por rol (GET /gestion-usuarios/mis-permisos)
// ---------------------------------------------------------------------
export const PERMISOS_FUNCIONARIO = [
    'VER_DASHBOARD',
    'VER_DENUNCIAS_PROPIAS',
    'REGISTRAR_CRITERIOS',
    'REGISTRAR_SEGUIMIENTO',
    'SOLICITAR_CIERRE',
];

export const PERMISOS_JEFE = [
    'VER_DASHBOARD',
    'VER_DENUNCIAS_AREA',
    'VER_FUNCIONARIOS_AREA',
    'CREAR_FUNCIONARIOS_AREA',
    'DELEGAR_PERMISOS_AREA',
    'ASIGNAR_DENUNCIAS',
    'GESTIONAR_DISPONIBILIDAD',
    'APROBAR_CIERRE',
    'VER_BITACORA_ADMINISTRATIVA',
];

export const PERMISOS_SUPERUSUARIO = [
    ...PERMISOS_JEFE,
    'ADMINISTRAR_SISTEMA',
    'VER_TODAS_AREAS',
    'VER_LISTA_AREAS',
    'GESTIONAR_CRITERIOS',
    'GESTIONAR_PLAZOS',
    'GESTIONAR_TERRITORIO',
];

// ---------------------------------------------------------------------
// Areas (GET /gestion-usuarios/areas)
// ---------------------------------------------------------------------
export const AREA_SJ = {
    idArea: 10,
    nombre: 'Área Rectora de Salud San José Centro',
    codigoArea: 'ARS-SJC',
    tipo: 'ARS',
    regionNombre: 'Central Sur',
    maxJefes: 2,
    jefesActivos: 2, // cupo completo
    jefesEnRenuncia: 0,
    funcionariosActivos: 4,
    denunciasPorAsignar: 3,
    denunciasActivas: 7,
};

export const AREA_HEREDIA = {
    idArea: 20,
    nombre: 'Área Rectora de Salud Heredia',
    codigoArea: 'ARS-HER',
    tipo: 'ARS',
    regionNombre: 'Central Norte',
    maxJefes: 2,
    jefesActivos: 1,
    jefesEnRenuncia: 0,
    funcionariosActivos: 2,
    denunciasPorAsignar: 0,
    denunciasActivas: 1,
};

export const AREA_UVC = {
    idArea: 30,
    nombre: 'Unidad de Vigilancia y Control',
    codigoArea: 'UVC',
    tipo: 'UVC',
    regionNombre: null,
    maxJefes: 2,
    jefesActivos: 0,
    jefesEnRenuncia: 1,
    funcionariosActivos: 2,
    denunciasPorAsignar: 1,
    denunciasActivas: 2,
};

export const AREA_DIRECCION = {
    idArea: 40,
    nombre: 'Dirección Regional Central Sur',
    codigoArea: 'DRCS',
    tipo: 'DR', // no admite jefe de area
    regionNombre: 'Central Sur',
    maxJefes: 0,
    jefesActivos: 0,
    jefesEnRenuncia: 0,
    funcionariosActivos: 0,
    denunciasPorAsignar: 0,
    denunciasActivas: 0,
};

export const AREAS = [AREA_SJ, AREA_HEREDIA, AREA_UVC, AREA_DIRECCION];

// ---------------------------------------------------------------------
// Funcionarios (GET /gestion-usuarios/funcionarios)
// ---------------------------------------------------------------------
function funcionario(datos) {
    return {
        correo: null,
        especialidad: null,
        activo: true,
        permiteAsignar: false,
        permisosDelegados: [],
        denunciasAsignadas: 0,
        denunciasPorAsignar: 0,
        renunciaSuperusuarioFecha: null,
        ...datos,
    };
}

export const JEFE_LAURA = funcionario({
    idFuncionario: 1,
    nombre: 'Laura Jiménez Solano',
    correo: 'laura.jimenez@misalud.go.cr',
    rol: 'jefe_area',
    areaId: AREA_SJ.idArea,
    areaNombre: AREA_SJ.nombre,
    permiteAsignar: true,
    denunciasPorAsignar: 3,
});

export const JEFE_CARLOS = funcionario({
    idFuncionario: 2,
    nombre: 'Carlos Vargas Mora',
    correo: 'carlos.vargas@misalud.go.cr',
    rol: 'jefe_area',
    areaId: AREA_SJ.idArea,
    areaNombre: AREA_SJ.nombre,
    permiteAsignar: true,
    denunciasPorAsignar: 3,
});

export const FUNC_ANA = funcionario({
    idFuncionario: 3,
    nombre: 'Ana Rojas Méndez',
    correo: 'ana.rojas@misalud.go.cr',
    rol: 'validador',
    areaId: AREA_SJ.idArea,
    areaNombre: AREA_SJ.nombre,
    especialidad: 'Medicamentos',
    denunciasAsignadas: 2,
});

export const FUNC_PEDRO = funcionario({
    idFuncionario: 4,
    nombre: 'Pedro Castro Rojas',
    correo: 'pedro.castro@misalud.go.cr',
    rol: 'validador',
    areaId: AREA_SJ.idArea,
    areaNombre: AREA_SJ.nombre,
    permiteAsignar: true,
    permisosDelegados: ['VER_DENUNCIAS_AREA'],
    denunciasAsignadas: 0,
});

export const FUNC_MARTA_INACTIVA = funcionario({
    idFuncionario: 5,
    nombre: 'Marta Quesada Arias',
    correo: 'marta.quesada@misalud.go.cr',
    rol: 'validador',
    areaId: AREA_SJ.idArea,
    areaNombre: AREA_SJ.nombre,
    activo: false,
});

export const JEFE_HEREDIA = funcionario({
    idFuncionario: 6,
    nombre: 'Rosa Alfaro Chaves',
    correo: 'rosa.alfaro@misalud.go.cr',
    rol: 'jefe_area',
    areaId: AREA_HEREDIA.idArea,
    areaNombre: AREA_HEREDIA.nombre,
    permiteAsignar: true,
});

export const SUPER_SOFIA = funcionario({
    idFuncionario: 100,
    nombre: 'Sofía Herrera Brenes',
    correo: 'sofia.herrera@misalud.go.cr',
    rol: 'superusuario',
    areaId: AREA_UVC.idArea,
    areaNombre: AREA_UVC.nombre,
});

export const JEFE_EN_RENUNCIA = funcionario({
    idFuncionario: 101,
    nombre: 'Diego Salas Mora',
    correo: 'diego.salas@misalud.go.cr',
    rol: 'jefe_area',
    areaId: AREA_UVC.idArea,
    areaNombre: AREA_UVC.nombre,
    renunciaSuperusuarioFecha: '2026-09-20T10:00:00',
});

export const FUNCIONARIOS_AREA_SJ = [JEFE_LAURA, JEFE_CARLOS, FUNC_ANA, FUNC_PEDRO, FUNC_MARTA_INACTIVA];

export const FUNCIONARIOS_TODOS = [
    ...FUNCIONARIOS_AREA_SJ,
    JEFE_HEREDIA,
    SUPER_SOFIA,
    JEFE_EN_RENUNCIA,
];

// Catalogo de permisos delegables (GET /gestion-usuarios/permisos-delegables)
export const CATALOGO_PERMISOS = [
    { codigo: 'ASIGNAR_DENUNCIAS', nivelDelegante: 'jefe_area', grupo: 'OPERATIVO', descripcion: 'Asignar denuncias del área a funcionarios validadores.' },
    { codigo: 'VER_DENUNCIAS_AREA', nivelDelegante: 'jefe_area', grupo: 'SUPERVISION', descripcion: 'Consultar las denuncias del área.' },
    { codigo: 'VER_FUNCIONARIOS_AREA', nivelDelegante: 'jefe_area', grupo: 'SUPERVISION', descripcion: 'Consultar los funcionarios del área.' },
    { codigo: 'VER_BITACORA_ADMINISTRATIVA', nivelDelegante: 'jefe_area', grupo: 'SUPERVISION', descripcion: 'Consultar el historial administrativo del área.' },
    { codigo: 'GESTIONAR_DISPONIBILIDAD', nivelDelegante: 'jefe_area', grupo: 'ADMINISTRATIVO', descripcion: 'Registrar la disponibilidad de los funcionarios.' },
    { codigo: 'VER_TODAS_AREAS', nivelDelegante: 'superusuario', grupo: 'SUPERVISION', descripcion: 'Consultar todas las áreas del sistema.' },
    { codigo: 'GESTIONAR_CRITERIOS', nivelDelegante: 'superusuario', grupo: 'ADMINISTRATIVO', descripcion: 'Gestionar las reglas de criticidad.' },
];

// ---------------------------------------------------------------------
// Perfiles de sesion (usuario del token + permisos efectivos)
// ---------------------------------------------------------------------
export const PERFILES = {
    funcionario: {
        usuario: { funcionarioId: 3, areaId: 10, identificacion: '303330333', nombre: 'Ana', apellido1: 'Rojas', apellido2: 'Méndez', rol: 'validador' },
        permisos: PERMISOS_FUNCIONARIO,
        alcance: 'PROPIAS',
    },
    jefe: {
        usuario: { funcionarioId: 1, areaId: 10, identificacion: '101110111', nombre: 'Laura', apellido1: 'Jiménez', apellido2: 'Solano', rol: 'jefe_area' },
        permisos: PERMISOS_JEFE,
        alcance: 'AREA',
    },
    superusuario: {
        usuario: { funcionarioId: 100, areaId: 30, identificacion: '100000100', nombre: 'Sofía', apellido1: 'Herrera', apellido2: 'Brenes', rol: 'superusuario' },
        permisos: PERMISOS_SUPERUSUARIO,
        alcance: 'TODAS',
    },
};

const RUTA = '/api/gestion-usuarios';

// Responde la lista de funcionarios aplicando los filtros de la consulta
// (areaId, rol) igual que el backend. `lista` puede ser una funcion para
// que la prueba cambie los datos entre llamadas.
export function responderFuncionarios(lista) {
    return (req) => {
        let resultado = typeof lista === 'function' ? lista() : lista;
        if (req.query.areaId) resultado = resultado.filter((f) => String(f.areaId) === String(req.query.areaId));
        if (req.query.rol) resultado = resultado.filter((f) => f.rol === req.query.rol);
        req.reply({ statusCode: 200, body: resultado });
    };
}

// Inicia sesion "como" un perfil sin pasar por el MSI ni por el backend:
// guarda la sesion en sessionStorage (como lo hace el login real) y abre
// directamente la vista indicada (history.state.vista, igual que al
// refrescar la pagina). Devuelve un objeto mutable: si la prueba cambia
// `sesion.permisos`, la siguiente consulta a mis-permisos lo refleja
// (sirve para probar revocaciones / renuncia sin volver a iniciar sesion).
export function entrarComo(perfil, { vista = 'institucional-inicio', permisos, funcionarios, areas = AREAS } = {}) {
    const base = PERFILES[perfil];
    const sesion = {
        usuario: base.usuario,
        permisos: permisos ?? base.permisos,
        alcance: base.alcance,
    };

    cy.intercept('GET', '**/api/autenticacion/sesion', { statusCode: 200, body: base.usuario }).as('sesionActual');
    cy.intercept('POST', '**/api/autenticacion/logout', { statusCode: 204 }).as('logout');
    cy.intercept('POST', '**/api/autenticacion/iniciar-sesion').as('iniciarSesion');
    cy.intercept('GET', `**${RUTA}/mis-permisos`, (req) => {
        req.reply({ statusCode: 200, body: { permisos: sesion.permisos, alcance: sesion.alcance } });
    }).as('misPermisos');

    cy.intercept('GET', `**${RUTA}/areas`, { statusCode: 200, body: areas }).as('listarAreas');
    cy.intercept('GET', `**${RUTA}/permisos-delegables`, { statusCode: 200, body: CATALOGO_PERMISOS }).as('catalogo');
    cy.intercept(
        { method: 'GET', pathname: `${RUTA}/funcionarios` },
        responderFuncionarios(funcionarios ?? (perfil === 'superusuario' ? FUNCIONARIOS_TODOS : FUNCIONARIOS_AREA_SJ))
    ).as('listarFuncionarios');
    cy.intercept(
        { method: 'GET', pathname: `${RUTA}/bitacora` },
        { statusCode: 200, body: { items: [], total: 0, pagina: 1, tamano: 25 } }
    ).as('bitacora');

    cy.visit('/', {
        onBeforeLoad(win) {
            win.sessionStorage.clear();
            win.sessionStorage.setItem('sdpis.sesion', JSON.stringify({
                token: `token-prueba-${perfil}`,
                expiraEn: new Date(Date.now() + 60 * 60 * 1000).toISOString(),
                usuario: base.usuario,
            }));
            win.history.replaceState({ vista }, '');
        },
    });

    cy.wait('@misPermisos');
    cy.get('.app-shell').should('be.visible');
    return sesion;
}

export function irAlMenu(etiqueta) {
    cy.contains('.menu-label', etiqueta).click();
}

export function irAPestana(etiqueta) {
    cy.contains('[role="tab"]', etiqueta).click();
}

export function filaDe(nombre) {
    return cy.contains('table.gu-tabla tbody tr', nombre);
}

export function modal() {
    return cy.get('.gu-modal');
}

export function campoModal(etiqueta) {
    return cy.contains('.gu-modal label.gu-campo', etiqueta);
}
