import { entrarComo } from '../support/rolesPermisosHelpers';

describe('HU-028: Cerrar Sesión', () => {
    beforeEach(() => {
        // Sesión preexistente en sessionStorage. entrarComo registra los
        // intercepts (sesión, permisos, logout...) ANTES de visitar la app,
        // para que ninguna consulta inicial llegue al backend real.
        entrarComo('funcionario');
    });

    it('debe cerrar sesión, borrar sessionStorage y volver al login al hacer clic en cerrar sesión', () => {
        cy.intercept('POST', '**/api/autenticacion/logout', {
            statusCode: 200,
            body: { mensaje: 'Sesión finalizada.' }
        }).as('logout');

        // El modal de login no debe estar visible porque hay sesión
        cy.get('.inicio-sesion').should('not.exist');

        cy.contains('button', 'Cerrar sesión').click();

        cy.wait('@logout');

        // Verificar que se haya borrado la sesión local
        cy.window().then((win) => {
            expect(win.sessionStorage.getItem('sdpis.sesion')).to.be.null;
        });

        // Verificar que redirigió o mostró el modal de login nuevamente
        cy.get('.inicio-sesion').should('be.visible');
        cy.get('#itc-usuario').should('be.visible');
    });
});
