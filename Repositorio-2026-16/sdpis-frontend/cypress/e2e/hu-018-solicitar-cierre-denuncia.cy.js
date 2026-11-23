import { entrarComo, irAlMenu, PERMISOS_JEFE } from '../support/rolesPermisosHelpers';

// HU-018 Solicitar cierre de denuncia.
// Pruebas funcionales de la interfaz con el backend simulado (cy.intercept);
// las reglas del backend (solo el responsable asignado, estado en curso,
// bitacora y notificacion) estan cubiertas por SolicitudCierreServiceTests.
// Solo se necesita el frontend corriendo (npm run dev).

const DENUNCIA_ID = 14;
const CONSECUTIVO = 'ACOS-2026-000005';

// El jefe real tambien tiene REGISTRAR_SEGUIMIENTO (CatalogoPermisos.cs); el
// perfil del helper de HU-015 no lo incluye porque esas pruebas no usan la bitacora.
const PERMISOS_JEFE_REAL = [...PERMISOS_JEFE, 'REGISTRAR_SEGUIMIENTO'];

const MENSAJE_CONFIRMACION = 'Una vez enviada, no podrá ser modificada ni se podrá agregar más documentación';

const SOLICITUD = {
    idSolicitudCierre: 1,
    denunciaId: DENUNCIA_ID,
    consecutivo: CONSECUTIVO,
    solicitadoPorId: 3,
    solicitadoPorNombre: 'Ana Rojas Méndez',
    areaId: 10,
    areaNombre: 'Área Rectora de Salud San José Centro',
    resolucionFinal: 'Se verificó el retiro del producto.',
    observacionesFinales: 'El establecimiento corrigió la situación.',
    accionesRealizadas: 'Inspección en sitio y verificación documental.',
    fechaSolicitud: '2026-10-08T10:00:00.000',
    estado: 'PENDIENTE',
};

const ENTRADAS_BITACORA = [
    {
        idBitacora: 1, denunciaId: DENUNCIA_ID, funcionarioId: null, funcionarioNombre: null,
        areaOrigenId: 10, areaOrigenNombre: 'ARS San José Centro', fecha: '2026-09-10T08:00:00.000',
        tipoRegistro: 'asignacion', contenido: 'Denuncia asignada al funcionario encargado Ana Rojas Méndez.',
    },
    {
        idBitacora: 2, denunciaId: DENUNCIA_ID, funcionarioId: 3, funcionarioNombre: 'Ana Rojas Méndez',
        areaOrigenId: 10, areaOrigenNombre: 'ARS San José Centro', fecha: '2026-09-15T09:30:00.000',
        tipoRegistro: 'comentario', contenido: 'Inspección realizada al establecimiento.',
    },
];

const DETALLE = {
    idDenuncia: DENUNCIA_ID,
    consecutivo: CONSECUTIVO,
    fechaCreacion: '2026-09-01T00:00:00',
    estado: 'EN_TIEMPO',
    hecho: {
        nombreEstablecimiento: 'Farmacia La Esquina',
        descripcionHecho: 'Venta de medicamento vencido.',
        denunciadoOtraInstitucion: 'N', detalleOtraInstitucion: null,
        denunciadoPreviamenteMinsalud: 'N', detallePrevioMinsalud: null,
    },
    ubicacion: { provincia: 'San José', canton: 'Acosta', distrito: 'San Ignacio', direccionExacta: 'Frente al parque' },
    productos: [{
        idDetalleProducto: 1, nombreProducto: 'Acetaminofén 500 mg', tipoProducto: 'Medicamentos',
        descripcionProducto: 'Caja de 10 tabletas', registroSanitario: null, marca: null, numeroLote: null,
        fabricante: null, paisOrigen: null, presentacion: null, fechaCompra: null, motivos: ['Producto vencido'],
    }],
};

const CRITERIOS = {
    idEvaluacion: 1, denunciaId: DENUNCIA_ID, funcionarioValidadorId: 3, fechaEvaluacion: '2026-09-05T00:00:00',
    puntajeTotal: 3, vinculadoComercioElectronico: 'N', danoPotencialSalud: 'Medio',
    requiereCriterioEspecializado: 'N', observacionesTecnicas: 'Riesgo moderado.',
    nivelCriticidad: 'MEDIO', fechaLimite: '2026-12-04T00:00:00',
};

