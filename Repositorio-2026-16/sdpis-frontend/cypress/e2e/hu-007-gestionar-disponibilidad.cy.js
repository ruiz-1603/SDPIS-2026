describe('HU-007: Gestionar disponibilidad de funcionarios', () => {
  beforeEach(() => {

    cy.intercept('GET', '**/api/disponibilidad/funcionarios', {
      statusCode: 200,
      body: [
        {
          funcionarioId: 1,
          nombre: 'Juan Pérez',
          rol: 'validador',
          disponible: true,
          cargaActiva: 2,
          tieneProximo: false,
          areaNombre: 'Área Técnica 1'
        },
        {
          funcionarioId: 2,
          nombre: 'María Gómez',
          rol: 'validador',
          disponible: false,
          motivoVigente: 'vacaciones',
          fechaFinVigente: '2026-10-15',
          cargaActiva: 0,
          tieneProximo: false,
          areaNombre: 'Área Técnica 1'
        }
      ]
    }).as('getFuncionarios');

    cy.intercept('GET', '**/api/disponibilidad/funcionarios/1', {
      statusCode: 200,
      body: {
        funcionarioId: 1,
        nombre: 'Juan Pérez',
        rol: 'validador',
        disponible: true,
        areaNombre: 'Área Técnica 1',
        historial: []
      }
    }).as('getDetalleFuncionario1');

    cy.visit('/');
    cy.contains('Ingresar como funcionario').click({ force: true });
    cy.get('#usuarioMsi').should('be.visible').type('jefe');
    cy.get('#claveMsi').type('123');
    cy.contains('button', 'Iniciar sesión').click();

    cy.contains('.menu-label', 'Disponibilidad de funcionarios').click();

    cy.get('.disp-layout').should('be.visible');
  });

  it('Debería mostrar la lista de funcionarios y sus KPI', () => {
    cy.wait('@getFuncionarios');
    cy.wait('@getDetalleFuncionario1');

      const kpi = (etiqueta) =>
          cy.contains('.disp-masthead__stat .etiqueta', etiqueta).siblings('.cifra');

      kpi(/^Funcionarios$/).should('have.text', '2');
      kpi('Disponibles ahora').should('have.text', '1');
      kpi('No disponibles').should('have.text', '1');
      kpi('Con periodo próximo').should('have.text', '0');

    // El selector de funcionario va arriba (SelectorBuscable): se abre y se
    // revisan las opciones con su estado.
    cy.get('button[aria-label^="Funcionario"]').click();
    cy.contains('.sb-opcion', 'Juan Pérez').should('be.visible').contains('Disponible');
    cy.contains('.sb-opcion', 'María Gómez').should('be.visible').contains('Vacaciones');
    cy.contains('button', 'Cerrar').click();
  });

  it('Debería permitir registrar un nuevo periodo de indisponibilidad', () => {
    cy.wait('@getFuncionarios');
    cy.wait('@getDetalleFuncionario1');

    cy.intercept('POST', '**/api/disponibilidad', {
      statusCode: 200,
      body: { idPeriodo: 100, funcionarioId: 1, motivo: 'incapacidad', fechaInicio: '2026-11-01', fechaFinEstimada: '2026-11-05', estado: 'programado' }
    }).as('postDisponibilidad');

    cy.contains('button', 'Registrar no disponibilidad').click();

    cy.get('#disp-motivo').select('incapacidad');
    cy.get('#disp-inicio').type('2026-11-01');
    cy.get('#disp-fin').type('2026-11-05');

    cy.contains('button', 'Guardar periodo').click();

    cy.wait('@postDisponibilidad').then((interception) => {
      expect(interception.request.body.funcionarioId).to.equal(1);
      expect(interception.request.body.motivo).to.equal('incapacidad');
      expect(interception.request.body.fechaInicio).to.equal('2026-11-01');
    });

    cy.get('.panel-lateral').should('not.have.class', 'visible');
  });

  it('Debería mostrar error de validación al intentar guardar sin fechas', () => {
    cy.wait('@getFuncionarios');
    cy.wait('@getDetalleFuncionario1');

    cy.contains('button', 'Registrar no disponibilidad').click();

    cy.contains('button', 'Guardar periodo').click();

    cy.get('#disp-motivo-error').should('contain', 'seleccionar');
    cy.get('#disp-inicio-error').should('contain', 'obligatoria');
  });
});
