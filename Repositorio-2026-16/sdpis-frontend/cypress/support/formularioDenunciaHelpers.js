export function irAlFormulario() {
    cy.visit('/', {
        onBeforeLoad(win) {
            win.sessionStorage.clear();
            win.history.replaceState(null, '', win.location.href);
        },
    });
    cy.contains('Presentar una denuncia').click({ force: true });
    cy.contains('h1', 'Denuncia de Productos');
}

export function completarUbicacionYHecho(datos) {
    if (datos.provincia) cy.get('select').eq(0).select(datos.provincia);
    if (datos.canton) cy.get('select').eq(1).select(datos.canton);
    if (datos.distrito) cy.get('select').eq(2).select(datos.distrito);
    if (datos.establecimiento) cy.get('input[type="text"]').first().type(datos.establecimiento);
    if (datos.descripcion) cy.get('textarea').last().type(datos.descripcion);
}

export function irSiguiente() {
    cy.contains('button', 'Siguiente').click();
}

function llenarBloqueProducto(bloque, producto) {
    cy.wrap(bloque).within(() => {
        if (producto.nombre) cy.get('input[type="text"]').eq(0).type(producto.nombre);
        if (producto.descripcion) cy.get('textarea').eq(0).type(producto.descripcion);
        if (producto.tipoProducto) cy.get('select').eq(0).select(producto.tipoProducto);
    });
    (producto.motivos || []).forEach((motivo) => {
        cy.wrap(bloque).contains('.motivo-check-item', motivo).click();
    });
}

export function completarProductos(productos) {
    productos.forEach((producto, index) => {
        if (index > 0) {
            cy.contains('+ Agregar otro producto').click();
        }
        cy.get('.producto-block').eq(index).then((bloque) => llenarBloqueProducto(bloque, producto));
    });
}

export function completarDenunciante(tipoTramite, datos = {}) {
    const etiqueta = tipoTramite === 'confidencial' ? 'Denuncia confidencial' : 'Denuncia anónima';
    cy.contains(etiqueta).click({ force: true });
    if (tipoTramite === 'confidencial') {
        if (datos.nombreCompleto) cy.get('input[type="text"]').eq(0).type(datos.nombreCompleto);
        if (datos.numeroIdentificacion) cy.get('input[type="text"]').eq(1).type(datos.numeroIdentificacion);
    }
}

export function enviarDenuncia() {
    cy.contains('button', 'Registrar denuncia').click();
}

export function registrarDenunciaCompleta({
    provincia = 'San José',
    canton = 'Central',
    distrito = 'Carmen',
    establecimiento = 'Farmacia La Esperanza',
    descripcionHecho = 'El día 10 de setiembre compré el producto y noté que la etiqueta no incluía el número de registro sanitario.',
    productos = [
        {
            nombre: 'Jarabe para la tos ToseFin',
            descripcion: 'Frasco de 120ml, la etiqueta no muestra el número de lote.',
            tipoProducto: 'Medicamentos',
            motivos: ['Etiquetado'],
        },
    ],
    tipoTramite = 'confidencial',
    denunciante = { nombreCompleto: 'María Fernanda Rodríguez Solano', numeroIdentificacion: '1-2345-6789' },
} = {}) {
    irAlFormulario();
    completarUbicacionYHecho({ provincia, canton, distrito, establecimiento, descripcion: descripcionHecho });
    irSiguiente();
    completarProductos(productos);
    irSiguiente();
    completarDenunciante(tipoTramite, denunciante);
    irSiguiente();
    enviarDenuncia();
}