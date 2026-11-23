import {
    irAlFormulario,
    completarUbicacionYHecho,
    irSiguiente,
    completarProductos,
    completarDenunciante,
    enviarDenuncia,
} from '../support/formularioDenunciaHelpers';

describe('HU-002: Recibir y registrar denuncias del formulario', () => {
    beforeEach(() => {
        cy.intercept('POST', '**/api/denuncias').as('registrarDenuncia');
        irAlFormulario();
    });

    it('no permite avanzar del paso 1 si falta la ubicación o la descripción del hecho', () => {
        irSiguiente();

        cy.contains('Producto(s) denunciado(s)').should('not.exist');

        cy.contains('.error-msg', 'Seleccione una provincia.');
        cy.contains('.error-msg', 'Seleccione un cantón.');
        cy.contains('.error-msg', /nombre del establecimiento/i);
        cy.contains('.error-msg', /problemática denunciada/i);
    });

    it('en denuncia anónima no solicita datos del denunciante y no entrega código de seguimiento', () => {
        completarUbicacionYHecho({
            provincia: 'San José',
            canton: 'Central',
            distrito: 'Carmen',
            establecimiento: 'Farmacia La Esperanza',
            descripcion: 'Descripción cronológica de la problemática, con más de diez caracteres.',
        });
        irSiguiente();

        completarProductos([
            {
                nombre: 'Jarabe ToseFin',
                descripcion: 'Frasco de 120ml sin lote visible.',
                tipoProducto: 'Medicamentos',
                motivos: ['Sin Registro Sanitario'],
            },
        ]);
        irSiguiente();


        completarDenunciante('anonima');

        cy.get('#denunciante-nombre').should('not.exist');
        cy.get('#denunciante-identificacion').should('not.exist');

        irSiguiente();
        enviarDenuncia();

        cy.wait('@registrarDenuncia').its('response.statusCode').should('eq', 201);

        cy.contains('h2', 'Denuncia registrada con éxito');
        cy.contains('no es posible entregarle un número de seguimiento');
        cy.get('.confirmacion-denuncia__numero-valor').should('not.exist');
    });

    it('en denuncia confidencial exige nombre e identificación, y los campos de contacto en blanco se envían como NULL', () => {
        completarUbicacionYHecho({
            provincia: 'San José',
            canton: 'Central',
            distrito: 'Carmen',
            establecimiento: 'Farmacia La Esperanza',
            descripcion: 'Descripción cronológica de la problemática, con más de diez caracteres.',
        });
        irSiguiente();

        completarProductos([
            {
                nombre: 'Jarabe ToseFin',
                descripcion: 'Frasco de 120ml sin lote visible.',
                tipoProducto: 'Medicamentos',
                motivos: ['Sin Registro Sanitario'],
            },
        ]);
        irSiguiente();

        cy.contains('Denuncia confidencial').click({ force: true });
        irSiguiente();
        cy.contains('.error-msg', /nombre completo/i);
        cy.contains('.error-msg', /identificación/i);

        cy.get('#denunciante-nombre').type('María Fernanda Rodríguez Solano');
        cy.get('#denunciante-identificacion').type('1-2345-6789');
        irSiguiente();
        enviarDenuncia();

        cy.wait('@registrarDenuncia').then(({ request, response }) => {
            expect(response.statusCode).to.eq(201);

            const { denunciante } = request.body.privacidad;
            expect(denunciante.correo).to.be.null;
            expect(denunciante.telefono).to.be.null;
            expect(denunciante.direccionDomicilio).to.be.null;
            expect(denunciante.nombre).to.eq('María Fernanda Rodríguez Solano');
            expect(denunciante.numeroIdentificacion).to.eq('1-2345-6789');
        });

        cy.contains('h2', 'Denuncia registrada con éxito');
    });
});