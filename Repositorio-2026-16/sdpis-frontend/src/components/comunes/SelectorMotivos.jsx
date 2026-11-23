// Motivos estandarizados definidos en HU-004
export const MOTIVOS_DENUNCIA = [
    'Adulteración',
    'Calidad',
    'Etiquetado',
    'Falsificación',
    'Indicaciones no Aprobadas',
    'Intoxicación',
    'Irregularidades en el registro',
    'Producto vencido',
    'Publicidad',
    'Seguridad',
    'Sin Registro Sanitario',
];
export function SelectorMotivos({ motivosSeleccionados, onChange }) {
    function alternarMotivo(motivo) {
        const yaSeleccionado = motivosSeleccionados.includes(motivo);
        const nuevos = yaSeleccionado
            ? motivosSeleccionados.filter((m) => m !== motivo)
            : [...motivosSeleccionados, motivo];
        onChange(nuevos);
    }

    return (
        <div className="motivo-checklist">
            {MOTIVOS_DENUNCIA.map((motivo) => {
                const seleccionado = motivosSeleccionados.includes(motivo);
                return (
                    <div
                        key={motivo}
                        className={`motivo-check-item${seleccionado ? ' selected' : ''}`}
                        onClick={() => alternarMotivo(motivo)}
                    >
                        <div className="motivo-check-box">{seleccionado ? '✓' : ''}</div>
                        <div className="motivo-check-text">{motivo}</div>
                    </div>
                );
            })}
        </div>
    );
}