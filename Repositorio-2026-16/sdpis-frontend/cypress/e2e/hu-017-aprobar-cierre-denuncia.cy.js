import { entrarComo, PERMISOS_SUPERUSUARIO } from '../support/rolesPermisosHelpers';

// HU-017 Gestionar aprobación de resolución de denuncias.
// Pruebas funcionales de la interfaz con el backend simulado (cy.intercept);
// las reglas del backend (solo la jefatura del área de la solicitud, estado
// FINALIZADA o el previo, bitácora y notificación) están cubiertas por
// ResolucionCierreServiceTests y por sp_resolver_solicitud_cierre.
// La revisión se hace desde la bandeja "Solicitudes de cierre"; la bitácora
// solo muestra el aviso de que el cierre está en revisión.
// Solo se necesita el frontend corriendo (npm run dev).

const DENUNCIA_ID = 14;
const CONSECUTIVO = 'ACOS-2026-000005';
const AREA_NOMBRE = 'Área Rectora de Salud San José Centro';

// El jefe real tiene APROBAR_CIERRE y REGISTRAR_SEGUIMIENTO (CatalogoPermisos.cs).
const PERMISOS_JEFE_REAL = [
    'VER_DASHBOARD', 'VER_DENUNCIAS_AREA', 'VER_FUNCIONARIOS_AREA', 'CREAR_FUNCIONARIOS_AREA',
    'DELEGAR_PERMISOS_AREA', 'ASIGNAR_DENUNCIAS', 'GESTIONAR_DISPONIBILIDAD', 'APROBAR_CIERRE',
    'VER_BITACORA_ADMINISTRATIVA', 'REGISTRAR_SEGUIMIENTO',
];
// El superusuario ve la bandeja de todas las áreas y también resuelve
// (tiene APROBAR_CIERRE, que no es delegable).
const PERMISOS_SUPERUSUARIO_REAL = [...PERMISOS_SUPERUSUARIO, 'REGISTRAR_SEGUIMIENTO'];

const SOLICITUD = {
    idSolicitudCierre: 2,
    denunciaId: DENUNCIA_ID,
    consecutivo: CONSECUTIVO,
    solicitadoPorId: 3,
    solicitadoPorNombre: 'Ana Rojas Méndez',
    areaId: 10,
    areaNombre: AREA_NOMBRE,
    resolucionFinal: 'Se verificó el retiro del producto vencido.',
    observacionesFinales: 'El establecimiento corrigió la situación.',
    accionesRealizadas: 'Inspección en sitio y verificación documental.',
    fechaSolicitud: '2026-10-08T10:00:00.000',
    estado: 'PENDIENTE',
};

// Una solicitud anterior ya rechazada + la pendiente (sin decisión).
const HISTORIAL_CIERRE = [
    { solicitud: SOLICITUD, estadoPrevio: 'ATRASADA', decision: null },
    {
        solicitud: {
            ...SOLICITUD, idSolicitudCierre: 1, resolucionFinal: 'Primera resolución propuesta.',
            fechaSolicitud: '2026-10-01T09:00:00.000', estado: 'RECHAZADA',
        },
        estadoPrevio: 'ATRASADA',
        decision: {
            idDecisionCierre: 1, decision: 'RECHAZADA', decididoPorId: 1, decididoPorNombre: 'Laura Jiménez Solano',
            observacionesJefatura: 'Falta adjuntar el acta de inspección.', fechaDecision: '2026-10-02T11:00:00.000',
        },
    },
];

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
    estado: 'EN_REVISION',
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

function resolucion(decision, estadoDenuncia, observaciones) {
    return {
        idSolicitudCierre: SOLICITUD.idSolicitudCierre,
        denunciaId: DENUNCIA_ID,
        estadoDenuncia,
        decision: {
            idDecisionCierre: 2, decision, decididoPorId: 1, decididoPorNombre: 'Laura Jiménez Solano',
            observacionesJefatura: observaciones, fechaDecision: '2026-10-09T10:00:00.000',
        },
    };
}

