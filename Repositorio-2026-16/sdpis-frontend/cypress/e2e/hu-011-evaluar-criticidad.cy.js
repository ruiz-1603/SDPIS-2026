const DENUNCIA = {
    idDenuncia: 9001,
    consecutivo: 'SDPIS-2026-9001',
    fechaRecepcion: '2026-09-10',
    nombreValidador: 'María Mora'
};

const CASOS = [
    { escenario: 'BAJO', dano: 'Bajo', especializado: 'No', puntaje: 1, nivel: 'BAJO', plazoDias: 120 },
    { escenario: 'BAJO', dano: 'Bajo', especializado: 'Si', puntaje: 2, nivel: 'BAJO', plazoDias: 120 },
    { escenario: 'MEDIO', dano: 'Medio', especializado: 'No', puntaje: 3, nivel: 'MEDIO', plazoDias: 90 },
    { escenario: 'MEDIO', dano: 'Medio', especializado: 'Si', puntaje: 4, nivel: 'MEDIO', plazoDias: 90 },
    { escenario: 'ALTO', dano: 'Alto', especializado: 'No', puntaje: 5, nivel: 'ALTO', plazoDias: 60 },
    { escenario: 'ALTO', dano: 'Alto', especializado: 'Si', puntaje: 6, nivel: 'ALTO', plazoDias: 60 }
];

function fechaLimiteIso(plazoDias) {
    const fecha = new Date();
    fecha.setHours(0, 0, 0, 0);
    fecha.setDate(fecha.getDate() + plazoDias);
    const pad = (n) => String(n).padStart(2, '0');
    return `${fecha.getFullYear()}-${pad(fecha.getMonth() + 1)}-${pad(fecha.getDate())}T00:00:00`;
}

function criteriosRegistrados({ dano, especializado, puntaje, nivel, plazoDias }) {
    return {
        idEvaluacion: 1,
        denunciaId: DENUNCIA.idDenuncia,
        funcionarioValidadorId: 100,
        fechaEvaluacion: new Date().toISOString(),
        puntajeTotal: puntaje,
        vinculadoComercioElectronico: 'N',
        danoPotencialSalud: dano,
        requiereCriterioEspecializado: especializado,
        observacionesTecnicas: null,
        nivelCriticidad: nivel,
        fechaLimite: nivel ? fechaLimiteIso(plazoDias) : null
    };
}

