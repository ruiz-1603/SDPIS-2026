describe('HU-016: Registrar seguimiento y bitácora', () => {
    const DENUNCIA_ID = 100;

    const paginaInicial = {
        items: [
            {
                idBitacora: 1,
                denunciaId: DENUNCIA_ID,
                funcionarioId: null,
                funcionarioNombre: null,
                areaOrigenId: 1,
                areaOrigenNombre: 'Area Rectora San Jose',
                fecha: '2026-09-10T08:00:00.000',
                tipoRegistro: 'registro_denuncia',
                contenido: 'Denuncia recibida y registrada automaticamente en el sistema.'
            },
            {
                idBitacora: 2,
                denunciaId: DENUNCIA_ID,
                funcionarioId: 1,
                funcionarioNombre: 'Jefe Prueba',
                areaOrigenId: 1,
                areaOrigenNombre: 'Area Rectora San Jose',
                fecha: '2026-09-11T09:30:00.000',
                tipoRegistro: 'comentario',
                contenido: 'Seguimiento telefonico con el denunciante'
            }
        ],
        pagina: 1,
        tamanoPagina: 20,
        total: 2
    };

    beforeEach(() => {
        cy.intercept('GET', '**/api/funcionarios/*/bitacora/denuncias', {
            statusCode: 200,
            body: [
                {
                    idDenuncia: DENUNCIA_ID,
                    consecutivo: 'DEN-2026-100',
                    estado: 'EN_TIEMPO',
                    funcionarioAsignadoNombre: 'María Mora'
                }
            ]
        }).as('getDenuncias');

        cy.intercept('GET', `**/api/denuncias/${DENUNCIA_ID}/bitacora*`, {
            statusCode: 200,
            body: paginaInicial
        }).as('getBitacora');

        cy.visit('/');
        cy.contains('Ingresar como funcionario').click({ force: true });
        cy.get('#usuarioMsi').should('be.visible').type('jefe');
        cy.get('#claveMsi').type('123');
        cy.contains('button', 'Iniciar sesión').click();

        cy.contains('.menu-label', 'Bitácora').click();
        cy.get('.bt-layout').should('be.visible');

        // NOTA: BitacoraPage no carga las bandejas de Asignaciones/Encargado
        // (esas peticiones -pendientes-asignacion/asignadas- no existen en este
        // flujo), así que aquí solo se espera lo que la página sí pide.
        cy.wait('@getDenuncias');

        // El selector de denuncia va arriba de la bitácora (SelectorBuscable).
        cy.get('button[aria-label^="Denuncia"]').click();
        cy.contains('.sb-opcion', 'DEN-2026-100').click();
        cy.wait('@getBitacora');
    });

    it('Debería mostrar la bitácora de la denuncia en orden cronológico con todos los campos visibles', () => {
        cy.get('.bt-entrada').should('have.length', 2);

        cy.get('.bt-entrada').eq(0).within(() => {
            cy.contains('Registro de la denuncia').should('be.visible');
            cy.contains('Denuncia recibida y registrada automaticamente en el sistema.').should('be.visible');
            cy.contains('Sistema (acción automática)').should('be.visible');
            cy.contains('Area Rectora San Jose').should('be.visible');
        });

        cy.get('.bt-entrada').eq(1).within(() => {
            cy.contains('.bt-tipo', 'Comentarios').should('be.visible');
            cy.contains('Seguimiento telefonico con el denunciante').should('be.visible');
            cy.contains('Jefe Prueba').should('be.visible');
        });
    });

    it('Debería permitir agregar un comentario manual y mostrarlo en la bitácora', () => {
        const textoComentario = 'Se contacto al denunciante para solicitar informacion adicional';

        cy.intercept('POST', `**/api/denuncias/${DENUNCIA_ID}/bitacora`, {
            statusCode: 201,
            body: {
                idBitacora: 3,
                denunciaId: DENUNCIA_ID,
                funcionarioId: 1,
                funcionarioNombre: 'Jefe Prueba',
                areaOrigenId: 1,
                areaOrigenNombre: 'Area Rectora San Jose',
                fecha: '2026-09-12T10:00:00.000',
                tipoRegistro: 'comentario',
                contenido: textoComentario
            }
        }).as('postComentario');

        cy.intercept('GET', `**/api/denuncias/${DENUNCIA_ID}/bitacora*`, {
            statusCode: 200,
            body: {
                ...paginaInicial,
                items: [
                    ...paginaInicial.items,
                    {
                        idBitacora: 3,
                        denunciaId: DENUNCIA_ID,
                        funcionarioId: 1,
                        funcionarioNombre: 'Jefe Prueba',
                        areaOrigenId: 1,
                        areaOrigenNombre: 'Area Rectora San Jose',
                        fecha: '2026-09-12T10:00:00.000',
                        tipoRegistro: 'comentario',
                        contenido: textoComentario
                    }
                ],
                total: 3
            }
        }).as('getBitacoraConComentario');

        cy.get('#bt-comentario').type(textoComentario);
        cy.contains('button', 'Agregar comentario').click();

        cy.wait('@postComentario').then((interception) => {
            expect(interception.request.body.funcionarioId).to.equal(1);
            expect(interception.request.body.contenido).to.equal(textoComentario);
        });
        cy.wait('@getBitacoraConComentario');

        cy.contains('.bt-contenido', textoComentario).should('be.visible');
        cy.get('.bt-entrada').should('have.length', 3);
    });

    it('Debería filtrar la bitácora por tipo de acción y explicar qué incluye el tipo elegido', () => {
        cy.intercept('GET', `**/api/denuncias/${DENUNCIA_ID}/bitacora*`, (req) => {
            const comentarios = paginaInicial.items.filter((e) => e.tipoRegistro === 'comentario');
            req.reply({
                statusCode: 200,
                body: req.query.tipo === 'comentario'
                    ? { ...paginaInicial, items: comentarios, total: comentarios.length }
                    : paginaInicial
            });
        }).as('getBitacoraFiltrada');

        cy.get('button[aria-label^="Tipo de acción"]').click();
        cy.contains('.sb-opcion', 'Comentarios').click();

        cy.wait('@getBitacoraFiltrada').its('request.query.tipo').should('equal', 'comentario');
        cy.get('.bt-entrada').should('have.length', 1);
        cy.contains('.bt-filtro-ayuda', 'Comentarios manuales').should('be.visible');
    });

    it('No debería mostrar ninguna opción para editar o eliminar un registro existente', () => {
        cy.contains('.bt-detalle-head .subtitulo', 'no se pueden editar ni eliminar entradas').should('be.visible');

        cy.get('.bt-entrada').each(($entrada) => {
            cy.wrap($entrada).find('button, a').should('not.exist');
        });

        cy.get('.bt-linea-tiempo').contains('button', 'Editar').should('not.exist');
        cy.get('.bt-linea-tiempo').contains('button', 'Eliminar').should('not.exist');
    });
});