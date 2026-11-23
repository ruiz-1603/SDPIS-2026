import { entrarComo, irAlMenu, modal } from '../support/rolesPermisosHelpers';

// HU-014 Gestionar plazos de atención de denuncias.
// Pruebas funcionales del panel de configuración de plazos (permiso
// GESTIONAR_PLAZOS). El backend se simula con cy.intercept, por lo que solo
// se necesita el frontend corriendo (npm run dev). Las reglas de servidor
// (atomicidad, concurrencia, bitácora inmutable) están cubiertas por las
// pruebas unitarias de SDPIS.Api.Tests; aquí se simula su respuesta (400/403)
// para comprobar que la interfaz la comunica correctamente.

const PLAZOS = '**/api/plazos-atencion';

const plazosIniciales = () => ([
    { nivel: 'BAJO', puntajeMin: 1, puntajeMax: 2, plazoDias: 120 },
    { nivel: 'ALTO', puntajeMin: 5, puntajeMax: 6, plazoDias: 60 },
    { nivel: 'MEDIO', puntajeMin: 3, puntajeMax: 4, plazoDias: 90 },
]);

// Simula el backend: GET lista y PUT actualiza el nivel en memoria.
function simularPlazos({ rechazarPut } = {}) {
    const estado = plazosIniciales();

    cy.intercept('GET', PLAZOS, (req) => {
        req.reply({ statusCode: 200, body: estado });
    }).as('listarPlazos');

    cy.intercept('PUT', `${PLAZOS}/*`, (req) => {
        if (rechazarPut) {
            req.reply(rechazarPut);
            return;
        }
        const nivel = req.url.split('/').pop();
        const fila = estado.find((p) => p.nivel === nivel);
        fila.plazoDias = req.body.plazoDias;
        req.reply({ statusCode: 200, body: fila });
    }).as('actualizarPlazo');

    return estado;
}

function abrirPanel() {
    entrarComo('superusuario');
    irAlMenu('Plazos de atención');
    cy.wait('@listarPlazos');
}

function filaNivel(etiqueta) {
    return cy.contains('table.gu-tabla tbody tr', etiqueta);
}

function editarNivel(etiqueta, valor) {
    filaNivel(etiqueta).contains('button', 'Editar plazo').click();
    modal().should('be.visible');
    modal().find('input.gu-input--numero').clear();
    if (valor !== '') modal().find('input.gu-input--numero').type(valor);
    modal().contains('button', 'Guardar plazo').click();
}

