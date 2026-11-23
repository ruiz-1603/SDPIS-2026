const DENUNCIA_A = {
    idDenuncia: 9001,
    consecutivo: 'SDPIS-2026-9001',
    fechaRecepcion: '2026-09-10',
    nombreValidador: 'María Mora'
};
const DENUNCIA_B = {
    idDenuncia: 9002,
    consecutivo: 'SDPIS-2026-9002',
    fechaRecepcion: '2026-09-09',
    nombreValidador: 'María Mora'
};

const PUNTAJE_DANO = { Bajo: 1, Medio: 3, Alto: 5 };
const PUNTAJE_ESPECIALIZADO = { No: 0, Si: 1 };

function nivelPorPuntaje(puntaje) {
    if (puntaje >= 5) return { nivel: 'ALTO', plazoDias: 60 };
    if (puntaje >= 3) return { nivel: 'MEDIO', plazoDias: 90 };
    return { nivel: 'BAJO', plazoDias: 120 };
}

function fechaLimiteIso(plazoDias) {
    const fecha = new Date();
    fecha.setHours(0, 0, 0, 0);
    fecha.setDate(fecha.getDate() + plazoDias);
    const pad = (n) => String(n).padStart(2, '0');
    return `${fecha.getFullYear()}-${pad(fecha.getMonth() + 1)}-${pad(fecha.getDate())}T00:00:00`;
}

// Arma la evaluacion tal como la devuelve GET /criterios-tecnicos, calculando
// puntaje, nivel y fecha limite a partir de los valores del catalogo.
function armarEvaluacion(idDenuncia, { comercio, dano, especializado, observaciones, funcionarioId }) {
    const puntaje = PUNTAJE_DANO[dano] + PUNTAJE_ESPECIALIZADO[especializado];
    const { nivel, plazoDias } = nivelPorPuntaje(puntaje);
    return {
        idEvaluacion: idDenuncia,
        denunciaId: idDenuncia,
        funcionarioValidadorId: funcionarioId,
        fechaEvaluacion: new Date().toISOString(),
        puntajeTotal: puntaje,
        vinculadoComercioElectronico: comercio,
        danoPotencialSalud: dano,
        requiereCriterioEspecializado: especializado,
        observacionesTecnicas: observaciones || null,
        nivelCriticidad: nivel,
        fechaLimite: fechaLimiteIso(plazoDias)
    };
}

