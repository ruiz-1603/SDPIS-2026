import {
    irAlFormulario,
    completarUbicacionYHecho,
    irSiguiente,
    completarProductos,
    completarDenunciante,
    enviarDenuncia,
} from '../support/formularioDenunciaHelpers';

describe('HU-004: Registrar producto(s) denunciado(s) y su(s) motivo(s)', () => {
    beforeEach(() => {
        cy.intercept('POST', '**/api/denuncias').as('registrarDenuncia');
        irAlFormulario();
        completarUbicacionYHecho({
            provincia: 'San José',
            canton: 'Central',
            distrito: 'Carmen',
            establecimiento: 'Farmacia La Esperanza',
            descripcion: 'Descripción cronológica de la problemática, con más de diez caracteres.',
        });
        irSiguiente();
    });

    it('no permite avanzar sin nombre, descripción, tipo y al menos un motivo del producto', () => {
        irSiguiente();

        cy.contains('Datos del denunciante').should('not.exist');
        cy.contains('.error-msg', /nombre del producto/i);
        cy.contains('.error-msg', /descripción del producto/i);
        cy.contains('.error-msg', /tipo de producto/i);
        cy.contains('.error-msg', /motivo/i);
    });

    it('permite agregar varios bloques de producto, cada uno con su propio tipo y motivo(s) independientes', () => {
        completarProductos([
            {
                nombre: 'Jarabe ToseFin',
                descripcion: 'Frasco de 120ml sin lote visible.',
                tipoProducto: 'Medicamentos',
                motivos: [
                    'Sin Registro Sanitario',
                    'Etiquetado',
                ],
            },
            {
                nombre: 'Crema hidratante XY',
                descripcion: 'Tarro de 200g con publicidad de "cura todo".',
                tipoProducto: 'Cosméticos',
                motivos: ['Publicidad'],
            },
        ]);

        cy.get('.producto-block').should('have.length', 2);
        cy.get('.producto-block').eq(0).should('contain.text', 'Producto 1');
        cy.get('.producto-block').eq(1).should('contain.text', 'Producto 2');

        irSiguiente();
        completarDenunciante('anonima');
        irSiguiente();
        enviarDenuncia();

        cy.wait('@registrarDenuncia').then(({ request, response }) => {
            expect(response.statusCode).to.eq(201);

            const { productos } = request.body;
            expect(productos).to.have.length(2);

            expect(productos[0].nombreProductoTexto).to.eq('Jarabe ToseFin');
            expect(productos[1].nombreProductoTexto).to.eq('Crema hidratante XY');
            expect(productos[0].tipoProductoId).to.not.eq(productos[1].tipoProductoId);

            expect(productos[0].motivos).to.have.length(2);
            expect(productos[1].motivos).to.have.length(1);
            expect(productos[0].motivos).to.not.deep.eq(productos[1].motivos);
        });
    });

    it('permite quitar un producto adicional antes de enviar la denuncia', () => {
        completarProductos([
            {
                nombre: 'Jarabe ToseFin',
                descripcion: 'Frasco de 120ml sin lote visible.',
                tipoProducto: 'Medicamentos',
                motivos: ['Sin Registro Sanitario'],
            },
            {
                nombre: 'Crema hidratante XY',
                descripcion: 'Tarro de 200g con publicidad de "cura todo".',
                tipoProducto: 'Cosméticos',
                motivos: ['Publicidad'],
            },
        ]);
        cy.get('.producto-block').should('have.length', 2);

        cy.get('.producto-block').eq(1).contains('button', 'Quitar').click();

        cy.get('.producto-block').should('have.length', 1);
        cy.get('.producto-block').eq(0).contains('button', 'Quitar').should('not.exist');
    });
});