import { Fragment, useEffect, useId, useLayoutEffect, useMemo, useRef, useState } from 'react';
import { createPortal } from 'react-dom';
import './SelectorBuscable.css';

const normalizar = (texto) =>
    String(texto ?? '').normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase();

// Selector con busqueda en ventana modal (mismo patron que "Seleccionar
// validador" de Asignaciones): un boton muestra la opcion elegida y abre una
// ventana con buscador, conteo de resultados y la lista. Pensado para listas
// largas (83 areas, muchos funcionarios) donde un <select> no es practico.
//
// La ventana se dibuja en un portal (como los demas modales) para que no la
// recorten contenedores con scroll. Se cierra con "Cerrar", Escape o clic en
// el fondo.
//
// props:
//   valor        valor elegido ('' = todos)
//   opciones     [{ valor, texto, detalle? }]  (detalle se muestra como
//                etiqueta a la derecha y tambien se busca)
//   onCambiar    (valor: string) => void
//   textoTodos   texto de la opcion "todos" (p. ej. 'Todas las áreas'); si se
//                omite, no se ofrece esa opcion
//   etiqueta     nombre del campo (p. ej. 'Área'); arma el titulo
//                "Seleccionar área" y los textos accesibles
//   titulo       titulo de la ventana (opcional, reemplaza al anterior)
//   placeholder  texto del buscador
//   grupo        (en cada opcion, opcional) nombre de la "carpeta" a la que pertenece;
//                las opciones del mismo grupo deben venir contiguas. Cada carpeta se
//                puede expandir o contraer; al abrir solo queda expandida la de la
//                opcion elegida (o la primera), y al buscar se expanden todas las
//                que tengan coincidencias. Sin 'grupo' la lista es plana.
//   sobreModal   true cuando el selector vive dentro de otra ventana modal
//                propia de la pagina: la ventana de busqueda se dibuja encima
export function SelectorBuscable({
    valor, opciones, onCambiar, textoTodos, etiqueta = 'opción', titulo, placeholder = 'Buscar…', deshabilitado = false, sobreModal = false,
}) {
    const [abierto, setAbierto] = useState(false);
    const [busqueda, setBusqueda] = useState('');
    const [activo, setActivo] = useState(0);
    // Carpetas (grupos) contraidas en este momento.
    const [contraidos, setContraidos] = useState(() => new Set());
    const botonRef = useRef(null);
    const listaRef = useRef(null);
    const idTitulo = useId();
    const idLista = useId();

    const seleccionada = opciones.find((o) => String(o.valor) === String(valor ?? ''));

    const filtradas = useMemo(() => {
        const texto = normalizar(busqueda.trim());
        if (!texto) return opciones;
        return opciones.filter((o) => normalizar(o.texto).includes(texto) || normalizar(o.detalle).includes(texto));
    }, [opciones, busqueda]);

    // "Todos" solo se ofrece cuando no se esta buscando.
    const items = textoTodos && !busqueda.trim()
        ? [{ valor: '', texto: textoTodos }, ...filtradas]
        : filtradas;

    const buscando = Boolean(busqueda.trim());

    // Opciones que se pueden recorrer con el teclado: las de carpetas contraidas
    // quedan fuera (salvo al buscar, que expande todo).
    const navegables = items.filter((o) => !o.grupo || buscando || !contraidos.has(o.grupo));

    function abrir() {
        if (deshabilitado) return;
        setBusqueda('');
        // Solo queda expandida la carpeta de la opcion elegida (o la primera).
        const grupos = [...new Set(opciones.map((o) => o.grupo).filter(Boolean))];
        const grupoAbierto = seleccionada?.grupo ?? grupos[0];
        const cerrados = new Set(grupos.filter((g) => g !== grupoAbierto));
        setContraidos(cerrados);
        const lista = (textoTodos ? [{ valor: '' }, ...opciones] : opciones)
            .filter((o) => !o.grupo || !cerrados.has(o.grupo));
        const indiceActual = lista.findIndex((o) => String(o.valor) === String(valor ?? ''));
        setActivo(Math.max(0, indiceActual));
        setAbierto(true);
    }

    function alternarGrupo(grupo) {
        setContraidos((previos) => {
            const siguientes = new Set(previos);
            if (siguientes.has(grupo)) siguientes.delete(grupo);
            else siguientes.add(grupo);
            return siguientes;
        });
        setActivo(0);
    }

    function cerrar() {
        setAbierto(false);
        botonRef.current?.focus();
    }

    function elegir(opcion) {
        onCambiar(String(opcion.valor));
        cerrar();
    }

    // Escape cierra la ventana (desde cualquier elemento dentro de ella).
    useEffect(() => {
        if (!abierto) return undefined;
        function alPresionar(e) {
            if (e.key === 'Escape') cerrar();
        }
        document.addEventListener('keydown', alPresionar);
        return () => document.removeEventListener('keydown', alPresionar);
        // cerrar solo usa refs y setters estables.
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [abierto]);

    // Mantener visible la opcion activa al moverse con el teclado.
    useLayoutEffect(() => {
        if (!abierto) return;
        listaRef.current?.querySelector('[data-activo="true"]')?.scrollIntoView({ block: 'nearest' });
    }, [activo, abierto]);

    function alTeclear(e) {
        if (e.key === 'ArrowDown') {
            e.preventDefault();
            setActivo((i) => Math.min(i + 1, navegables.length - 1));
        } else if (e.key === 'ArrowUp') {
            e.preventDefault();
            setActivo((i) => Math.max(i - 1, 0));
        } else if (e.key === 'Enter') {
            e.preventDefault();
            if (navegables[activo]) elegir(navegables[activo]);
        }
    }

    const idOpcion = (i) => `${idLista}-op-${i}`;
    const etiquetaMinuscula = etiqueta.toLowerCase();

    return (
        <>
            <button
                ref={botonRef}
                type="button"
                className={`sb-boton${seleccionada ? ' sb-boton--elegido' : ''}`}
                onClick={abrir}
                aria-haspopup="dialog"
                aria-label={`${etiqueta}: ${seleccionada?.texto ?? textoTodos ?? 'sin seleccionar'}`}
                disabled={deshabilitado}
            >
                <span className="sb-boton__texto">
                    {seleccionada?.texto ?? textoTodos ?? `Seleccionar ${etiquetaMinuscula}`}
                </span>
                <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
                    <circle cx="11" cy="11" r="7" /><path d="m21 21-4.3-4.3" />
                </svg>
            </button>

            {abierto && createPortal(
                <div className={`sb-overlay${sobreModal ? ' sb-overlay--sobre-modal' : ''}`} onMouseDown={cerrar}>
                    <div
                        className="sb-modal"
                        role="dialog"
                        aria-modal="true"
                        aria-labelledby={idTitulo}
                        onMouseDown={(e) => e.stopPropagation()}
                    >
                        <h3 id={idTitulo} className="sb-modal__titulo">{titulo ?? `Seleccionar ${etiquetaMinuscula}`}</h3>

                        <div className="sb-modal__buscar">
                            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
                                <circle cx="11" cy="11" r="7" /><path d="m21 21-4.3-4.3" />
                            </svg>
                            <input
                                type="text"
                                autoFocus
                                role="combobox"
                                aria-expanded="true"
                                aria-controls={idLista}
                                aria-activedescendant={navegables[activo] ? idOpcion(activo) : undefined}
                                aria-label={`Buscar ${etiquetaMinuscula}`}
                                placeholder={placeholder}
                                value={busqueda}
                                onChange={(e) => { setBusqueda(e.target.value); setActivo(0); }}
                                onKeyDown={alTeclear}
                            />
                        </div>

                        <div className="sb-modal__resultado">
                            {filtradas.length} de {opciones.length}
                        </div>

                        <ul ref={listaRef} id={idLista} role="listbox" className="sb-modal__lista">
                            {items.length === 0 ? (
                                <li className="sb-modal__vacio">No hay coincidencias con la búsqueda.</li>
                            ) : (() => {
                                let indice = -1;
                                return items.map((o, posicion) => {
                                    const grupoNuevo = o.grupo && o.grupo !== items[posicion - 1]?.grupo;
                                    const grupoAbierto = !o.grupo || buscando || !contraidos.has(o.grupo);
                                    const total = grupoNuevo ? items.filter((x) => x.grupo === o.grupo).length : 0;
                                    const encabezado = grupoNuevo && (
                                        <li role="presentation" className="sb-grupo">
                                            <button
                                                type="button"
                                                className="sb-grupo__boton"
                                                aria-expanded={grupoAbierto}
                                                onClick={() => alternarGrupo(o.grupo)}
                                            >
                                                <svg width="16" height="16" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true" className="sb-grupo__carpeta">
                                                    <path d="M3 6a2 2 0 0 1 2-2h4.2a2 2 0 0 1 1.5.7L12 6h7a2 2 0 0 1 2 2v9a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z" />
                                                </svg>
                                                <span className="sb-grupo__nombre">{o.grupo}</span>
                                                <span className="sb-grupo__cuenta">{total}</span>
                                                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" aria-hidden="true" className={`sb-grupo__flecha${grupoAbierto ? ' sb-grupo__flecha--abierta' : ''}`}>
                                                    <path d="m6 9 6 6 6-6" />
                                                </svg>
                                            </button>
                                        </li>
                                    );
                                    if (!grupoAbierto) return <Fragment key={`${o.valor}`}>{encabezado}</Fragment>;

                                    indice += 1;
                                    const i = indice;
                                    const elegida = String(o.valor) === String(valor ?? '');
                                    return (
                                        <Fragment key={`${o.valor}`}>
                                            {encabezado}
                                            <li
                                                id={idOpcion(i)}
                                                role="option"
                                                aria-selected={elegida}
                                                data-activo={i === activo}
                                                className={`sb-opcion${i === activo ? ' sb-opcion--activa' : ''}${elegida ? ' sb-opcion--elegida' : ''}${o.valor === '' ? ' sb-opcion--todos' : ''}${o.grupo ? ' sb-opcion--en-carpeta' : ''}`}
                                                onClick={() => elegir(o)}
                                                onMouseEnter={() => setActivo(i)}
                                            >
                                                <span className="sb-opcion__texto">{o.texto}</span>
                                                {o.detalle && <span className="sb-opcion__detalle">{o.detalle}</span>}
                                                {elegida && <span className="sb-opcion__check" aria-hidden="true">✓</span>}
                                            </li>
                                        </Fragment>
                                    );
                                });
                            })()}
                        </ul>

                        <div className="sb-modal__pie">
                            <button type="button" className="sb-modal__cerrar" onClick={cerrar}>Cerrar</button>
                        </div>
                    </div>
                </div>,
                document.body
            )}
        </>
    );
}