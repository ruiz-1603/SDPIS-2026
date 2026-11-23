// Botón para ordenar listas por fecha de forma ascendente o descendente.
// Se coloca a la par de los controles de búsqueda en los listados del sistema.
//
// props:
// - orden: 'asc' | 'desc'
// - alCambiar: (nuevoOrden) => void
// - etiqueta: string (opcional, por defecto 'Fecha')
// - className: string (opcional)
export function BotonOrdenFecha({ orden = 'asc', alCambiar, etiqueta = 'Fecha', className = '' }) {
    const esAscendente = orden === 'asc';

    const alternar = () => {
        if (alCambiar) {
            alCambiar(esAscendente ? 'desc' : 'asc');
        }
    };

    return (
        <button
            type="button"
            className={`btn-orden-fecha ${className}`.trim()}
            onClick={alternar}
            title={
                esAscendente
                    ? 'Orden ascendente (más antiguas primero). Clic para ordenar más recientes primero.'
                    : 'Orden descendente (más recientes primero). Clic para ordenar más antiguas primero.'
            }
            aria-label={`Ordenar por ${etiqueta}: ${esAscendente ? 'más antiguas primero' : 'más recientes primero'}`}
        >
            <svg
                className="btn-orden-fecha__icono"
                width="15"
                height="15"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                strokeWidth="2"
                strokeLinecap="round"
                strokeLinejoin="round"
                aria-hidden="true"
            >
                <rect x="3" y="4" width="18" height="18" rx="2" ry="2" />
                <line x1="16" y1="2" x2="16" y2="6" />
                <line x1="8" y1="2" x2="8" y2="6" />
                <line x1="3" y1="10" x2="21" y2="10" />
            </svg>
            <span className="btn-orden-fecha__texto">
                {etiqueta}: {esAscendente ? 'Más antiguas' : 'Más recientes'}
            </span>
            <span className="btn-orden-fecha__flecha" aria-hidden="true">
                {esAscendente ? '↑' : '↓'}
            </span>
        </button>
    );
}