// Simula el backend de la bitacora y de la solicitud de cierre. `estado` es
// mutable para que la prueba cambie la respuesta tras enviar la solicitud.
function simularBackend({ puedeSolicitar = true, pendiente = null } = {}) {
    const estado = { puedeSolicitar, pendiente };

    cy.intercept('GET', '**/api/bitacora/denuncias', {
        statusCode: 200,
        body: [{
            idDenuncia: DENUNCIA_ID, consecutivo: CONSECUTIVO, estado: pendiente ? 'EN_REVISION' : 'EN_TIEMPO',
            fechaCreacion: '01 sep. 2026', funcionarioAsignadoId: 3, funcionarioAsignadoNombre: 'Ana Rojas Méndez',
            areaId: 10, areaNombre: 'ARS San José Centro',
        }],
    }).as('getDenuncias');

    cy.intercept('GET', `**/api/denuncias/${DENUNCIA_ID}/bitacora*`, {
        statusCode: 200,
        body: { items: ENTRADAS_BITACORA, pagina: 1, tamano: 20, total: ENTRADAS_BITACORA.length },
    }).as('getBitacora');

    cy.intercept('GET', `**/api/denuncias/${DENUNCIA_ID}/solicitud-cierre`, (req) => {
        req.reply({
            statusCode: 200,
            body: {
                puedeSolicitar: estado.puedeSolicitar,
                motivo: estado.puedeSolicitar ? null : 'Solo el funcionario responsable asignado a la denuncia puede solicitar su cierre.',
                solicitudPendiente: estado.pendiente,
                carpetaConfidencialUrl: null,
                carpetaNoConfidencialUrl: null,
            },
        });
    }).as('getEstadoCierre');

    cy.intercept('GET', `**/api/denuncias/${DENUNCIA_ID}/detalle`, { statusCode: 200, body: DETALLE }).as('getDetalle');
    cy.intercept('GET', `**/api/denuncias/${DENUNCIA_ID}/criterios-tecnicos`, { statusCode: 200, body: CRITERIOS }).as('getCriterios');
    cy.intercept('GET', `**/api/denuncias/${DENUNCIA_ID}/historial-asignacion`, {
        statusCode: 200,
        body: [{
            idHistorial: 1, idDenuncia: DENUNCIA_ID, areaAnteriorId: 10, areaNuevaId: 10,
            funcionarioAnteriorId: null, funcionarioNuevoId: 3, fecha: '2026-09-10T08:00:00', motivo: 'Asignación inicial',
        }],
    }).as('getHistorial');

    return estado;
}

function abrirDenuncia() {
    cy.wait('@getDenuncias');
    cy.get('button[aria-label^="Denuncia"]').click();
    cy.contains('.sb-opcion', CONSECUTIVO).click();
    cy.wait(['@getBitacora', '@getEstadoCierre']);
}

function abrirSolicitud() {
    cy.contains('button', 'Solicitar cierre').click();
    cy.wait(['@getDetalle', '@getCriterios', '@getHistorial']);
    cy.get('.sc-panel').should('be.visible');
}

function llenarFormulario() {
    cy.get('#sc-resolucion').type(SOLICITUD.resolucionFinal);
    cy.get('#sc-observaciones').type(SOLICITUD.observacionesFinales);
    cy.get('#sc-acciones').type(SOLICITUD.accionesRealizadas);
}

