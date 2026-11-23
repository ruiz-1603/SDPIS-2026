import {
    entrarComo,
    irAlMenu,
    irAPestana,
    filaDe,
    modal,
    campoModal,
    responderFuncionarios,
    PERMISOS_FUNCIONARIO,
    PERMISOS_JEFE,
    AREA_SJ,
    AREA_HEREDIA,
    AREA_UVC,
    FUNC_ANA,
    FUNC_MARTA_INACTIVA,
    FUNCIONARIOS_AREA_SJ,
    FUNCIONARIOS_TODOS,
} from '../support/rolesPermisosHelpers';

// HU-015 Gestionar roles y permisos de usuarios.
// Pruebas funcionales de la interfaz: cada bloque "describe" corresponde a
// un criterio de aceptacion (CA) de la historia. El backend se simula con
// cy.intercept (ver rolesPermisosHelpers.js), por lo que solo se necesita
// el frontend corriendo (npm run dev).

const FUNCIONARIOS = '/api/gestion-usuarios/funcionarios';

describe('HU-015: Gestionar roles y permisos de usuarios', () => {

    // -----------------------------------------------------------------
    context('CA-01 Funcionario: menú según sus permisos y acceso restringido', () => {
        it('solo ve en el menú las opciones de sus permisos', () => {
            entrarComo('funcionario');

            cy.get('.usuario-rol').should('have.text', 'Funcionario');
            cy.contains('.menu-label', 'Bitácora').should('be.visible');
            cy.contains('.menu-label', 'Criticidad').should('be.visible');

            cy.contains('.menu-label', 'Roles y permisos').should('not.exist');
            cy.contains('.menu-label', 'Asignaciones').should('not.exist');
            cy.contains('.menu-label', 'Encargado').should('not.exist');
            cy.contains('.menu-label', 'Disponibilidad de funcionarios').should('not.exist');
            cy.contains('.menu-group-label', 'Administración').should('not.exist');
        });

        it('si llega a "Roles y permisos" por enlace o recarga, ve "Acceso restringido"', () => {
            entrarComo('funcionario', { vista: 'institucional-gestion-usuarios' });

            cy.get('.gu-restringido').should('be.visible').within(() => {
                cy.contains('h2', 'Acceso restringido');
                cy.contains('solicítelo a la jefatura de su área');
            });
            cy.get('.gu-pestanas').should('not.exist');
            cy.get('@listarFuncionarios.all').should('have.length', 0);

            cy.contains('button', 'Volver al inicio').click();
            cy.get('.gu-restringido').should('not.exist');
        });

        it('tampoco puede abrir pantallas de gestión como Asignaciones o Disponibilidad', () => {
            entrarComo('funcionario', { vista: 'institucional-asignaciones' });
            cy.contains('h2', 'Acceso restringido').should('be.visible');

            entrarComo('funcionario', { vista: 'institucional' });
            cy.contains('h2', 'Acceso restringido').should('be.visible');
        });

        it('una revocación rige de inmediato, sin volver a iniciar sesión', () => {
            const sesion = entrarComo('funcionario', {
                permisos: [...PERMISOS_FUNCIONARIO, 'ASIGNAR_DENUNCIAS'],
            });
            cy.contains('.menu-label', 'Asignaciones').should('be.visible');

            // El jefe le revoca el permiso (el backend ya no lo devuelve).
            cy.then(() => { sesion.permisos = PERMISOS_FUNCIONARIO; });
            cy.reload();
            cy.wait('@misPermisos');

            cy.contains('.menu-label', 'Asignaciones').should('not.exist');
            cy.get('.usuario-nombre').should('contain.text', 'Ana Rojas');
            cy.get('@iniciarSesion.all').should('have.length', 0);
        });
    });

    // -----------------------------------------------------------------
    context('CA-02 Jefe de área: solo ve los funcionarios de su área', () => {
        it('lista los funcionarios de su área con sus denuncias y sin selector de área', () => {
            entrarComo('jefe');
            irAlMenu('Roles y permisos');

            cy.contains('[role="tab"]', 'Funcionarios del área').should('have.attr', 'aria-selected', 'true');
            cy.contains('[role="tab"]', 'Áreas').should('not.exist');

            // La identidad y el área salen del token: nunca se envía areaId.
            cy.wait('@listarFuncionarios').its('request.query').should('not.have.property', 'areaId');

            cy.contains('th', 'Área').should('not.exist');
            cy.get('.gu-panel .sb-boton').should('not.exist');
            cy.contains('.gu-resultado', '4 de 5 funcionarios'); // la inactiva se oculta por defecto

            filaDe('Ana Rojas Méndez').should('contain.text', '2 asignadas');
            filaDe('Laura Jiménez Solano').within(() => {
                cy.contains('.gu-chip', 'Usted');
                cy.contains('3 por asignar');
            });
            cy.contains('.gu-stat', 'Por asignar').find('dd').should('have.text', '3');
            cy.contains('.gu-masthead__descripcion', AREA_SJ.nombre);
        });

        it('consulta las denuncias asignadas de un funcionario y sus propias denuncias por asignar', () => {
            cy.intercept('GET', `**${FUNCIONARIOS}/3/denuncias`, {
                statusCode: 200,
                body: [
                    { idDenuncia: 501, consecutivo: 'SDPIS-2026-000501', estado: 'EN_VALIDACION', fechaCreacion: '2026-09-15T08:00:00', areaNombre: AREA_SJ.nombre },
                    { idDenuncia: 502, consecutivo: 'SDPIS-2026-000502', estado: 'EN_PROCESO', fechaCreacion: '2026-09-18T08:00:00', areaNombre: AREA_SJ.nombre },
                ],
            }).as('denunciasAna');
            cy.intercept('GET', `**${FUNCIONARIOS}/1/denuncias`, {
                statusCode: 200,
                body: [
                    { idDenuncia: 601, consecutivo: 'SDPIS-2026-000601', estado: 'REGISTRADA', fechaCreacion: '2026-10-01T08:00:00', areaNombre: AREA_SJ.nombre },
                ],
            }).as('denunciasLaura');

            entrarComo('jefe');
            irAlMenu('Roles y permisos');

            filaDe('Ana Rojas Méndez').contains('button', 'Denuncias').click();
            cy.wait('@denunciasAna');
            modal().within(() => {
                cy.contains('h3', 'Denuncias asignadas');
                cy.contains('SDPIS-2026-000501');
                cy.contains('SDPIS-2026-000502');
                cy.contains('2 denuncias');
                cy.contains('button', 'Cerrar').click();
            });

            filaDe('Laura Jiménez Solano').contains('button', 'Por asignar').click();
            cy.wait('@denunciasLaura');
            modal().within(() => {
                cy.contains('h3', 'Denuncias por asignar');
                cy.contains('El jefe de área no lleva denuncias asignadas');
                cy.contains('SDPIS-2026-000601');
            });
        });

        it('si el backend rechaza la consulta de otra área, se muestra el rechazo', () => {
            entrarComo('jefe');
            cy.intercept({ method: 'GET', pathname: FUNCIONARIOS }, {
                statusCode: 403,
                body: { error: 'No puede consultar funcionarios de otra área.' },
            }).as('rechazo');

            irAlMenu('Roles y permisos');
            cy.wait('@rechazo');
            cy.contains('.gu-alerta--error', 'No puede consultar funcionarios de otra área.');
            cy.get('table.gu-tabla').should('not.exist');
        });
    });

    // -----------------------------------------------------------------
    context('CA-03 Superusuario: áreas con cupo, jefes y funcionarios de todas las áreas', () => {
        beforeEach(() => {
            entrarComo('superusuario');
            irAlMenu('Roles y permisos');
        });

        it('muestra las pestañas de administración completas', () => {
            cy.get('[role="tab"]').then(($tabs) => {
                expect([...$tabs].map((t) => t.innerText.trim()))
                    .to.deep.equal(['Áreas', 'Funcionarios', 'Historial administrativo', 'Ayuda']);
            });
            cy.contains('button', 'Renunciar a superusuario').should('be.visible');
        });

        it('la pestaña Áreas muestra el cupo de jefes de cada área', () => {
            cy.wait('@listarAreas');
            cy.contains('.gu-resultado', '4 de 4 áreas');

            filaDe(AREA_SJ.nombre).find('.gu-cupo__texto')
                .should('have.text', '2 / 2')
                .and('have.class', 'gu-cupo__texto--lleno');
            filaDe(AREA_HEREDIA.nombre).find('.gu-cupo__texto').should('have.text', '1 / 2');
            filaDe(AREA_UVC.nombre).should('contain.text', '+ 1 en renuncia');
            filaDe('Dirección Regional Central Sur').should('contain.text', 'No aplica')
                .contains('button', 'Editar cupo').should('not.exist');

            cy.contains('.gu-stat', 'Áreas').find('dd').should('have.text', '4');
        });

        it('la pestaña Funcionarios lista todas las áreas y filtra jefes y superusuarios en renuncia', () => {
            irAPestana('Funcionarios');
            cy.wait('@listarFuncionarios');

            cy.contains('th', 'Área').should('be.visible');
            filaDe('Rosa Alfaro Chaves').should('contain.text', AREA_HEREDIA.nombre);
            filaDe('Ana Rojas Méndez').should('contain.text', AREA_SJ.nombre);

            cy.intercept({ method: 'GET', pathname: '**/funcionarios', query: { rol: 'jefe_area' } }, responderFuncionarios(FUNCIONARIOS_TODOS)).as('filtrarJefes');
            cy.contains('label.gu-filtro', 'Rol').find('select').select('Jefes de área');
            cy.wait('@filtrarJefes').its('request.query.rol').should('eq', 'jefe_area');
            // Con reintento: espera a que la tabla se redibuje con el filtro.
            cy.get('table.gu-tabla tbody tr').should(($filas) => {
                expect($filas).to.have.length.greaterThan(0);
                $filas.each((_, fila) => {
                    expect(fila.textContent).to.contain('Jefe de área');
                });
            });

            cy.contains('label.gu-filtro', 'Rol').find('select').select('Superusuarios en renuncia');
            cy.get('table.gu-tabla tbody tr').should('have.length', 1);
            filaDe('Diego Salas Mora').contains('.gu-chip--renuncia', 'Superusuario en renuncia');
        });

        it('filtra por área con el selector con búsqueda en ventana', () => {
            irAPestana('Funcionarios');
            cy.wait('@listarFuncionarios');

            cy.intercept({ method: 'GET', pathname: '**/funcionarios', query: { areaId: String(AREA_HEREDIA.idArea) } }, responderFuncionarios(FUNCIONARIOS_TODOS)).as('filtrarPorArea');

            cy.get('.gu-panel button.sb-boton').click();
            cy.get('.sb-modal').within(() => {
                cy.get('input[role="combobox"]').type('Heredia');
                cy.contains('.sb-modal__resultado', '1 de 4');
                cy.contains('.sb-opcion', AREA_HEREDIA.nombre).click();
            });

            cy.wait('@filtrarPorArea').its('request.query.areaId').should('eq', String(AREA_HEREDIA.idArea));
            cy.get('table.gu-tabla tbody tr').should('have.length', 1);
            filaDe('Rosa Alfaro Chaves').should('be.visible');
        });
    });

    // -----------------------------------------------------------------
    context('CA-04 Jefe de área otorga y revoca permisos específicos', () => {
        it('el cambio rige de inmediato, conserva el rol y queda en el historial', () => {
            let ana = { ...FUNC_ANA };
            let solicitudes = [];

            cy.intercept('POST', `**${FUNCIONARIOS}/3/permisos`, (req) => {
                solicitudes.push(req.body);
                ana = { ...ana, permiteAsignar: req.body.accion === 'OTORGAR' };
                req.reply({ statusCode: 204 });
            }).as('gestionarPermiso');

            entrarComo('jefe', {
                funcionarios: () => FUNCIONARIOS_AREA_SJ.map((f) => (f.idFuncionario === 3 ? ana : f)),
            });
            irAlMenu('Roles y permisos');
            cy.wait('@catalogo');

            filaDe('Ana Rojas Méndez').contains('button', 'Permisos').click();
            modal().within(() => {
                cy.contains('h3', 'Permisos específicos');
                cy.contains('el funcionario sigue siendo funcionario');
                // El jefe no ve permisos de nivel superusuario.
                cy.contains('Ver todas las áreas').should('not.exist');

                cy.get('button[aria-label="Otorgar Asignar denuncias"]').click();
            });

            cy.wait('@gestionarPermiso').its('request.body')
                .should('deep.equal', { codigo: 'ASIGNAR_DENUNCIAS', accion: 'OTORGAR' });
            modal().within(() => {
                cy.contains('.gu-alerta--ok', 'Asignar denuncias: otorgado.');
                cy.get('button[aria-label="Revocar Asignar denuncias"]').should('be.visible');
            });

            // La tabla se recarga de inmediato y Ana sigue siendo "Funcionario".
            cy.wait('@listarFuncionarios');
            modal().contains('button', 'Cerrar').click();
            filaDe('Ana Rojas Méndez').within(() => {
                cy.contains('.gu-rol', 'Funcionario');
                cy.contains('.gu-chip--dorado', 'Asignar denuncias');
            });

            // Revocar
            filaDe('Ana Rojas Méndez').contains('button', 'Permisos').click();
            modal().find('button[aria-label="Revocar Asignar denuncias"]').click();
            cy.wait('@gestionarPermiso').its('request.body.accion').should('eq', 'REVOCAR');
            modal().contains('.gu-alerta--ok', 'Asignar denuncias: revocado.');
            modal().contains('button', 'Cerrar').click();
            filaDe('Ana Rojas Méndez').find('.gu-chip--dorado').should('not.exist');

            cy.then(() => expect(solicitudes).to.have.length(2));

            // El historial administrativo se vuelve a consultar con el cambio.
            cy.intercept({ method: 'GET', pathname: '/api/gestion-usuarios/bitacora' }, {
                statusCode: 200,
                body: {
                    total: 1, pagina: 1, tamano: 25,
                    items: [{
                        origen: 'ADM', idRegistro: 900, fecha: new Date().toISOString(),
                        categoria: 'PERMISOS', tipoEvento: 'PERMISO_REVOCADO',
                        detalle: 'Revocó ASIGNAR_DENUNCIAS',
                        valorAnterior: 'ASIGNAR_DENUNCIAS=SI', valorNuevo: 'ASIGNAR_DENUNCIAS=NO',
                        realizadoPorNombre: 'Laura Jiménez Solano', funcionarioAfectadoNombre: 'Ana Rojas Méndez',
                        areaNombre: AREA_SJ.nombre, detalleTecnico: null,
                    }],
                },
            }).as('bitacoraConCambio');
            irAPestana('Historial administrativo');
            cy.wait('@bitacoraConCambio');
            cy.contains('.gu-evento__detalle', 'Revocó «Asignar denuncias»');
            cy.contains('.gu-cambios li', 'Permiso «Asignar denuncias»').should('contain.text', 'Otorgado → No otorgado');
        });
    });

    // -----------------------------------------------------------------
    context('CA-05 El jefe no puede revocarse ni revocar a otro jefe el permiso de asignar', () => {
        beforeEach(() => {
            entrarComo('jefe');
            irAlMenu('Roles y permisos');
            cy.wait('@listarFuncionarios');
        });

        it('no tiene acciones de permisos ni edición sobre sí mismo', () => {
            filaDe('Laura Jiménez Solano').within(() => {
                cy.contains('button', 'Permisos').should('not.exist');
                cy.contains('button', 'Editar').should('not.exist');
            });
        });

        it('para otro jefe, "Asignar denuncias" aparece como permanente y sin botón de revocar', () => {
            filaDe('Carlos Vargas Mora').contains('button', 'Editar').should('not.exist');
            filaDe('Carlos Vargas Mora').contains('button', 'Permisos').click();

            modal().within(() => {
                cy.contains('.gu-permiso', 'Asignar denuncias')
                    .should('contain.text', 'Permanente del jefe')
                    .find('button').should('not.exist');
                cy.get('button[aria-label^="Revocar"]').should('not.exist');
                cy.contains('.gu-permiso', 'Ver denuncias del área').should('contain.text', 'Incluido por el rol');
            });
        });
    });

    // -----------------------------------------------------------------
    context('CA-06 Cupo de jefes completo', () => {
        beforeEach(() => {
            entrarComo('superusuario');
            irAlMenu('Roles y permisos');
        });

        it('al crear un jefe, el área con cupo completo no se puede elegir', () => {
            irAPestana('Funcionarios');
            cy.contains('button', '+ Nuevo funcionario').click();

            campoModal('Rol').find('select').select('Jefe de área');
            modal().contains('Se respeta el cupo de jefes del área');
            campoModal('Área').find('select').within(() => {
                cy.contains('option', `${AREA_SJ.nombre} — jefes 2/2`).should('be.disabled');
                cy.contains('option', `${AREA_HEREDIA.nombre} — jefes 1/2`).should('not.be.disabled');
                cy.contains('option', 'Dirección Regional Central Sur — no admite jefe').should('be.disabled');
            });
        });

        it('al promover a jefe en un área llena, el sistema lo rechaza', () => {
            cy.intercept('PUT', `**${FUNCIONARIOS}/3`, {
                statusCode: 422,
                body: { error: 'El área alcanzó su cupo de jefes (2). Amplíe el cupo para crear o promover otro jefe.' },
            }).as('promover');

            irAPestana('Funcionarios');
            filaDe('Ana Rojas Méndez').contains('button', 'Editar').click();
            campoModal('Rol').find('select').select('Jefe de área');
            modal().contains('button', 'Guardar cambios').click();

            cy.wait('@promover').its('request.body').should('deep.equal', { rol: 'jefe_area' });
            modal().contains('.gu-alerta--error', 'El área alcanzó su cupo de jefes (2)');
        });

        it('el superusuario amplía el cupo, sin dejarlo por debajo de los jefes activos', () => {
            cy.intercept('PUT', `**/api/gestion-usuarios/areas/${AREA_SJ.idArea}/max-jefes`, { statusCode: 204 }).as('cupo');

            filaDe(AREA_SJ.nombre).contains('button', 'Editar cupo').click();
            modal().within(() => {
                cy.contains('Actualmente hay 2 jefes activos');
                cy.get('input[type="number"]').clear().type('1');
                cy.root().parent().contains('button', 'Guardar cupo').click();
                cy.contains('.gu-alerta--error', 'Indique un número entero entre 2 y 999.');

                cy.get('input[type="number"]').clear().type('3');
                cy.root().parent().contains('button', 'Guardar cupo').click();
            });

            cy.wait('@cupo').its('request.body').should('deep.equal', { maxJefes: 3 });
            cy.contains('.gu-toast', `Cupo de jefes de ${AREA_SJ.nombre}: 3.`);
            cy.wait('@listarAreas');
        });
    });

    // -----------------------------------------------------------------
    context('CA-07 Máximo 2 superusuarios activos', () => {
        it('rechaza la creación de un tercer superusuario', () => {
            cy.intercept('POST', `**${FUNCIONARIOS}`, {
                statusCode: 422,
                body: { error: 'Ya existen 2 superusuarios activos. No se puede registrar otro.' },
            }).as('crear');

            entrarComo('superusuario');
            irAlMenu('Roles y permisos');
            irAPestana('Funcionarios');
            cy.contains('button', '+ Nuevo funcionario').click();

            campoModal('Nombre completo').find('input').type('Mario Solís Vega');
            campoModal('Correo institucional').find('input').type('mario.solis@misalud.go.cr');
            campoModal('Rol').find('select').select('Superusuario');
            modal().contains('.gu-alerta--aviso', 'Solo se permiten 2 superusuarios activos al mismo tiempo.');
            campoModal('Área').find('select').select(String(AREA_UVC.idArea));
            modal().contains('button', 'Registrar funcionario').click();

            cy.wait('@crear').its('request.body').should('include', { rol: 'superusuario', areaId: AREA_UVC.idArea });
            modal().contains('.gu-alerta--error', 'Ya existen 2 superusuarios activos.');
            cy.get('.gu-toast').should('not.exist');
        });
    });

    // -----------------------------------------------------------------
    context('CA-08 Renuncia del superusuario', () => {
        it('queda como jefe de área en renuncia y pierde la administración del sistema', () => {
            const sesion = entrarComo('superusuario');
            cy.intercept('POST', '**/api/gestion-usuarios/superusuario/renuncia', (req) => {
                sesion.permisos = PERMISOS_JEFE; // el backend ya no le da ADMINISTRAR_SISTEMA
                req.reply({ statusCode: 204 });
            }).as('renuncia');

            irAlMenu('Roles y permisos');
            cy.contains('button', 'Renunciar a superusuario').click();

            modal().within(() => {
                cy.contains('h3', 'Renunciar al rol de superusuario');
                cy.contains('no ocupa cupo de jefes');
                cy.contains('perder todos los privilegios de superusuario');
                cy.contains(AREA_UVC.nombre);
                cy.get('select').should('not.exist');
            });
            modal().contains('button', 'Renunciar al rol').should('be.disabled');
            modal().find('input[placeholder="CONFIRMAR"]').type('CONFIRMA');
            modal().contains('button', 'Renunciar al rol').should('be.disabled');
            modal().find('input[placeholder="CONFIRMAR"]').type('R');
            modal().contains('button', 'Renunciar al rol').should('not.be.disabled').click();

            cy.wait('@renuncia').its('request.body').should('be.oneOf', [undefined, null, '']);
            cy.wait('@misPermisos');

            cy.contains('.gu-toast', 'Renunció al rol de superusuario');
            cy.get('.usuario-rol').should('have.text', 'Jefe de área');
            cy.contains('[role="tab"]', 'Áreas').should('not.exist');
            cy.contains('[role="tab"]', 'Funcionarios del área').should('be.visible');
            cy.contains('button', 'Renunciar a superusuario').should('not.exist');
        });

        it('no permite renunciar si no queda otro superusuario activo', () => {
            entrarComo('superusuario');
            cy.intercept('POST', '**/api/gestion-usuarios/superusuario/renuncia', {
                statusCode: 422,
                body: { error: 'Debe quedar al menos otro superusuario activo.' },
            }).as('renuncia');

            irAlMenu('Roles y permisos');
            cy.contains('button', 'Renunciar a superusuario').click();
            modal().find('input[placeholder="CONFIRMAR"]').type('CONFIRMAR');
            modal().contains('button', 'Renunciar al rol').click();

            cy.wait('@renuncia');
            modal().contains('.gu-alerta--error', 'Debe quedar al menos otro superusuario activo.');
            cy.get('.usuario-rol').should('have.text', 'Superusuario');
        });
    });

    // -----------------------------------------------------------------
    context('CA-09 Baja de un funcionario con denuncias asignadas activas', () => {
        it('impide la baja hasta que se reasignen sus denuncias', () => {
            cy.intercept('PUT', `**${FUNCIONARIOS}/*`).as('actualizar');

            entrarComo('jefe');
            irAlMenu('Roles y permisos');
            filaDe('Ana Rojas Méndez').contains('button', 'Editar').click();

            modal().within(() => {
                cy.get('.gu-interruptor input[type="checkbox"]').first().should('be.checked').and('be.disabled');
                cy.contains('Tiene 2 denuncias asignadas activas: reasígnelas antes de darlo de baja.');
                cy.contains('button', 'Dar de baja').should('not.exist');
            });
            cy.get('@actualizar.all').should('have.length', 0);
        });
    });

    // -----------------------------------------------------------------
    context('CA-10 Único jefe activo de un área', () => {
        it('el sistema impide darlo de baja', () => {
            cy.intercept('PUT', `**${FUNCIONARIOS}/6`, {
                statusCode: 422,
                body: { error: 'No se puede dar de baja ni quitar el rol al único jefe activo del área.' },
            }).as('bajaJefe');

            entrarComo('superusuario');
            irAlMenu('Roles y permisos');
            irAPestana('Funcionarios');
            filaDe('Rosa Alfaro Chaves').contains('button', 'Editar').click();

            modal().find('.gu-interruptor input[type="checkbox"]').first().uncheck();
            modal().contains('Si es el único jefe activo del área, el sistema no permitirá la baja.');
            modal().contains('label', 'Confirmo la baja de Rosa Alfaro Chaves').click();
            modal().contains('button', 'Dar de baja').click();

            cy.wait('@bajaJefe').its('request.body').should('deep.equal', { activo: false });
            modal().contains('.gu-alerta--error', 'único jefe activo del área');
        });

        it('el sistema impide quitarle el rol de jefe', () => {
            cy.intercept('PUT', `**${FUNCIONARIOS}/6`, {
                statusCode: 422,
                body: { error: 'No se puede dar de baja ni quitar el rol al único jefe activo del área.' },
            }).as('quitarRol');

            entrarComo('superusuario');
            irAlMenu('Roles y permisos');
            irAPestana('Funcionarios');
            filaDe('Rosa Alfaro Chaves').contains('button', 'Editar').click();

            campoModal('Rol').find('select').select('Funcionario');
            modal().contains('Al dejar de ser jefe pierde el permiso de asignar');
            modal().contains('button', 'Guardar cambios').click();

            cy.wait('@quitarRol').its('request.body').should('deep.equal', { rol: 'validador' });
            modal().contains('.gu-alerta--error', 'único jefe activo del área');
        });
    });

    // -----------------------------------------------------------------
    context('CA-11 Baja y reactivación: no recupera los permisos delegados', () => {
        it('al dar de baja se advierte que perderá sus permisos delegados', () => {
            cy.intercept('PUT', `**${FUNCIONARIOS}/4`, { statusCode: 204 }).as('baja');

            entrarComo('jefe');
            irAlMenu('Roles y permisos');
            filaDe('Pedro Castro Rojas').within(() => {
                cy.contains('.gu-chip--dorado', 'Asignar denuncias');
                cy.contains('.gu-chip', 'Ver denuncias del área');
                cy.contains('button', 'Editar').click();
            });

            modal().find('.gu-interruptor input[type="checkbox"]').first().uncheck();
            modal().contains('.gu-alerta--aviso', 'Perderá todos sus permisos delegados')
                .and('contain.text', 'no se devuelven al reactivarlo');
            modal().contains('button', 'Dar de baja').should('be.disabled');
            modal().contains('label', 'Confirmo la baja de Pedro Castro Rojas').click();
            modal().contains('button', 'Dar de baja').click();

            cy.wait('@baja').its('request.body').should('deep.equal', { activo: false });
            cy.contains('.gu-toast', 'Se dio de baja a Pedro Castro Rojas')
                .and('contain.text', 'administrador del MSI');
        });

        it('al reactivarlo queda sin permisos delegados ni permiso de asignar', () => {
            // Marta tenia permisos antes de su baja; el backend se los revocó.
            let marta = { ...FUNC_MARTA_INACTIVA };
            cy.intercept('PUT', `**${FUNCIONARIOS}/5`, (req) => {
                marta = { ...marta, activo: true, permisosDelegados: [], permiteAsignar: false };
                req.reply({ statusCode: 204 });
            }).as('reactivar');

            entrarComo('jefe', {
                funcionarios: () => FUNCIONARIOS_AREA_SJ.map((f) => (f.idFuncionario === 5 ? marta : f)),
            });
            irAlMenu('Roles y permisos');

            cy.contains('label.gu-filtro', 'Estado').find('select').select('Inactivos');
            filaDe('Marta Quesada Arias').should('have.class', 'gu-fila-inactiva')
                .contains('button', 'Editar').click();
            modal().find('.gu-interruptor input[type="checkbox"]').first().check();
            modal().contains('button', 'Guardar cambios').click();

            cy.wait('@reactivar').its('request.body').should('deep.equal', { activo: true });
            cy.contains('.gu-toast', 'Se actualizó a Marta Quesada Arias.');

            cy.contains('label.gu-filtro', 'Estado').find('select').select('Activos');
            filaDe('Marta Quesada Arias').within(() => {
                cy.contains('.gu-estado', 'Activo');
                cy.get('.gu-chips .gu-chip').should('not.exist');
                cy.contains('button', 'Permisos').click();
            });
            modal().find('button[aria-label^="Revocar"]').should('not.exist');
            modal().find('button[aria-label="Otorgar Asignar denuncias"]').should('be.visible');
        });
    });

    // -----------------------------------------------------------------
    context('CA-12 Bitácora administrativa inmutable con valores anteriores y nuevos', () => {
        it('muestra fecha, responsable, afectado, área y el cambio en lenguaje comprensible', () => {
            const hoy = new Date();
            entrarComo('superusuario');
            cy.intercept({ method: 'GET', pathname: '/api/gestion-usuarios/bitacora' }, {
                statusCode: 200,
                body: {
                    total: 2, pagina: 1, tamano: 25,
                    items: [
                        {
                            origen: 'ADM', idRegistro: 10, fecha: hoy.toISOString(),
                            categoria: 'FUNCIONARIOS', tipoEvento: 'MODIFICACION', detalle: 'Cambio de rol',
                            valorAnterior: 'rol=validador; activo=S', valorNuevo: 'rol=jefe_area; activo=S',
                            realizadoPorNombre: 'Sofía Herrera Brenes', funcionarioAfectadoNombre: 'Rosa Alfaro Chaves',
                            areaNombre: AREA_HEREDIA.nombre, detalleTecnico: null,
                        },
                        {
                            origen: 'ADM', idRegistro: 11, fecha: hoy.toISOString(),
                            categoria: 'CUPOS', tipoEvento: 'CUPO', detalle: 'Cambio de cupo de jefes',
                            valorAnterior: 'max_jefes=2', valorNuevo: 'max_jefes=3',
                            realizadoPorNombre: 'Sofía Herrera Brenes', funcionarioAfectadoNombre: null,
                            areaNombre: AREA_SJ.nombre, detalleTecnico: null,
                        },
                    ],
                },
            }).as('bitacoraSuper');

            irAlMenu('Roles y permisos');
            irAPestana('Historial administrativo');
            cy.wait('@bitacoraSuper');

            cy.contains('.gu-nota', 'Registro inmutable: no se puede modificar ni eliminar.');
            cy.contains('.gu-dia__fecha', 'Hoy');
            cy.contains('.gu-dia__conteo', '2 eventos');

            cy.contains('tr', 'Cambio de rol').within(() => {
                cy.contains('.gu-cambios li', 'Rol:').should('contain.text', 'Funcionario → Jefe de área');
                cy.contains('td', 'Sofía Herrera Brenes');
                cy.contains('td', 'Rosa Alfaro Chaves');
                cy.contains('td', AREA_HEREDIA.nombre);
                cy.get('.gu-col-hora').invoke('text').should('match', /\d{1,2}:\d{2}/);
            });
            cy.contains('tr', 'Cambio de cupo de jefes')
                .find('.gu-cambios li').should('contain.text', 'Cupo de jefes:').and('contain.text', '2 → 3');

            // Sin acciones para modificar ni eliminar registros.
            cy.get('.gu-tabla--historial').within(() => {
                cy.get('button').should('not.exist');
                cy.get('input').should('not.exist');
            });
        });

        it('filtra por periodo, categoría y búsqueda por funcionario', () => {
            entrarComo('superusuario');
            irAlMenu('Roles y permisos');
            irAPestana('Historial administrativo');
            cy.wait('@bitacora').its('request.query').should('include', { incluirConsultas: 'false' });

            cy.intercept({ method: 'GET', pathname: '**/bitacora', query: { categoria: 'PERMISOS' } }, {
                statusCode: 200, body: { items: [], total: 0, pagina: 1, tamano: 25 },
            }).as('bitacoraPermisos');
            cy.contains('label.gu-filtro', 'Categoría').find('select').select('Permisos');
            cy.wait('@bitacoraPermisos').its('request.query.categoria').should('eq', 'PERMISOS');

            cy.intercept({ method: 'GET', pathname: '**/bitacora' }, (req) => {
                if (!req.query.desde) req.alias = 'bitacoraTodo';
                req.reply({ statusCode: 200, body: { items: [], total: 0, pagina: 1, tamano: 25 } });
            });
            cy.contains('label.gu-filtro', 'Periodo').find('select').select('Todo el historial');
            cy.wait('@bitacoraTodo').its('request.query').should('not.have.property', 'desde');

            cy.intercept({ method: 'GET', pathname: '**/bitacora', query: { texto: 'Ana Rojas' } }, {
                statusCode: 200, body: { items: [], total: 0, pagina: 1, tamano: 25 },
            }).as('bitacoraTexto');
            cy.get('input[aria-label="Buscar en el historial"]').type('Ana Rojas');
            cy.wait('@bitacoraTexto').its('request.query.texto').should('eq', 'Ana Rojas');
        });
    });

    // -----------------------------------------------------------------
    context('CA-13 Jefe de área consulta el historial administrativo', () => {
        it('solo ve su área y no ve direcciones IP', () => {
            entrarComo('jefe');
            cy.intercept({ method: 'GET', pathname: '/api/gestion-usuarios/bitacora' }, {
                statusCode: 200,
                body: {
                    total: 1, pagina: 1, tamano: 25,
                    items: [{
                        origen: 'SES', idRegistro: 20, fecha: new Date().toISOString(),
                        categoria: 'SESION', tipoEvento: 'INTENTO_FALLIDO', detalle: 'Intento de inicio de sesión fallido',
                        valorAnterior: null, valorNuevo: null,
                        realizadoPorNombre: null, funcionarioAfectadoNombre: 'Ana Rojas Méndez',
                        areaNombre: AREA_SJ.nombre, detalleTecnico: null, // el backend no lo envía al jefe
                    }],
                },
            }).as('bitacoraJefe');

            irAlMenu('Roles y permisos');
            irAPestana('Historial administrativo');

            cy.wait('@bitacoraJefe').its('request.query').should('not.have.property', 'areaId');
            cy.get('.gu-tabla--historial thead th').should('have.length', 4);
            cy.contains('.gu-tabla--historial th', 'Área').should('not.exist');
            cy.get('.gu-panel .sb-boton').should('not.exist');

            cy.contains('.gu-categoria--alerta', 'Intento fallido');
            cy.get('.gu-evento__tecnico').should('not.exist');
            cy.get('.gu-tabla--historial').invoke('text').should('not.match', /\b\d{1,3}(\.\d{1,3}){3}\b/);
        });

        it('el superusuario sí ve el detalle técnico de las sesiones', () => {
            entrarComo('superusuario');
            cy.intercept({ method: 'GET', pathname: '/api/gestion-usuarios/bitacora' }, {
                statusCode: 200,
                body: {
                    total: 1, pagina: 1, tamano: 25,
                    items: [{
                        origen: 'SES', idRegistro: 21, fecha: new Date().toISOString(),
                        categoria: 'SESION', tipoEvento: 'INTENTO_FALLIDO', detalle: 'Intento de inicio de sesión fallido',
                        valorAnterior: null, valorNuevo: null,
                        realizadoPorNombre: null, funcionarioAfectadoNombre: null,
                        areaNombre: null, detalleTecnico: 'IP 10.0.0.15 · usuario digitado: arojas',
                    }],
                },
            }).as('bitacoraSuper');

            irAlMenu('Roles y permisos');
            irAPestana('Historial administrativo');
            cy.wait('@bitacoraSuper');

            cy.contains('.gu-tabla--historial th', 'Área');
            cy.contains('.gu-evento__tecnico', 'IP 10.0.0.15');
        });
    });

    // -----------------------------------------------------------------
    context('CA-14 Visibilidad en Criterios técnicos', () => {
        const denuncia = (id, funcionarioId, funcionarioNombre) => ({
            idDenuncia: id,
            consecutivo: `SDPIS-2026-000${id}`,
            fechaCreacion: '2026-09-20T08:00:00',
            estado: 'EN_VALIDACION',
            areaActualId: AREA_SJ.idArea,
            areaNombre: AREA_SJ.nombre,
            funcionarioAsignadoId: funcionarioId,
            funcionarioAsignadoNombre: funcionarioNombre,
        });

        const evaluacion = (id) => ({
            idEvaluacion: id, denunciaId: id, funcionarioValidadorId: 3,
            fechaEvaluacion: '2026-09-25T10:00:00', puntajeTotal: 5,
            vinculadoComercioElectronico: 'N', danoPotencialSalud: 'Alto', requiereCriterioEspecializado: 'No',
            observacionesTecnicas: 'Producto sin registro sanitario.',
            nivelCriticidad: 'ALTO', fechaLimite: '2026-11-24T00:00:00',
        });

        beforeEach(() => {
            cy.intercept('GET', '**/api/denuncias/*/alerta-sanitaria-vigente', { statusCode: 200, body: { tieneAlertaVigente: false, numerosAlerta: [] } });
            cy.intercept('GET', '**/api/denuncias/*/detalle', {
                statusCode: 200,
                body: { idDenuncia: 0, consecutivo: '', fechaCreacion: null, estado: 'EN_TIEMPO', hecho: null, ubicacion: null, productos: [] },
            });
        });

        it('el funcionario solo ve sus denuncias asignadas', () => {
            cy.intercept({ method: 'GET', pathname: '/api/denuncias-visibles' }, {
                statusCode: 200, body: [denuncia(701, 3, 'Ana Rojas Méndez')],
            }).as('visibles');
            cy.intercept('GET', '**/api/denuncias/701/criterios-tecnicos', { statusCode: 204 });

            entrarComo('funcionario');
            irAlMenu('Criticidad');

            cy.wait('@visibles').its('request.query').then((q) => {
                expect(q).to.include({ filtro: 'ASIGNADAS' });
                expect(q).not.to.have.property('funcionarioId');
                expect(q).not.to.have.property('areaId');
            });
            cy.contains('h3', 'Mis denuncias asignadas');
            cy.get('button[aria-label^="Denuncia"]').click();
            cy.get('.sb-opcion').should('have.length', 1);
            cy.contains('.sb-opcion', 'SDPIS-2026-000701').click();
            cy.get('form#ct-form').should('be.visible'); // puede registrar las suyas
        });

        it('el jefe de área consulta las de su área en solo lectura', () => {
            cy.intercept({ method: 'GET', pathname: '/api/denuncias-visibles' }, {
                statusCode: 200,
                body: [denuncia(801, 3, 'Ana Rojas Méndez'), denuncia(802, 4, 'Pedro Castro Rojas')],
            }).as('visibles');
            cy.intercept('GET', '**/api/denuncias/801/criterios-tecnicos', { statusCode: 200, body: evaluacion(801) });

            entrarComo('jefe');
            irAlMenu('Criticidad');
            cy.wait('@visibles');

            cy.contains('h3', 'Denuncias asignadas');
            cy.get('button[aria-label^="Denuncia"]').click();
            cy.contains('.sb-opcion', 'SDPIS-2026-000802').should('contain.text', 'Pedro Castro Rojas');
            cy.contains('.sb-opcion', 'SDPIS-2026-000801').click();
            cy.contains('.ct-solo-lectura', 'Vista de solo lectura');
            cy.get('form#ct-form').should('not.exist');
            cy.get('.ct-detalle').contains('button', /^Editar$/).should('not.exist');
        });

        it('solo el superusuario puede modificar criterios ya registrados', () => {
            cy.intercept({ method: 'GET', pathname: '/api/denuncias-visibles' }, {
                statusCode: 200, body: [denuncia(901, 3, 'Ana Rojas Méndez')],
            }).as('visibles');
            cy.intercept('GET', '**/api/denuncias/901/criterios-tecnicos', { statusCode: 200, body: evaluacion(901) });

            entrarComo('superusuario');
            irAlMenu('Criticidad');

            cy.wait('@visibles').its('request.query.criterios').should('eq', 'REGISTRADOS');
            cy.contains('h3', 'Denuncias con criterios registrados');

            cy.get('button[aria-label^="Denuncia"]').click();
            cy.contains('.sb-opcion', 'SDPIS-2026-000901').click();
            cy.contains('Criterios técnicos de análisis');
            cy.get('.ct-solo-lectura').should('not.exist');
        });
    });

    // -----------------------------------------------------------------
    context('Interfaz: pestaña Ayuda', () => {
        it('está disponible para el jefe de área', () => {
            entrarComo('jefe');
            irAlMenu('Roles y permisos');
            irAPestana('Ayuda');
            cy.get('[role="tabpanel"]').should('not.be.empty');
        });
    });
});
