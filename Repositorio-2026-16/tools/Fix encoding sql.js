#!/usr/bin/env node
/**
 * fix-encoding-sql.js
 * --------------------
 * Convierte los literales de texto ('...') de un script .sql que
 * contengan tildes, ñ, ¿, ¡, etc. a la forma UNISTR('...\00XX...'),
 * que se guarda correctamente en Oracle sin importar el charset del
 * cliente que ejecute el script (SQL Developer, SQL*Plus, SQLcl,
 * Visual Studio/ODT, etc.).
 *
 * USO
 * ---
 *   Un archivo, sobrescribiéndolo:
 *       node fix-encoding-sql.js mi_script.sql
 *
 *   Un archivo, generando una copia nueva:
 *       node fix-encoding-sql.js mi_script.sql -o mi_script_fixed.sql
 *
 *   Varios archivos de una vez (sobrescribe cada uno):
 *       node fix-encoding-sql.js 05_seed_ars_y_ubicaciones.sql 10_sp_criterios_tecnicos.sql
 *
 *   Todos los .sql de una carpeta (pasando la CARPETA, no un patrón
 *   con "*"): el propio script busca los .sql adentro. Esto es a
 *   propósito: en Windows, "npm run ..." ejecuta con cmd.exe, que NO
 *   expande "*.sql" como sí lo hace una terminal bash/zsh — pasarle
 *   la carpeta evita depender de esa expansión:
 *       node fix-encoding-sql.js Scripts
 *
 * y correrlo con `npm run fix-encoding` antes de cada commit que
 * toque un script con texto en español.
 */

const fs = require("fs");
const path = require("path");

// Coincide con literales SQL 'texto' incluyendo '' (comilla escapada) adentro.
const LITERAL_RE = /'(?:''|[^'])*'/gs;

function convertirLiteral(match) {
    const contenido = match.slice(1, -1);

    // Si ya es puro ASCII, no hay nada que arreglar: se deja igual.
    let esAscii = true;
    for (const ch of contenido) {
        if (ch.codePointAt(0) > 127) {
            esAscii = false;
            break;
        }
    }
    if (esAscii) return match;

    let resultado = "";
    for (const ch of contenido) {
        const code = ch.codePointAt(0);
        if (code > 127) {
            resultado += "\\" + code.toString(16).toUpperCase().padStart(4, "0");
        } else {
            resultado += ch;
        }
    }
    return `UNISTR('${resultado}')`;
}

function convertirArchivo(ruta) {
    const contenido = fs.readFileSync(ruta, "utf8");

    let cantidad = 0;
    const nuevoContenido = contenido.replace(LITERAL_RE, (match) => {
        const contenidoLit = match.slice(1, -1);
        const tieneNoAscii = [...contenidoLit].some((ch) => ch.codePointAt(0) > 127);
        if (tieneNoAscii) cantidad++;
        return convertirLiteral(match);
    });

    return { nuevoContenido, cantidad };
}

function expandirCarpetas(rutas) {
    const resultado = [];
    for (const ruta of rutas) {
        if (fs.existsSync(ruta) && fs.statSync(ruta).isDirectory()) {
            const archivosSql = fs
                .readdirSync(ruta)
                .filter((nombre) => nombre.toLowerCase().endsWith(".sql"))
                .map((nombre) => path.join(ruta, nombre));
            if (archivosSql.length === 0) {
                console.error(`  (!) ${ruta} es una carpeta pero no tiene archivos .sql adentro.`);
            }
            resultado.push(...archivosSql);
        } else {
            resultado.push(ruta);
        }
    }
    return resultado;
}

function main() {
    const argvCrudo = process.argv.slice(2);
    if (argvCrudo.length === 0) {
        console.error("Uso: node fix-encoding-sql.js archivo1.sql [archivo2.sql ...] [-o salida.sql]");
        console.error("     node fix-encoding-sql.js CarpetaConScripts");
        process.exit(1);
    }

    let outputPath = null;
    const outIndex = argvCrudo.findIndex((a) => a === "-o" || a === "--output");
    if (outIndex !== -1) {
        outputPath = argvCrudo[outIndex + 1];
        argvCrudo.splice(outIndex, 2);
    }

    const argv = expandirCarpetas(argvCrudo);

    if (outputPath && argv.length > 1) {
        console.error("Error: -o/--output solo se puede usar con un único archivo de entrada.");
        process.exit(1);
    }

    let archivosTocados = 0;

    for (const ruta of argv) {
        if (!fs.existsSync(ruta)) {
            console.error(`  (!) No se encontró: ${ruta}`);
            continue;
        }

        const { nuevoContenido, cantidad } = convertirArchivo(ruta);
        const destino = outputPath || ruta;

        if (cantidad === 0) {
            console.log(`  ${ruta}: sin literales con tildes/ñ — sin cambios.`);
            if (outputPath) fs.writeFileSync(destino, nuevoContenido, "utf8");
            continue;
        }

        fs.writeFileSync(destino, nuevoContenido, "utf8");
        archivosTocados++;
        console.log(`  ${ruta} -> ${destino}: ${cantidad} literal(es) convertidos a UNISTR()`);
    }

    console.log(`\nListo. ${archivosTocados} archivo(s) con cambios.`);
}

main();