describe('HU-018: Solicitar cierre de denuncia', () => {

    // -----------------------------------------------------------------
    context('CA-01 Solo el funcionario responsable asignado puede solicitar el cierre', () => {
        it('el responsable asignado ve el botón "Solicitar cierre" en la bitácora', () => {
            simularBackend({ puedeSolicitar: true });
            entrarComo('funcionario', { vista: 'institucional-bitacora' });
            abrirDenuncia();

            cy.contains('.bt-detalle-head button', 'Solicitar cierre').should('be.visible');
        });

        it('un funcionario que no es el responsable no ve el botón', () => {
            simularBackend({ puedeSolicitar: false });
            entrarComo('funcionario', { vista: 'institucional-bitacora' });
            abrirDenuncia();

            cy.get('.bt-entrada').should('have.length', 2);
            cy.contains('button', 'Solicitar cierre').should('not.exist');
        });

        it('la jefatura no ve el botón aunque consulte la bitácora de la denuncia', () => {
            simularBackend({ puedeSolicitar: false });
            entrarComo('jefe', { vista: 'institucional-bitacora', permisos: PERMISOS_JEFE_REAL });
            abrirDenuncia();

            cy.contains('button', 'Solicitar cierre').should('not.exist');
        });

        it('si el backend rechaza al funcionario (403), la interfaz muestra el motivo', () => {
            simularBackend({ puedeSolicitar: true });
            cy.intercept('POST', `**/api/denuncias/${DENUNCIA_ID}/solicitud-cierre`, {
                statusCode: 403,
                body: { error: 'Solo el funcionario responsable asignado a la denuncia puede solicitar su cierre.' },
            }).as('postSolicitud');

            entrarComo('funcionario', { vista: 'institucional-bitacora' });
            abrirDenuncia();
            abrirSolicitud();
            llenarFormulario();
            cy.contains('.sc-formulario button', 'Solicitar cierre').click();
            cy.contains('.modal-card button', 'Sí, solicitar cierre').click();

            cy.wait('@postSolicitud');
            cy.get('.sc-error').should('contain', 'Solo el funcionario responsable asignado');
            cy.get('.sc-panel').should('be.visible');
        });
    });

    // -----------------------------------------------------------------
    context('Requisito 1: muestra toda la información del caso antes de solicitar', () => {
        it('muestra denuncia, criticidad, historial, bitácora y adjuntos', () => {
            simularBackend();
            entrarComo('funcionario', { vista: 'institucional-bitacora' });
            abrirDenuncia();
            abrirSolicitud();

            cy.get('.sc-panel').within(() => {
                cy.contains('Farmacia La Esquina').should('be.visible');
                cy.contains('Venta de medicamento vencido.').should('be.visible');
                cy.contains('Acetaminofén 500 mg').should('be.visible');
                cy.contains('Motivos: Producto vencido').should('be.visible');
                cy.contains('.badge-criticidad', 'MEDIO').should('be.visible');
                cy.contains('Asignación inicial').should('be.visible');
                // La bitácora inicia oculta y se despliega con el botón.
                cy.get('.sc-lista--bitacora').should('not.exist');
                cy.contains('button', 'Desplegar bitácora').click();
                cy.contains('Inspección realizada al establecimiento.').should('be.visible');
                cy.contains('button', 'Ocultar bitácora').click();
                cy.get('.sc-lista--bitacora').should('not.exist');
                cy.contains('button', 'Desplegar bitácora').should('be.visible');
                cy.contains('no tiene documentos registrados').should('be.visible');
            });
        });

        it('"Volver a la bitácora" regresa sin enviar nada', () => {
            simularBackend();
            cy.intercept('POST', `**/api/denuncias/${DENUNCIA_ID}/solicitud-cierre`, cy.spy().as('postSpy'));
            entrarComo('funcionario', { vista: 'institucional-bitacora' });
            abrirDenuncia();
            abrirSolicitud();

            cy.contains('button', 'Volver a la bitácora').click();
            cy.get('.sc-panel').should('not.exist');
            cy.get('.bt-entrada').should('have.length', 2);
            cy.get('@postSpy').should('not.have.been.called');
        });
    });

    // -----------------------------------------------------------------
    context('Entradas obligatorias de la solicitud', () => {
        it('no abre la confirmación si faltan la resolución, las observaciones o las acciones', () => {
            simularBackend();
            entrarComo('funcionario', { vista: 'institucional-bitacora' });
            abrirDenuncia();
            abrirSolicitud();

            cy.contains('.sc-formulario button', 'Solicitar cierre').click();

            cy.get('.sc-campo-error').should('have.length', 3);
            cy.get('.modal-card').should('not.exist');
        });
    });

    // -----------------------------------------------------------------
    context('Requisito 2 / CA-02 / CA-03: doble verificación y envío', () => {
        it('cancelar la confirmación no envía la solicitud', () => {
            simularBackend();
            cy.intercept('POST', `**/api/denuncias/${DENUNCIA_ID}/solicitud-cierre`, cy.spy().as('postSpy'));
            entrarComo('funcionario', { vista: 'institucional-bitacora' });
            abrirDenuncia();
            abrirSolicitud();
            llenarFormulario();

            cy.contains('.sc-formulario button', 'Solicitar cierre').click();
            cy.get('.modal-card').should('contain', MENSAJE_CONFIRMACION);
            cy.contains('.modal-card button', 'Cancelar').click();

            cy.get('.modal-card').should('not.exist');
            cy.get('.sc-panel').should('be.visible');
            cy.get('@postSpy').should('not.have.been.called');
        });

        it('al confirmar envía la solicitud, la denuncia queda en revisión y se bloquean los comentarios', () => {
            const estado = simularBackend();
            cy.intercept('POST', `**/api/denuncias/${DENUNCIA_ID}/solicitud-cierre`, (req) => {
                estado.puedeSolicitar = false;
                estado.pendiente = SOLICITUD;
                req.reply({ statusCode: 201, body: SOLICITUD });
            }).as('postSolicitud');

            entrarComo('funcionario', { vista: 'institucional-bitacora' });
            abrirDenuncia();
            abrirSolicitud();
            llenarFormulario();

            cy.contains('.sc-formulario button', 'Solicitar cierre').click();
            cy.get('.modal-card').should('contain', '¿Seguro que desea solicitar el cierre de la denuncia?');
            cy.contains('.modal-card button', 'Sí, solicitar cierre').click();

            cy.wait('@postSolicitud').its('request.body').should('deep.equal', {
                resolucionFinal: SOLICITUD.resolucionFinal,
                observacionesFinales: SOLICITUD.observacionesFinales,
                accionesRealizadas: SOLICITUD.accionesRealizadas,
            });

            cy.contains('Solicitud de cierre enviada a la jefatura').should('be.visible');
            cy.get('.sc-panel').should('not.exist');
            cy.get('.sc-aviso-revision')
                .should('contain', 'Cierre en revisión')
                .and('contain', SOLICITUD.areaNombre);
            cy.contains('button', 'Solicitar cierre').should('not.exist');
            cy.get('#bt-comentario').should('not.exist');
            cy.contains('no se pueden agregar más comentarios').should('be.visible');
        });

        it('la jefatura sigue pudiendo comentar una denuncia con cierre en revisión', () => {
            simularBackend({ puedeSolicitar: false, pendiente: SOLICITUD });
            entrarComo('jefe', { vista: 'institucional-bitacora', permisos: PERMISOS_JEFE_REAL });
            abrirDenuncia();

            cy.get('.sc-aviso-revision').should('contain', 'Cierre en revisión');
            cy.get('#bt-comentario').should('be.visible');
        });
    });

    // -----------------------------------------------------------------
    context('Requisito 3 / CA-04: bandeja de solicitudes pendientes de la jefatura', () => {
        it('la jefatura ve las solicitudes pendientes de su área con la acción de revisarlas (HU-017)', () => {
            cy.intercept('GET', '**/api/solicitudes-cierre/pendientes', { statusCode: 200, body: [SOLICITUD] })
                .as('getPendientes');
            entrarComo('jefe');
            irAlMenu('Solicitudes de cierre');
            cy.wait('@getPendientes');

            cy.get('.scp-item').should('have.length', 1).first().within(() => {
                cy.contains(CONSECUTIVO).should('be.visible');
                cy.contains('Pendiente de evaluación').should('be.visible');
                cy.contains(`Jefatura de ${SOLICITUD.areaNombre}`).should('be.visible');
                cy.contains(SOLICITUD.solicitadoPorNombre).should('be.visible');
                cy.contains(SOLICITUD.resolucionFinal).should('be.visible');
                cy.get('button').should('have.length', 1).and('contain', 'Revisar solicitud');
            });
        });

        it('muestra un mensaje cuando no hay solicitudes pendientes', () => {
            cy.intercept('GET', '**/api/solicitudes-cierre/pendientes', { statusCode: 200, body: [] }).as('getPendientes');
            entrarComo('jefe');
            irAlMenu('Solicitudes de cierre');
            cy.wait('@getPendientes');

            cy.contains('No hay solicitudes de cierre pendientes.').should('be.visible');
        });

        it('un funcionario no ve la opción "Solicitudes de cierre" en el menú', () => {
            entrarComo('funcionario');
            cy.contains('.menu-label', 'Bitácora').should('be.visible');
            cy.contains('.menu-label', 'Solicitudes de cierre').should('not.exist');
        });
    });
});
