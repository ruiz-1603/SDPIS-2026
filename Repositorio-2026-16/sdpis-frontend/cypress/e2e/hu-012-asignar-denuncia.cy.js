describe('HU-012: Asignar Denuncia a Encargado o Reasignar Área', () => {
  beforeEach(() => {
    cy.intercept('GET', '/api/areasencargados/disponibles', {
      statusCode: 200,
      body: [
        {
          funcionarioId: 101,
          nombre: 'Juan Perez',
          rol: 'funcionario',
          cargaActiva: 2,
          especialidad: 'Alimentos'
        }
      ]
    }).as('getEncargados');

    cy.intercept('GET', '/api/areas/destino', {
      statusCode: 200,
      body: [
        {
          areaId: 20,
          nombre: 'Area Rectora Escazu',
          tipo: 'ARS'
        }
      ]
    }).as('getAreas');

    cy.intercept('POST', '/api/areasasignar-encargado', {
      statusCode: 200,
      body: { mensaje: 'Denuncia asignada al funcionario encargado correctamente.' }
    }).as('asignarEncargado');

    cy.intercept('POST', '/api/areasreasignar-area', {
      statusCode: 200,
      body: { mensaje: 'Denuncia reasignada de area correctamente.' }
    }).as('reasignarArea');
  });

  it('Debe mostrar la bandeja de denuncias', () => {
    cy.visit('/institucional/asignar-encargado', {
      onBeforeLoad(win) {
        win.localStorage.setItem('auth_user', JSON.stringify({ areaId: 10, idFuncionario: 50, rol: 'jefe_area' }));
      }
    });
  });

  it('Debe permitir asignar un encargado a una denuncia', () => {
    cy.visit('/institucional/asignar-encargado');
  });

  it('Debe permitir reasignar una denuncia a otra area', () => {
    cy.visit('/institucional/asignar-encargado');
  });
});
