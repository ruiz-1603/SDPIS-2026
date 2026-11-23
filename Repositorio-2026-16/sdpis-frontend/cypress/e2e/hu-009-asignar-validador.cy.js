describe('HU-009: Asignar denuncia a un funcionario validador', () => {
    beforeEach(() => {
       
        cy.intercept('GET', '**/api/areas/*/denuncias/pendientes-asignacion', {
            statusCode: 200,
            body: [
                { idDenuncia: 100, consecutivo: 'DEN-2026-001', fechaRecepcion: '2026-09-10' },
                { idDenuncia: 101, consecutivo: 'DEN-2026-002', fechaRecepcion: '2026-09-15' },
            ],
        }).as('getDenunciasPendientes');

        cy.intercept('GET', '**/api/areas/*/denuncias/asignadas', {
            statusCode: 200,
            body: [],
        }).as('getDenunciasAsignadas');

        cy.intercept('GET', '**/api/areas/*/validadores-disponibles', {
            statusCode: 200,
            body: [
                { idFuncionario: 2, nombre: 'Juan Pérez', cargaActiva: 2 },
                { idFuncionario: 3, nombre: 'Ana Rojas', cargaActiva: 0 },
            ],
        }).as('getValidadores');

        cy.visit('/');
        cy.contains('Ingresar como funcionario').click({ force: true });
        cy.get('#usuarioMsi').should('be.visible').type('jefe');
        cy.get('#claveMsi').type('123');
        cy.contains('button', 'Iniciar sesión').click();

        cy.contains('.menu-label', 'Asignaciones').click();
        cy.get('.page-asignacion-manual').should('be.visible');
    });

    it('Debería mostrar la lista de denuncias pendientes de asignación', () => {
        cy.wait('@getDenunciasPendientes');
        cy.contains('DEN-2026-001').should('be.visible');
        cy.contains('DEN-2026-002').should('be.visible');
    });

    it('Debería poder seleccionar un funcionario disponible y asignarle la denuncia exitosamente', () => {
        cy.wait('@getDenunciasPendientes');
        cy.wait('@getValidadores');

        cy.intercept('POST', '**/api/areas/*/denuncias/101/asignar', {
            statusCode: 200,
            body: { mensaje: 'Denuncia asignada correctamente.' },
        }).as('postAsignacion');

        cy.contains('tr', 'DEN-2026-002').within(() => {
            cy.contains('button', 'Seleccionar validador').click();
        });

        cy.get('.modal-content--validadores').contains('button', 'Juan Pérez').click();

        cy.contains('tr', 'DEN-2026-002').within(() => {
            cy.contains('button', 'Asignar').should('not.be.disabled').click();
        });

        cy.get('.modal-content').contains('button', 'Confirmar').click();

        cy.wait('@postAsignacion').then((interception) => {
            expect(interception.request.body.validadorId).to.equal(2);
        });
    });

    it('Debería listar los validadores disponibles con su carga activa en el selector', () => {
        cy.wait('@getDenunciasPendientes');
        cy.wait('@getValidadores');

        cy.contains('tr', 'DEN-2026-002').within(() => {
            cy.contains('button', 'Seleccionar validador').click();
        });

        cy.get('.modal-content--validadores').within(() => {
            cy.contains('2 de 2 validadores');
            cy.contains('button', 'Juan Pérez').contains('Carga: 2');
            cy.contains('button', 'Ana Rojas').contains('Carga: 0');
        });
    });
});