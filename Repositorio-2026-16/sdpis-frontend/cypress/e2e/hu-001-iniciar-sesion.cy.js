describe('HU-001: Iniciar Sesión', () => {
    beforeEach(() => {
        cy.visit('/');
        // Abrir el modal de inicio de sesión
        cy.contains('button', 'Ingresar como funcionario').click();
    });

    it('debe mostrar el formulario de inicio de sesión al abrir la aplicación sin estar autenticado', () => {
        cy.get('.inicio-sesion').should('be.visible');
        cy.get('#itc-usuario').should('be.visible');
        cy.get('#itc-clave').should('be.visible');
        cy.contains('button', 'Iniciar sesión').should('be.visible');
    });

    it('debe mostrar un mensaje de error si las credenciales son inválidas', () => {
        cy.intercept('POST', '/api/autenticacion/iniciar-sesion', {
            statusCode: 401,
            body: { error: 'Credenciales invalidas. Verifique su usuario o clave.' }
        }).as('loginFallido');

        cy.get('#itc-usuario').type('usuario@invalido.com');
        cy.get('#itc-clave').type('ClaveIncorrecta');
        cy.contains('button', 'Iniciar sesión').click();

        cy.wait('@loginFallido');
        cy.get('.itc-alert').should('be.visible').and('contain', 'Credenciales invalidas');
    });

    it('debe iniciar sesión exitosamente y guardar el token en sessionStorage', () => {
        const fakeToken = 'fake-jwt-token-123';
        cy.intercept('POST', '/api/autenticacion/iniciar-sesion', {
            statusCode: 200,
            body: {
                token: fakeToken,
                expiraEn: '2027-01-01T00:00:00.000Z',
                usuario: {
                    idUsuario: 1,
                    identificacion: '123456789',
                    nombre: 'Test',
                    apellido1: 'Usuario',
                    correoInstitucional: 'test@misalud.go.cr',
                    rol: 'Funcionario'
                }
            }
        }).as('loginExitoso');

        // Mock para la ruta protegida que valida la sesión si la recarga (no estrictamente necesario en este flujo si interceptamos)
        cy.intercept('GET', '/api/autenticacion/sesion', {
            statusCode: 200,
            body: {
                idUsuario: 1,
                identificacion: '123456789',
                nombre: 'Test',
                apellido1: 'Usuario',
                correoInstitucional: 'test@misalud.go.cr',
                rol: 'Funcionario'
            }
        }).as('obtenerSesion');

        cy.get('#itc-usuario').type('test@misalud.go.cr');
        cy.get('#itc-clave').type('ClaveValida123!');
        cy.contains('button', 'Iniciar sesión').click();

        cy.wait('@loginExitoso');

        // El modal debe desaparecer
        cy.get('.inicio-sesion').should('not.exist');

        // Validar que el sessionStorage tiene la informacion de sesion
        cy.window().then((win) => {
            const sesionStr = win.sessionStorage.getItem('sdpis.sesion');
            expect(sesionStr).to.not.be.null;
            const sesion = JSON.parse(sesionStr);
            expect(sesion.token).to.eq(fakeToken);
            expect(sesion.usuario.nombre).to.eq('Test');
        });
    });
});