describe('HU-010: Registrar criterios técnicos de análisis', () => {
    // Estado simulado del backend por denuncia: null (204) hasta que se registra.
    let evaluaciones;

    beforeEach(() => {
        evaluaciones = {};

        cy.intercept('GET', '**/api/areas/*/denuncias/asignadas', {
            statusCode: 200,
            body: [DENUNCIA_A, DENUNCIA_B]
        }).as('getAsignadas');

        cy.intercept('GET', '**/api/denuncias/*/alerta-sanitaria-vigente', {
            statusCode: 200,
            body: { tieneAlertaVigente: false, numerosAlerta: [] }
        }).as('getAlerta');

        cy.intercept('GET', '**/api/denuncias/*/detalle', {
            statusCode: 200,
            body: { idDenuncia: 0, consecutivo: '', fechaCreacion: null, estado: 'EN_TIEMPO', hecho: null, ubicacion: null, productos: [] }
        }).as('getDetalle');

        cy.intercept('GET', '**/api/denuncias/*/criterios-tecnicos', (req) => {
            const id = Number(req.url.match(/denuncias\/(\d+)\//)[1]);
            if (evaluaciones[id]) {
                req.reply({ statusCode: 200, body: evaluaciones[id] });
            } else {
                req.reply({ statusCode: 204 });
            }
        }).as('getCriterios');

        // Registro inicial: el "backend" guarda la evaluacion y calcula puntaje/nivel
        // a partir de lo que realmente envia el frontend.
        cy.intercept('POST', '**/api/denuncias/*/criterios-tecnicos', (req) => {
            const id = Number(req.url.match(/denuncias\/(\d+)\//)[1]);
            const b = req.body;
            evaluaciones[id] = armarEvaluacion(id, {
                comercio: b.vinculadoComercioElectronico,
                dano: b.danoPotencialSalud,
                especializado: b.requiereCriterioEspecializado,
                observaciones: b.observacionesTecnicas,
                funcionarioId: b.funcionarioValidadorId
            });
            req.reply({
                statusCode: 201,
                body: {
                    idEvaluacion: id,
                    puntajeTotal: evaluaciones[id].puntajeTotal,
                    nivelCriticidad: evaluaciones[id].nivelCriticidad
                }
            });
        }).as('postCriterios');

        cy.visit('/');

        // simular el ingreso institucional (mismo flujo que hu-009)
        cy.contains('Ingresar como funcionario').click({ force: true });
        cy.get('#usuarioMsi').should('be.visible').type('jefe');
        cy.get('#claveMsi').type('123');
        cy.contains('button', 'Iniciar sesión').click();

        cy.contains('.menu-label', 'Criticidad').click();
        cy.get('.ct-layout').should('be.visible');
        cy.wait('@getAsignadas');
    });

    function seleccionarDenuncia(denuncia) {
        cy.get('button[aria-label^="Denuncia"]').click();
        cy.contains('.sb-opcion', denuncia.consecutivo).click();
        cy.wait('@getCriterios');
    }

    function llenarFormulario({ comercio = 'No', dano, especializado, observaciones }) {
        cy.get('#ct-comercio').select(comercio);
        cy.get('#ct-dano').select(dano);
        cy.get('#ct-especializado').select(especializado);
        if (observaciones) cy.get('#ct-observaciones').type(observaciones);
    }

    // ---------------------------------------------------------------------
    // Escenario 1 (AC #1 y #5): valores estandarizados y asociacion al expediente
    // ---------------------------------------------------------------------
    describe('Escenario 1: registro con valores estandarizados', () => {
        it('El formulario solo ofrece los valores definidos en el catálogo de criterios', () => {
            seleccionarDenuncia(DENUNCIA_A);

            cy.get('#ct-dano option').then(($o) => {
                expect([...$o].map((o) => o.textContent)).to.deep.equal(['Seleccione…', 'Bajo', 'Medio', 'Alto']);
            });
            cy.get('#ct-especializado option').then(($o) => {
                expect([...$o].map((o) => o.textContent)).to.deep.equal(['Seleccione…', 'No', 'Sí']);
            });
            cy.get('#ct-comercio option').then(($o) => {
                expect([...$o].map((o) => o.textContent)).to.deep.equal(['Seleccione…', 'Sí', 'No']);
            });
        });

        it('Los criterios registrados quedan guardados y visibles en el expediente de la denuncia', () => {
            seleccionarDenuncia(DENUNCIA_A);
            cy.contains('Aún no se han registrado criterios técnicos para esta denuncia.').should('be.visible');

            llenarFormulario({
                dano: 'Medio',
                especializado: 'No',
                observaciones: 'Producto con empaque dañado'
            });
            cy.contains('button', 'Registrar criterios').click();

            // Condiciones de entrada enviadas con los valores del catalogo
            cy.wait('@postCriterios').then(({ request }) => {
                expect(request.url).to.match(new RegExp(`/api/denuncias/${DENUNCIA_A.idDenuncia}/criterios-tecnicos$`));
                expect(request.body).to.include({
                    funcionarioValidadorId: 100,
                    vinculadoComercioElectronico: 'N',
                    danoPotencialSalud: 'Medio',
                    requiereCriterioEspecializado: 'No',
                    observacionesTecnicas: 'Producto con empaque dañado'
                });
            });
            cy.get('.toast.show.exito').should('contain.text', 'Criterios tecnicos registrados correctamente.');

            // Resultado esperado: el expediente muestra lo registrado
            cy.wait('@getCriterios');
            cy.get('#ct-form').should('not.exist');
            cy.contains('.ct-criterio-fila', 'Daño potencial a la salud').should('contain.text', 'Medio');
            cy.contains('.ct-criterio-fila', 'Criterio técnico especializado UVC').should('contain.text', 'No');
            cy.contains('.ct-metadata-resumen div', 'Observaciones técnicas').should('contain.text', 'Producto con empaque dañado');
            cy.contains('.ct-metadata-resumen div', 'Relación con comercio electrónico').should('contain.text', 'No');
            cy.get('.ct-puntaje-total strong').should('have.text', '3');
        });

        it('Los criterios siguen asociados al expediente al volver a abrir la denuncia', () => {
            seleccionarDenuncia(DENUNCIA_A);
            llenarFormulario({ dano: 'Bajo', especializado: 'Si' });
            cy.contains('button', 'Registrar criterios').click();
            cy.wait('@postCriterios');
            cy.get('.ct-puntaje-total strong').should('have.text', '2');

            // Ir a otra denuncia (sin criterios) y volver a la primera
            seleccionarDenuncia(DENUNCIA_B);
            cy.get('#ct-form').should('be.visible');
            cy.get('.ct-puntaje-total').should('not.exist');

            seleccionarDenuncia(DENUNCIA_A);
            cy.contains('.ct-criterio-fila', 'Daño potencial a la salud').should('contain.text', 'Bajo');
            cy.contains('.ct-criterio-fila', 'Criterio técnico especializado UVC').should('contain.text', 'Sí');
            cy.get('.ct-puntaje-total strong').should('have.text', '2');
        });

        it('No permite registrar si falta un criterio obligatorio', () => {
            let seEnvio = false;
            cy.intercept('POST', '**/api/denuncias/*/criterios-tecnicos', () => {
                seEnvio = true;
            }).as('postSinDatos');

            seleccionarDenuncia(DENUNCIA_A);
            cy.contains('button', 'Registrar criterios').click();

            cy.contains('Debe indicar si la denuncia esta vinculada a comercio electronico.').should('be.visible');
            cy.contains('Debe seleccionar un nivel de dano potencial a la salud.').should('be.visible');
            cy.contains('Debe indicar si requiere criterio tecnico especializado de UVC.').should('be.visible');
            cy.then(() => expect(seEnvio).to.equal(false));
        });
    });

    // ---------------------------------------------------------------------
    // Escenario 2 (AC #3 y #6): calculo automatico de criticidad
    // ---------------------------------------------------------------------
    describe('Escenario 2: cálculo automático de criticidad', () => {
        it('Daño Alto + Especializado Sí suma 6 puntos y muestra nivel ALTO', () => {
            seleccionarDenuncia(DENUNCIA_A);
            llenarFormulario({ dano: 'Alto', especializado: 'Si' });
            cy.contains('button', 'Registrar criterios').click();

            cy.wait('@postCriterios').then(({ request }) => {
                expect(request.body.danoPotencialSalud).to.equal('Alto');
                expect(request.body.requiereCriterioEspecializado).to.equal('Si');
            });
            cy.get('.toast.show.exito')
                .should('contain.text', 'Puntaje total: 6. Nivel de criticidad: ALTO.');

            cy.wait('@getCriterios');
            cy.get('.ct-puntaje-total strong').should('have.text', '6');
            cy.get('.badge-criticidad')
                .should('be.visible')
                .and('have.text', 'ALTO')
                .and('have.class', 'alto');
        });
    });

    // ---------------------------------------------------------------------
    // Escenario 3 (AC #4): modificacion solo para superusuario
    // ---------------------------------------------------------------------
    describe('Escenario 3: modificación de criterios ya registrados', () => {
        beforeEach(() => {
            // Evaluacion ya registrada por la validadora Maria Mora (Alto + Si)
            evaluaciones[DENUNCIA_A.idDenuncia] = armarEvaluacion(DENUNCIA_A.idDenuncia, {
                comercio: 'N', dano: 'Alto', especializado: 'Si', funcionarioId: 100
            });
            seleccionarDenuncia(DENUNCIA_A);
            cy.get('.ct-criterios-lista').should('be.visible');
        });

        it('Validador (no superusuario): no aparece la opción de editar, agregar ni eliminar', () => {
            cy.get('.ct-sesion-simulada select').should('have.value', 'validador-maria');

            cy.contains('button', 'Editar').should('not.exist');
            cy.contains('button', 'Agregar').should('not.exist');
            cy.contains('button', 'Eliminar').should('not.exist');
            cy.contains('Solo un superusuario puede modificar los criterios técnicos ya registrados.')
                .should('be.visible');
        });

        it('Jefe de área: ve el expediente en solo lectura, sin opciones de edición', () => {
            cy.get('.ct-sesion-simulada select').select('jefe-area');

            cy.contains('Vista de solo lectura: el Jefe de área puede consultar el expediente, no editarlo.')
                .should('be.visible');
            cy.contains('button', 'Editar').should('not.exist');
            cy.contains('button', 'Eliminar').should('not.exist');
            cy.contains('.ct-criterio-fila', 'Daño potencial a la salud').should('contain.text', 'Alto');
        });

        it('Superusuario: puede modificar un criterio y el cambio se envía y se refleja en el expediente', () => {
            cy.intercept('PUT', `**/api/denuncias/${DENUNCIA_A.idDenuncia}/criterios-tecnicos/dano-potencial-salud`, (req) => {
                evaluaciones[DENUNCIA_A.idDenuncia] = armarEvaluacion(DENUNCIA_A.idDenuncia, {
                    comercio: 'N', dano: req.body.valorSeleccionado, especializado: 'Si', funcionarioId: 100
                });
                req.reply({
                    statusCode: 200,
                    body: {
                        idEvaluacion: DENUNCIA_A.idDenuncia,
                        criterio: 'dano-potencial-salud',
                        valorSeleccionado: req.body.valorSeleccionado,
                        puntajeTotal: evaluaciones[DENUNCIA_A.idDenuncia].puntajeTotal
                    }
                });
            }).as('putCriterio');

            cy.get('.ct-sesion-simulada select').select('superusuario');
            cy.contains('.ct-criterio-fila', 'Daño potencial a la salud').within(() => {
                cy.contains('button', 'Editar').click();
                cy.get('select').select('Medio');
                // Editar un valor ya registrado exige justificación (mínimo 10 caracteres)
                cy.get('textarea').type('Ajuste por nueva información del expediente.');
                cy.contains('button', 'Guardar').click();
            });

            // Se envia quien modifica (funcionario y rol) junto con el nuevo valor
            cy.wait('@putCriterio').then(({ request }) => {
                expect(request.body).to.deep.equal({
                    rolFuncionarioSimulado: 'superusuario',
                    funcionarioId: 100,
                    valorSeleccionado: 'Medio',
                    justificacion: 'Ajuste por nueva información del expediente.'
                });
            });
            cy.get('.toast.show.exito').should('contain.text', '"Daño potencial a la salud" actualizado. Puntaje total: 4.');

            cy.wait('@getCriterios');
            cy.contains('.ct-criterio-fila', 'Daño potencial a la salud').should('contain.text', 'Medio');
            cy.get('.ct-puntaje-total strong').should('have.text', '4');
            cy.get('.badge-criticidad').should('have.text', 'MEDIO');
        });
    });

    // ---------------------------------------------------------------------
    // Escenario 5: el numero de oficio ya no se pide en los criterios tecnicos;
    // se registra al reasignar la denuncia a UVC (HU-012).
    // ---------------------------------------------------------------------
    describe('Escenario 5: sin número de oficio en los criterios técnicos', () => {
        it('Al marcar comercio electrónico en Sí no aparece mensaje ni campo de número de oficio', () => {
            seleccionarDenuncia(DENUNCIA_A);

            cy.get('#ct-comercio').select('Sí');
            cy.contains(/n[uú]mero de oficio/i).should('not.exist');
            cy.get('input[placeholder="Ej. OF-2026-001"]').should('not.exist');
        });

        it('Al registrar con comercio electrónico en Sí no se envía número de oficio ni se muestra recordatorio', () => {
            seleccionarDenuncia(DENUNCIA_A);
            llenarFormulario({ comercio: 'Sí', dano: 'Bajo', especializado: 'No' });
            cy.contains('button', 'Registrar criterios').click();

            cy.wait('@postCriterios').then(({ request }) => {
                expect(request.body.vinculadoComercioElectronico).to.equal('S');
                expect(request.body).to.not.have.property('numeroOficio');
            });
            cy.get('.toast.show.exito').should('contain.text', 'Criterios tecnicos registrados correctamente.');
            cy.get('.toast.show.aviso').should('not.exist');
        });
    });
});