describe('HU-014: Gestionar plazos de atención de denuncias', () => {

    // -----------------------------------------------------------------
    context('CA-01 Permiso GESTIONAR_PLAZOS: acceso al panel', () => {
        it('el superusuario ve la opción en el menú y abre el panel', () => {
            simularPlazos();
            entrarComo('superusuario');

            cy.contains('.menu-label', 'Plazos de atención').should('be.visible').click();
            cy.wait('@listarPlazos');
            cy.contains('th', 'Plazo de atención').should('be.visible');
            cy.get('.gu-restringido').should('not.exist');
        });

        it('el jefe de área no ve la opción en el menú', () => {
            simularPlazos();
            entrarComo('jefe');

            cy.contains('.menu-label', 'Plazos de atención').should('not.exist');
        });

        it('el funcionario no ve la opción en el menú', () => {
            simularPlazos();
            entrarComo('funcionario');

            cy.contains('.menu-label', 'Plazos de atención').should('not.exist');
        });

        it('si un jefe llega al panel por recarga, ve "Acceso restringido" y no consulta los plazos', () => {
            simularPlazos();
            entrarComo('jefe', { vista: 'institucional-plazos' });

            cy.contains('h2', 'Acceso restringido').should('be.visible');
            cy.get('table.gu-tabla').should('not.exist');
            cy.get('@listarPlazos.all').should('have.length', 0);
        });

        it('si un funcionario llega al panel por recarga, ve "Acceso restringido"', () => {
            simularPlazos();
            entrarComo('funcionario', { vista: 'institucional-plazos' });

            cy.contains('h2', 'Acceso restringido').should('be.visible');
            cy.get('@listarPlazos.all').should('have.length', 0);
        });
    });

    // -----------------------------------------------------------------
    context('CA-02 Consulta de los plazos vigentes', () => {
        it('muestra los plazos oficiales por defecto: alto 60, medio 90, bajo 120', () => {
            simularPlazos();
            abrirPanel();

            cy.contains('.gu-stat', 'Criticidad alta').find('dd').should('have.text', '60 días');
            cy.contains('.gu-stat', 'Criticidad media').find('dd').should('have.text', '90 días');
            cy.contains('.gu-stat', 'Criticidad baja').find('dd').should('have.text', '120 días');

            filaNivel('Alta').find('td strong').should('have.text', '60');
            filaNivel('Media').find('td strong').should('have.text', '90');
            filaNivel('Baja').find('td strong').should('have.text', '120');
        });

        it('ordena los niveles de alta a baja aunque el backend los devuelva desordenados', () => {
            simularPlazos();
            abrirPanel();

            cy.get('table.gu-tabla tbody tr').then(($filas) => {
                expect($filas).to.have.length(3);
                expect($filas.eq(0).text()).to.contain('Alta');
                expect($filas.eq(1).text()).to.contain('Media');
                expect($filas.eq(2).text()).to.contain('Baja');
            });
        });

        it('indica que el plazo corre desde la notificación y que no afecta fechas ya calculadas', () => {
            simularPlazos();
            abrirPanel();

            cy.contains('.gu-masthead__descripcion', 'desde la notificación al funcionario encargado');
            cy.contains('.gu-nota', 'Las fechas límite ya calculadas de denuncias existentes no se modifican');
        });

        it('muestra el error si no se pueden cargar los plazos', () => {
            cy.intercept('GET', PLAZOS, { statusCode: 500, body: { error: 'No se pudieron cargar los plazos.' } }).as('listarPlazos');
            entrarComo('superusuario');
            irAlMenu('Plazos de atención');
            cy.wait('@listarPlazos');

            cy.get('.gu-alerta--error').should('contain.text', 'No se pudieron cargar los plazos.');
        });
    });

    // -----------------------------------------------------------------
    context('CA-03 Modificación válida de un plazo', () => {
        it('el modal muestra el plazo actual y el rango permitido', () => {
            simularPlazos();
            abrirPanel();

            filaNivel('Media').contains('button', 'Editar plazo').click();
            modal().within(() => {
                cy.contains('Plazo de atención');
                cy.contains('Criticidad Media');
                cy.contains('Plazo actual: 90 días');
                cy.contains('entre 1 y 365');
                cy.get('input.gu-input--numero').should('have.value', '90');
            });
        });

        it('modifica el plazo de criticidad media y actualiza el panel', () => {
            simularPlazos();
            abrirPanel();

            editarNivel('Media', '100');

            // La identidad se toma de la sesión: solo se envía el nuevo valor.
            cy.wait('@actualizarPlazo').then(({ request }) => {
                expect(request.method).to.eq('PUT');
                expect(request.url).to.match(/\/plazos-atencion\/MEDIO$/);
                expect(request.body).to.deep.eq({ plazoDias: 100 });
            });
            cy.wait('@listarPlazos');

            cy.get('.gu-modal').should('not.exist');
            cy.get('.gu-toast').should('contain.text', 'Plazo de criticidad media: 100 días.');
            filaNivel('Media').find('td strong').should('have.text', '100');
            cy.contains('.gu-stat', 'Criticidad media').find('dd').should('have.text', '100 días');
        });

        it('modifica el plazo de criticidad alta a un valor menor', () => {
            simularPlazos();
            abrirPanel();

            editarNivel('Alta', '30');

            cy.wait('@actualizarPlazo').its('request.body').should('deep.eq', { plazoDias: 30 });
            cy.wait('@listarPlazos');
            filaNivel('Alta').find('td strong').should('have.text', '30');
        });

        it('el aviso de confirmación se puede cerrar', () => {
            simularPlazos();
            abrirPanel();

            editarNivel('Baja', '150');
            cy.wait('@actualizarPlazo');

            cy.get('.gu-toast').should('be.visible');
            cy.get('.gu-toast button[aria-label="Cerrar aviso"]').click();
            cy.get('.gu-toast').should('not.exist');
        });

        it('cancelar el modal no envía ningún cambio', () => {
            simularPlazos();
            abrirPanel();

            filaNivel('Baja').contains('button', 'Editar plazo').click();
            modal().find('input.gu-input--numero').clear().type('200');
            modal().contains('button', 'Cancelar').click();

            cy.get('.gu-modal').should('not.exist');
            cy.get('@actualizarPlazo.all').should('have.length', 0);
            filaNivel('Baja').find('td strong').should('have.text', '120');
        });
    });

    // -----------------------------------------------------------------
    context('CA-04 Valor igual al actual: sin cambios ni registro', () => {
        it('guardar el mismo valor cierra el modal y no llama al backend', () => {
            simularPlazos();
            abrirPanel();

            filaNivel('Alta').contains('button', 'Editar plazo').click();
            modal().contains('button', 'Guardar plazo').click();

            cy.get('.gu-modal').should('not.exist');
            cy.get('.gu-toast').should('not.exist');
            cy.get('@actualizarPlazo.all').should('have.length', 0);
        });
    });

    // -----------------------------------------------------------------
    context('CA-05 Validación del rango (entero entre 1 y 365)', () => {
        const MENSAJE_RANGO = 'Indique un número entero de días entre 1 y 365.';

        it('rechaza un plazo vacío', () => {
            simularPlazos();
            abrirPanel();

            editarNivel('Media', '');

            modal().find('.gu-alerta--error').should('contain.text', MENSAJE_RANGO);
            cy.get('@actualizarPlazo.all').should('have.length', 0);
        });

        it('rechaza un plazo de 0 días', () => {
            simularPlazos();
            abrirPanel();

            editarNivel('Media', '0');

            modal().find('.gu-alerta--error').should('contain.text', MENSAJE_RANGO);
            cy.get('@actualizarPlazo.all').should('have.length', 0);
        });

        it('el campo descarta letras, signos y decimales al escribir', () => {
            simularPlazos();
            abrirPanel();

            filaNivel('Baja').contains('button', 'Editar plazo').click();
            modal().find('input.gu-input--numero').clear().type('1a2-.5');
            modal().find('input.gu-input--numero').should('have.value', '125');
        });

        it('el campo limita la entrada a 3 dígitos', () => {
            simularPlazos();
            abrirPanel();

            filaNivel('Baja').contains('button', 'Editar plazo').click();
            modal().find('input.gu-input--numero').clear().type('9999');
            modal().find('input.gu-input--numero').should('have.value', '999');
            modal().contains('button', 'Guardar plazo').click();

            modal().find('.gu-alerta--error').should('contain.text', MENSAJE_RANGO);
            cy.get('@actualizarPlazo.all').should('have.length', 0);
        });

        it('rechaza 366 días (sobre el máximo)', () => {
            simularPlazos();
            abrirPanel();

            editarNivel('Baja', '366');

            modal().find('.gu-alerta--error').should('contain.text', MENSAJE_RANGO);
            cy.get('@actualizarPlazo.all').should('have.length', 0);
        });

        it('acepta el límite máximo de 365 días para criticidad baja', () => {
            simularPlazos();
            abrirPanel();

            editarNivel('Baja', '365');

            cy.wait('@actualizarPlazo').its('request.body').should('deep.eq', { plazoDias: 365 });
            cy.wait('@listarPlazos');
            filaNivel('Baja').find('td strong').should('have.text', '365');
        });

        it('acepta el límite mínimo de 1 día para criticidad alta', () => {
            simularPlazos();
            abrirPanel();

            editarNivel('Alta', '1');

            cy.wait('@actualizarPlazo').its('request.body').should('deep.eq', { plazoDias: 1 });
            cy.wait('@listarPlazos');
            filaNivel('Alta').find('td strong').should('have.text', '1');
        });
    });

    // -----------------------------------------------------------------
    context('CA-06 Regla de orden: alto debe ser menor que medio y bajo', () => {
        const MENSAJE_ORDEN = 'El plazo de criticidad alta debe ser menor que el de media y baja.';

        it('rechaza subir el plazo alto por encima del medio (95 > 90)', () => {
            simularPlazos();
            abrirPanel();

            editarNivel('Alta', '95');

            modal().find('.gu-alerta--error').should('contain.text', MENSAJE_ORDEN);
            cy.get('@actualizarPlazo.all').should('have.length', 0);
        });

        it('rechaza un plazo alto igual al medio (90)', () => {
            simularPlazos();
            abrirPanel();

            editarNivel('Alta', '90');

            modal().find('.gu-alerta--error').should('contain.text', MENSAJE_ORDEN);
            cy.get('@actualizarPlazo.all').should('have.length', 0);
        });

        it('rechaza bajar el plazo medio hasta el valor del alto (60)', () => {
            simularPlazos();
            abrirPanel();

            editarNivel('Media', '60');

            modal().find('.gu-alerta--error').should('contain.text', MENSAJE_ORDEN);
            cy.get('@actualizarPlazo.all').should('have.length', 0);
        });

        it('rechaza bajar el plazo bajo por debajo del alto (50 < 60)', () => {
            simularPlazos();
            abrirPanel();

            editarNivel('Baja', '50');

            modal().find('.gu-alerta--error').should('contain.text', MENSAJE_ORDEN);
            cy.get('@actualizarPlazo.all').should('have.length', 0);
        });

        it('acepta un plazo medio apenas mayor al alto (61)', () => {
            simularPlazos();
            abrirPanel();

            editarNivel('Media', '61');

            cy.wait('@actualizarPlazo').its('request.body').should('deep.eq', { plazoDias: 61 });
            cy.wait('@listarPlazos');
            filaNivel('Media').find('td strong').should('have.text', '61');
        });

        it('el modal queda abierto con el error y se puede corregir el valor', () => {
            simularPlazos();
            abrirPanel();

            editarNivel('Alta', '95');
            modal().find('.gu-alerta--error').should('be.visible');

            modal().find('input.gu-input--numero').clear().type('80');
            modal().contains('button', 'Guardar plazo').click();

            cy.wait('@actualizarPlazo').its('request.body').should('deep.eq', { plazoDias: 80 });
            cy.get('.gu-modal').should('not.exist');
        });
    });

    // -----------------------------------------------------------------
    context('CA-07 Validación en servidor: se muestra el error del backend', () => {
        it('muestra el rechazo por concurrencia (orden roto por otra modificación simultánea)', () => {
            simularPlazos({
                rechazarPut: {
                    statusCode: 400,
                    body: { errores: ['El plazo de criticidad alta debe ser menor que el de media y baja.'] },
                },
            });
            abrirPanel();

            // Pasa la validación local, pero el servidor lo rechaza.
            editarNivel('Media', '100');
            cy.wait('@actualizarPlazo');

            modal().find('.gu-alerta--error')
                .should('contain.text', 'El plazo de criticidad alta debe ser menor que el de media y baja.');
            filaNivel('Media').find('td strong').should('have.text', '90');
        });

        it('muestra el rechazo por falta de permiso (403) y no actualiza el panel', () => {
            simularPlazos({
                rechazarPut: { statusCode: 403, body: { error: 'No tiene permiso para gestionar plazos.' } },
            });
            abrirPanel();

            editarNivel('Baja', '110');
            cy.wait('@actualizarPlazo');

            modal().find('.gu-alerta--error').should('contain.text', 'No tiene permiso para gestionar plazos.');
            cy.get('.gu-toast').should('not.exist');
            filaNivel('Baja').find('td strong').should('have.text', '120');
        });

        it('si falla el guardado (500), el cambio no se aplica y se informa el error', () => {
            simularPlazos({
                rechazarPut: { statusCode: 500, body: { error: 'No se pudo guardar el plazo.' } },
            });
            abrirPanel();

            editarNivel('Media', '100');
            cy.wait('@actualizarPlazo');

            modal().find('.gu-alerta--error').should('contain.text', 'No se pudo guardar el plazo.');
            filaNivel('Media').find('td strong').should('have.text', '90');
        });
    });

    // -----------------------------------------------------------------
    context('CA-08 Autenticación y trazabilidad de la solicitud', () => {
        it('las solicitudes viajan con el token de la sesión y sin identidad en el cuerpo', () => {
            simularPlazos();
            abrirPanel();

            editarNivel('Media', '100');

            cy.wait('@actualizarPlazo').then(({ request }) => {
                expect(request.headers.authorization).to.eq('Bearer token-prueba-superusuario');
                expect(request.body).to.have.all.keys('plazoDias');
            });
        });
    });
});
