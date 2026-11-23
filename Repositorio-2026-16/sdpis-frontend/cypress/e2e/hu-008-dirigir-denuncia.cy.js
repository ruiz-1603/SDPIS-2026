describe('HU-008: Dirigir la denuncia a área', () => {
  beforeEach(() => {
    cy.intercept('POST', '**/api/denuncias', {
      statusCode: 200,
      body: { codigoSeguimiento: 'DEN-2026-HU008-TEST' }
    }).as('registrarDenuncia');
  });

  it('Debería enviar la denuncia con la ubicación correcta (Guanacaste, Abangares, Colorado) para que el sistema asigne el área', () => {
    cy.visit('/');

    cy.contains('Presentar una denuncia').click({ force: true });

    cy.contains('h1', 'Denuncia de Productos');

    cy.get('select').eq(0).select('Guanacaste');
    cy.get('select').eq(1).select('Abangares');
    cy.get('select').eq(2).select('Colorado');

    cy.get('input[type="text"]').first().type('Establecimiento Prueba HU-008');
    cy.get('textarea').last().type('Descripción cronológica de los hechos denunciados en la prueba automatizada HU-008.');

    cy.contains('button', 'Siguiente').click();

    cy.get('input[type="text"]').eq(0).type('Producto Falso');
    cy.get('textarea').eq(0).type('Descripción de un producto para prueba');
    cy.get('select').first().select('Alimentos');

    cy.contains('.motivo-check-item', 'Sin Registro Sanitario').click();

    cy.contains('button', 'Siguiente').click();

    cy.contains('Datos del denunciante');
    cy.contains('Denuncia anónima').click();
    cy.contains('button', 'Siguiente').click();

    cy.contains('Provincia').next('dd').should('contain', 'Guanacaste');
    cy.contains('Cantón').next('dd').should('contain', 'Abangares');
    cy.contains('Distrito').next('dd').should('contain', 'Colorado');

    cy.contains('button', 'Registrar denuncia').click();

    cy.wait('@registrarDenuncia').then((interception) => {
      const body = interception.request.body;

      expect(body.ubicacion).to.have.property('cantonId');
      expect(body.ubicacion).to.have.property('distritoId');

      expect(body.ubicacion.cantonId).to.be.a('number');
      expect(body.ubicacion.distritoId).to.be.a('number');
    });

    cy.contains('Denuncia registrada con éxito');
  });
});
