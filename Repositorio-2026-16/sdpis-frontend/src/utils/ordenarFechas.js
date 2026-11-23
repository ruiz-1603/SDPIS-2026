// Utilidades para parsear y ordenar fechas en listados del sistema.

const MESES = {
    ene: 0, feb: 1, mar: 2, abr: 3, may: 4, jun: 5,
    jul: 6, ago: 7, set: 8, sep: 8, oct: 9, nov: 10, dic: 11,
};

/**
 * Convierte cualquier fecha (ISO, objeto Date, o cadena en español como "03 oct. 2026")
 * a milisegundos desde epoch para comparaciones de ordenamiento.
 */
export function parsearFechaEspanol(cadena) {
    if (!cadena) return 0;
    if (cadena instanceof Date) return cadena.getTime();

    // Intentar Date.parse estándar (cadenas ISO o fechas estándar)
    const t = Date.parse(cadena);
    if (!Number.isNaN(t)) return t;

    // Formato común devuelto por backend: "03 oct. 2026", "4 oct 2026", etc.
    const partes = String(cadena).toLowerCase().replace(/\./g, '').trim().split(/\s+/);
    if (partes.length >= 3) {
        const dia = parseInt(partes[0], 10);
        const mesStr = partes[1].slice(0, 3);
        const anio = parseInt(partes[2], 10);
        const mes = MESES[mesStr];
        if (!Number.isNaN(dia) && mes !== undefined && !Number.isNaN(anio)) {
            return new Date(anio, mes, dia).getTime();
        }
    }

    return 0;
}

/**
 * Compara dos fechas según el orden especificado ('asc' o 'desc').
 */
export function compararFechas(fechaA, fechaB, orden = 'desc') {
    const tA = parsearFechaEspanol(fechaA);
    const tB = parsearFechaEspanol(fechaB);
    return orden === 'asc' ? tA - tB : tB - tA;
}