// Simula la bandeja, la bitácora, la solicitud de cierre y el expediente.
// `estado.pendiente` es mutable para que la prueba cambie la respuesta tras decidir.
function simularBackend({ pendiente = SOLICITUD } = {}) {
    const estado = { pendiente };

    cy.intercept('GET', '**/api/solicitudes-cierre/pendientes', (req) => {
        req.reply({ statusCode: 200, body: estado.pendiente ? [estado.pendiente] : [] });
    }).as('getPendientes');

    cy.intercept('GET', '**/api/bitacora/denuncias', (req) => {
        req.reply({
            statusCode: 200,
            body: [{
                idDenuncia: DENUNCIA_ID, consecutivo: CONSECUTIVO, estado: estado.pendiente ? 'EN_REVISION' : 'EN_TIEMPO',
                fechaCreacion: '01 sep. 2026', funcionarioAsignadoId: 3, funcionarioAsignadoNombre: 'Ana Rojas Méndez',
                areaId: 10, areaNombre: 'ARS San José Centro',
            }],
        });
    }).as('getDenuncias');

    cy.intercept('GET', `**/api/denuncias/${DENUNCIA_ID}/bitacora*`, {
        statusCode: 200,
        body: { items: ENTRADAS_BITACORA, pagina: 1, tamano: 20, total: ENTRADAS_BITACORA.length },
    }).as('getBitacora');

    cy.intercept('GET', `**/api/denuncias/${DENUNCIA_ID}/solicitud-cierre`, (req) => {
        req.reply({
            statusCode: 200,
            body: {
                puedeSolicitar: false,
                motivo: 'Solo el funcionario responsable asignado a la denuncia puede solicitar su cierre.',
                solicitudPendiente: estado.pendiente,
                carpetaConfidencialUrl: null,
                carpetaNoConfidencialUrl: 'https://drive.example/no-confidencial',
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
    cy.intercept('GET', `**/api/denuncias/${DENUNCIA_ID}/solicitudes-cierre/historial`, {
        statusCode: 200, body: HISTORIAL_CIERRE,
    }).as('getHistorialCierre');

    return estado;
}

function entrarComoJefe() {
    entrarComo('jefe', { vista: 'institucional-solicitudes-cierre', permisos: PERMISOS_JEFE_REAL });
    cy.wait('@getPendientes');
}

function abrirRevision() {
    cy.contains('.scp-item', CONSECUTIVO).contains('button', 'Revisar solicitud').click();
    cy.wait(['@getEstadoCierre', '@getDetalle', '@getCriterios', '@getHistorial', '@getHistorialCierre']);
    cy.get('.rc-panel').should('be.visible');
}

describe('HU-017: Gestionar aprobación de resolución de denuncias', () => {

    // -----------------------------------------------------------------
    context('CA-04 Solo la jefatura puede aprobar resoluciones finales', () => {
        it('la jefatura ve "Revisar solicitud" en cada solicitud pendiente de la bandeja', () => {
            simularBackend();
            entrarComoJefe();

            cy.get('.scp-item').should('have.length', 1).first().within(() => {
                cy.contains(CONSECUTIVO).should('be.visible');
                cy.contains('button', 'Revisar solicitud').should('be.visible');
            });
        });

        it('el superusuario también puede revisar y aprobar solicitudes de cualquier área', () => {
            const estado = simularBackend();
            cy.intercept('POST', `**/api/solicitudes-cierre/${SOLICITUD.idSolicitudCierre}/aprobar`, (req) => {
                estado.pendiente = null;
                req.reply({ statusCode: 200, body: resolucion('APROBADA', 'FINALIZADA', null) });
            }).as('postAprobar');

            entrarComo('superusuario', {
                vista: 'institucional-solicitudes-cierre', permisos: PERMISOS_SUPERUSUARIO_REAL,
            });
            cy.wait('@getPendientes');
            cy.contains('.subtitulo', 'Solicitudes de cierre de todas las áreas').should('be.visible');
            abrirRevision();

            cy.contains('.rc-decision-pie button', 'Aprobar cierre').click();
            cy.contains('.modal-card button', 'Sí, aprobar cierre').click();

            cy.wait('@postAprobar');
            cy.contains('La denuncia pasó a estado Finalizada.').should('be.visible');
            cy.get('.scp-item').should('not.exist');
        });

        it('sin el permiso APROBAR_CIERRE no ve el botón de revisar', () => {
            simularBackend();
            entrarComo('superusuario', {
                vista: 'institucional-solicitudes-cierre',
                permisos: PERMISOS_SUPERUSUARIO_REAL.filter((p) => p !== 'APROBAR_CIERRE'),
            });
            cy.wait('@getPendientes');

            cy.get('.scp-item').should('have.length', 1);
            cy.contains('button', 'Revisar solicitud').should('not.exist');
        });

        it('la bitácora ya no ofrece la revisión: solo avisa que se hace desde Solicitudes de cierre', () => {
            simularBackend();
            entrarComo('jefe', { vista: 'institucional-bitacora', permisos: PERMISOS_JEFE_REAL });
            cy.wait('@getDenuncias');
            cy.get('button[aria-label^="Denuncia"]').click();
            cy.contains('.sb-opcion', CONSECUTIVO).click();
            cy.wait(['@getBitacora', '@getEstadoCierre']);

            cy.get('.sc-aviso-revision')
                .should('contain', 'Cierre en revisión')
                .and('contain', 'Puede aprobarla o rechazarla desde Solicitudes de cierre.');
            cy.contains('button', 'Revisar solicitud').should('not.exist');
            cy.get('.rc-panel').should('not.exist');
        });

        it('sin solicitudes pendientes no hay nada que revisar', () => {
            simularBackend({ pendiente: null });
            entrarComoJefe();

            cy.contains('No hay solicitudes de cierre pendientes.').should('be.visible');
            cy.contains('button', 'Revisar solicitud').should('not.exist');
        });
    });

    // -----------------------------------------------------------------
    context('CA-01 La jefatura revisa la información completa del caso antes de decidir', () => {
        it('muestra la resolución propuesta, el expediente, la bitácora, la documentación y el historial', () => {
            simularBackend();
            entrarComoJefe();
            abrirRevision();

            cy.get('.rc-propuesta').within(() => {
                cy.contains(SOLICITUD.solicitadoPorNombre).should('be.visible');
                cy.contains(`Jefatura de ${AREA_NOMBRE}`).should('be.visible');
                cy.contains(SOLICITUD.resolucionFinal).should('be.visible');
                cy.contains(SOLICITUD.observacionesFinales).should('be.visible');
                cy.contains(SOLICITUD.accionesRealizadas).should('be.visible');
            });

            cy.get('.rc-panel').within(() => {
                cy.contains('Farmacia La Esquina').should('be.visible');
                cy.contains('Acetaminofén 500 mg').should('be.visible');
                cy.contains('.badge-criticidad', 'MEDIO').should('be.visible');
                cy.contains('Asignación inicial').should('be.visible');
                // La bitácora inicia oculta y se despliega con el botón.
                cy.get('.rc-lista--bitacora').should('not.exist');
                cy.contains('button', 'Desplegar bitácora').click();
                cy.contains('Inspección realizada al establecimiento.').should('be.visible');
                cy.contains('button', 'Ocultar bitácora').click();
                cy.get('.rc-lista--bitacora').should('not.exist');
                cy.contains('button', 'Desplegar bitácora').should('be.visible');
                cy.contains('a', 'Carpeta no confidencial')
                    .should('have.attr', 'href', 'https://drive.example/no-confidencial');
            });

            // Historial: solo la decision ya tomada (la pendiente se muestra arriba).
            cy.get('.rc-lista--decisiones > li').should('have.length', 1).first().within(() => {
                cy.contains('.rc-decision--rechazada', 'Rechazada').should('be.visible');
                cy.contains('Laura Jiménez Solano').should('be.visible');
                cy.contains('Primera resolución propuesta.').should('be.visible');
                cy.contains('Falta adjuntar el acta de inspección.').should('be.visible');
                cy.contains('volvió a Atrasada').should('be.visible');
            });
        });

        it('"Volver a las solicitudes" regresa a la bandeja sin decidir nada', () => {
            simularBackend();
            cy.intercept('POST', '**/api/solicitudes-cierre/**', cy.spy().as('postSpy'));
            entrarComoJefe();
            abrirRevision();

            cy.contains('.rc-panel button', 'Volver a las solicitudes').click();
            cy.get('.rc-panel').should('not.exist');
            cy.get('.scp-item').should('have.length', 1).and('contain', CONSECUTIVO);
            cy.get('@postSpy').should('not.have.been.called');
        });
    });

    // -----------------------------------------------------------------
    context('CA-02 / CA-06 Rechazar y devolver para ajustes', () => {
        it('rechazar sin observaciones muestra el error en pantalla y no llama a la API', () => {
            simularBackend();
            cy.intercept('POST', '**/api/solicitudes-cierre/**', cy.spy().as('postSpy'));
            entrarComoJefe();
            abrirRevision();

            cy.get('#rc-observaciones').type('   ');
            cy.contains('.rc-decision-pie button', 'Rechazar y devolver').click();

            cy.get('.rc-campo-error').should('contain', 'Indique las observaciones de la jefatura');
            cy.get('#rc-observaciones').should('have.attr', 'aria-invalid', 'true');
            cy.get('.modal-card').should('not.exist');
            cy.get('@postSpy').should('not.have.been.called');
        });

        it('limita las observaciones a 2000 caracteres', () => {
            simularBackend();
            entrarComoJefe();
            abrirRevision();

            cy.get('#rc-observaciones').should('have.attr', 'maxlength', '2000');
            cy.get('.rc-contador').should('have.text', '0/2000');
            cy.get('#rc-observaciones').type('Ajustes');
            cy.get('.rc-contador').should('have.text', '7/2000');
        });

        it('cancelar la confirmación no envía el rechazo', () => {
            simularBackend();
            cy.intercept('POST', '**/api/solicitudes-cierre/**', cy.spy().as('postSpy'));
            entrarComoJefe();
            abrirRevision();

            cy.get('#rc-observaciones').type('Falta el acta firmada.');
            cy.contains('.rc-decision-pie button', 'Rechazar y devolver').click();
            cy.get('.modal-card').should('contain', 'volverá al estado que tenía antes de la solicitud de cierre');
            cy.contains('.modal-card button', 'Cancelar').click();

            cy.get('.modal-card').should('not.exist');
            cy.get('.rc-panel').should('be.visible');
            cy.get('@postSpy').should('not.have.been.called');
        });

        it('rechazar con observaciones devuelve la denuncia a su estado previo y la quita de la bandeja', () => {
            const estado = simularBackend();
            cy.intercept('POST', `**/api/solicitudes-cierre/${SOLICITUD.idSolicitudCierre}/rechazar`, (req) => {
                estado.pendiente = null;
                req.reply({ statusCode: 200, body: resolucion('RECHAZADA', 'ATRASADA', req.body.observaciones) });
            }).as('postRechazar');

            entrarComoJefe();
            abrirRevision();

            cy.get('#rc-observaciones').type('  Falta el acta firmada.  ');
            cy.contains('.rc-decision-pie button', 'Rechazar y devolver').click();
            cy.contains('.modal-card button', 'Sí, rechazar y devolver').click();

            cy.wait('@postRechazar').its('request.body').should('deep.equal', {
                observaciones: 'Falta el acta firmada.',
            });

            cy.contains(`Resolución de ${CONSECUTIVO} rechazada y devuelta para ajustes. La denuncia volvió a estado Atrasada.`)
                .should('be.visible');
            cy.get('.rc-panel').should('not.exist');
            cy.get('.scp-item').should('not.exist');
            cy.contains('No hay solicitudes de cierre pendientes.').should('be.visible');
        });
    });

    // -----------------------------------------------------------------
    context('CA-03 / CA-06 Aprobar la resolución final', () => {
        it('aprobar sin observaciones finaliza la denuncia y la quita de la bandeja', () => {
            const estado = simularBackend();
            cy.intercept('POST', `**/api/solicitudes-cierre/${SOLICITUD.idSolicitudCierre}/aprobar`, (req) => {
                estado.pendiente = null;
                req.reply({ statusCode: 200, body: resolucion('APROBADA', 'FINALIZADA', null) });
            }).as('postAprobar');

            entrarComoJefe();
            abrirRevision();

            cy.contains('.rc-decision-pie button', 'Aprobar cierre').click();
            cy.get('.modal-card').should('contain', 'La denuncia pasará a estado Finalizada');
            cy.contains('.modal-card button', 'Sí, aprobar cierre').click();

            cy.wait('@postAprobar').its('request.body').should('deep.equal', { observaciones: null });

            cy.contains(`Resolución de ${CONSECUTIVO} aprobada. La denuncia pasó a estado Finalizada.`).should('be.visible');
            cy.get('.rc-panel').should('not.exist');
            cy.get('.scp-item').should('not.exist');
        });

        it('aprobar con observaciones las envía recortadas', () => {
            const estado = simularBackend();
            cy.intercept('POST', `**/api/solicitudes-cierre/${SOLICITUD.idSolicitudCierre}/aprobar`, (req) => {
                estado.pendiente = null;
                req.reply({ statusCode: 200, body: resolucion('APROBADA', 'FINALIZADA', req.body.observaciones) });
            }).as('postAprobar');

            entrarComoJefe();
            abrirRevision();

            cy.get('#rc-observaciones').type('  Conforme con la resolución.  ');
            cy.contains('.rc-decision-pie button', 'Aprobar cierre').click();
            cy.contains('.modal-card button', 'Sí, aprobar cierre').click();

            cy.wait('@postAprobar').its('request.body').should('deep.equal', {
                observaciones: 'Conforme con la resolución.',
            });
            cy.get('.rc-panel').should('not.exist');
        });
    });

    // -----------------------------------------------------------------
    context('Errores devueltos por la API', () => {
        [
            { codigo: 403, mensaje: 'Solo la jefatura del área a la que se envió la solicitud puede aprobar o rechazar la resolución.' },
            { codigo: 422, mensaje: 'La solicitud de cierre ya fue resuelta por la jefatura.' },
        ].forEach(({ codigo, mensaje }) => {
            it(`un ${codigo} se muestra en el panel y la solicitud sigue pendiente`, () => {
                simularBackend();
                cy.intercept('POST', `**/api/solicitudes-cierre/${SOLICITUD.idSolicitudCierre}/aprobar`, {
                    statusCode: codigo,
                    body: { error: mensaje },
                }).as('postAprobar');

                entrarComoJefe();
                abrirRevision();

                cy.contains('.rc-decision-pie button', 'Aprobar cierre').click();
                cy.contains('.modal-card button', 'Sí, aprobar cierre').click();

                cy.wait('@postAprobar');
                cy.get('.rc-error').should('contain', mensaje);
                cy.get('.rc-panel').should('be.visible');
                cy.contains('.rc-decision-pie button', 'Aprobar cierre').should('be.enabled');
            });
        });
    });
});