describe('HU-011: Evaluar criticidad de denuncias', () => {
    let criteriosActuales;

    beforeEach(() => {
        criteriosActuales = null;

        cy.intercept('GET', '**/api/areas/*/denuncias/asignadas', {
            statusCode: 200,
            body: [DENUNCIA]
        }).as('getAsignadas');

        cy.intercept('GET', `**/api/denuncias/${DENUNCIA.idDenuncia}/alerta-sanitaria-vigente`, {
            statusCode: 200,
            body: { tieneAlertaVigente: false, numerosAlerta: [] }
        }).as('getAlerta');

        cy.intercept('GET', `**/api/denuncias/${DENUNCIA.idDenuncia}/detalle`, {
            statusCode: 200,
            body: { idDenuncia: DENUNCIA.idDenuncia, consecutivo: DENUNCIA.consecutivo, fechaCreacion: null, estado: 'EN_TIEMPO', hecho: null, ubicacion: null, productos: [] }
        }).as('getDetalle');

        cy.intercept('GET', `**/api/denuncias/${DENUNCIA.idDenuncia}/criterios-tecnicos`, (req) => {
            if (criteriosActuales) {
                req.reply({ statusCode: 200, body: criteriosActuales });
            } else {
                req.reply({ statusCode: 204 });
            }
        }).as('getCriterios');

        cy.visit('/');

        cy.contains('Ingresar como funcionario').click({ force: true });
        cy.get('#usuarioMsi').should('be.visible').type('jefe');
        cy.get('#claveMsi').type('123');
        cy.contains('button', 'Iniciar sesión').click();

        cy.contains('.menu-label', 'Criticidad').click();
        cy.get('.ct-layout').should('be.visible');
        cy.wait('@getAsignadas');
    });

    function seleccionarDenuncia() {
        cy.get('button[aria-label^="Denuncia"]').click();
        cy.contains('.sb-opcion', DENUNCIA.consecutivo).click();
        cy.wait('@getCriterios');
        cy.get('#ct-form').should('be.visible');
    }

    function registrarCriterios(caso) {
        cy.intercept('POST', `**/api/denuncias/${DENUNCIA.idDenuncia}/criterios-tecnicos`, (req) => {
            criteriosActuales = criteriosRegistrados(caso);
            req.reply({
                statusCode: 201,
                body: {
                    idEvaluacion: 1,
                    puntajeTotal: caso.puntaje,
                    nivelCriticidad: caso.nivel
                }
            });
        }).as('postCriterios');

        cy.get('#ct-comercio').select('No');
        cy.get('#ct-dano').select(caso.dano);
        cy.get('#ct-especializado').select(caso.especializado);
        cy.contains('button', 'Registrar criterios').click();

        return cy.wait('@postCriterios');
    }

    ['BAJO', 'MEDIO', 'ALTO'].forEach((nivel) => {
        CASOS.filter((c) => c.escenario === nivel).forEach((caso) => {
            it(`Nivel ${nivel}: daño ${caso.dano} + especializado ${caso.especializado} (puntaje ${caso.puntaje}) muestra el badge y el plazo de ${caso.plazoDias} días`, () => {
                seleccionarDenuncia();

                registrarCriterios(caso).then((interception) => {
                    expect(interception.request.body.danoPotencialSalud).to.equal(caso.dano);
                    expect(interception.request.body.requiereCriterioEspecializado).to.equal(caso.especializado);
                    expect(interception.request.body.funcionarioValidadorId).to.equal(100);
                });

                cy.get('.toast.show.exito')
                    .should('contain.text', `Puntaje total: ${caso.puntaje}. Nivel de criticidad: ${caso.nivel}.`);

                cy.wait('@getCriterios');
                cy.get('.ct-puntaje-total strong').should('have.text', String(caso.puntaje));
                cy.get('.badge-criticidad')
                    .should('be.visible')
                    .and('have.text', caso.nivel)
                    .and('have.class', caso.nivel.toLowerCase());

                const fechaEsperada = new Date(fechaLimiteIso(caso.plazoDias)).toLocaleDateString('es-CR');
                cy.get('.ct-plazo-dias strong').should('have.text', String(caso.plazoDias));
                cy.contains('dt', 'Plazo máximo de días para atender la denuncia').should('be.visible');
                cy.contains('dt', 'Fecha máxima para finalizar la denuncia').should('be.visible');
                cy.get('.ct-fecha-limite')
                    .should('be.visible')
                    .and('have.text', fechaEsperada);
                cy.get('.ct-criticidad-pendiente').should('not.exist');
            });
        });
    });

    it('Evaluación incompleta: sin daño potencial no permite registrar ni muestra nivel ni plazo', () => {
        let seEnvio = false;
        cy.intercept('POST', `**/api/denuncias/${DENUNCIA.idDenuncia}/criterios-tecnicos`, () => {
            seEnvio = true;
        }).as('postCriterios');

        seleccionarDenuncia();

        cy.get('#ct-comercio').select('No');
        cy.get('#ct-especializado').select('No');
        cy.contains('button', 'Registrar criterios').click();

        cy.contains('Debe seleccionar un nivel de dano potencial a la salud.').should('be.visible');
        cy.then(() => expect(seEnvio).to.equal(false));

        cy.get('.badge-criticidad').should('not.exist');
        cy.get('.ct-fecha-limite').should('not.exist');
    });

    // Una denuncia siempre debe quedar con sus criterios registrados: ni
    // siquiera el superusuario puede eliminarlos, solo editarlos.
    it('Superusuario: no hay opción de eliminar criterios y la criticidad se mantiene', () => {
        criteriosActuales = criteriosRegistrados(CASOS[5]);

        cy.get('button[aria-label^="Denuncia"]').click();
        cy.contains('.sb-opcion', DENUNCIA.consecutivo).click();
        cy.wait('@getCriterios');

        cy.get('.ct-sesion-simulada select').select('superusuario');

        cy.get('.ct-criterio-fila').should('have.length', 2).each(($fila) => {
            cy.wrap($fila).contains('button', 'Editar').should('be.visible');
            cy.wrap($fila).contains('button', 'Eliminar').should('not.exist');
        });

        cy.get('.badge-criticidad').should('have.text', 'ALTO');
        cy.get('.ct-fecha-limite').should('be.visible');
    });
});