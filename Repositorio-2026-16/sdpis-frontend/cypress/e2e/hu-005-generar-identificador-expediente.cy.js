import { registrarDenunciaCompleta } from '../support/formularioDenunciaHelpers';

describe('HU-005: Generar identificación y expediente digital', () => {
    beforeEach(() => {
        cy.intercept('POST', '**/api/denuncias').as('registrarDenuncia');
    });

    it('genera un código de seguimiento único con formato XXXX-XXXX-XXXX al registrar la denuncia', () => {
        registrarDenunciaCompleta();

        cy.wait('@registrarDenuncia').then(({ response }) => {
            expect(response.statusCode).to.eq(201);
            expect(response.body).to.have.property('codigoSeguimiento');
            expect(response.body.codigoSeguimiento).to.match(/^[2-9A-HJ-NP-Z]{4}-[2-9A-HJ-NP-Z]{4}-[2-9A-HJ-NP-Z]{4}$/);
        });

        cy.contains('h2', 'Denuncia registrada con éxito');
        cy.get('.confirmacion-denuncia__numero-valor')
            .invoke('text')
            .should('match', /^[2-9A-HJ-NP-Z]{4}-[2-9A-HJ-NP-Z]{4}-[2-9A-HJ-NP-Z]{4}$/);
    });

    it('cada denuncia registrada recibe un código de seguimiento distinto (trazable e individual)', () => {
        let primerCodigo;

        registrarDenunciaCompleta({ establecimiento: 'Farmacia La Esperanza - caso 1' });
        cy.wait('@registrarDenuncia').then(({ response }) => {
            primerCodigo = response.body.codigoSeguimiento;
            expect(primerCodigo).to.be.a('string').and.not.be.empty;
        });

        registrarDenunciaCompleta({ establecimiento: 'Farmacia La Esperanza - caso 2' });
        cy.wait('@registrarDenuncia').then(({ response }) => {
            expect(response.body.codigoSeguimiento).to.not.eq(primerCodigo);
        });
    });

    it('registra la denuncia como una sola transacción: la respuesta 201 ya incluye el resultado completo', () => {
        registrarDenunciaCompleta();

        cy.wait('@registrarDenuncia').then((interception) => {
            expect(interception.response.statusCode).to.eq(201);
            expect(interception.response.body.codigoSeguimiento).to.exist;
        });

        cy.contains('h2', 'Denuncia registrada con éxito').should('be.visible');
    });
});