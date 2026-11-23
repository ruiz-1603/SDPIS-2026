import { SelectorMotivos } from '../../../components/comunes/SelectorMotivos';
import { AyudaContextual } from '../../../components/comunes/AyudaContextual';
import { AyudaCampo } from '../../../components/comunes/AyudaCampo';

const TIPOS_PRODUCTO = [
    'Alimentos',
    'Cannabis/Cáñamo',
    'Cosméticos',
    'Equipos y materiales biomédicos (EMB)',
    'Medicamentos',
    'Plaguicidas',
    'Productos Higiénicos',
    'Productos Naturales',
    'Productos Químicos',
    'Suplementos',
    'Tintas para Tatuaje',
    'Líquidos para vapeo',
    'Materias Primas',
];

export function BloqueProducto({ producto, index, onChange, onEliminar, puedeEliminar, errores = {} }) {
    function actualizarCampo(campo, valor) {
        onChange({ ...producto, [campo]: valor });
    }

    return (
        <div className="producto-block">
            <div className="producto-block-head">
                <span className="tag">Producto {index + 1}</span>
                {puedeEliminar && (
                    <button type="button" className="remove" onClick={onEliminar}>
                        Quitar
                    </button>
                )}
            </div>

            <div className="producto-block-body">
                <span className="subsection-lbl">Datos del producto</span>

                <div className={`field${errores.nombre ? ' error' : ''}`}>
                    <label>
                        Nombre del producto<span className="req">*</span>
                    </label>
                    <input
                        type="text"
                        value={producto.nombre}
                        onChange={(e) => actualizarCampo('nombre', e.target.value)}
                        aria-invalid={!!errores.nombre}
                    />
                    {!errores.nombre && <div className="help">Mínimo 2 caracteres.</div>}
                    {errores.nombre && <div className="error-msg">{errores.nombre}</div>}
                </div>

                <div className={`field${errores.descripcion ? ' error' : ''}`}>
                    <label htmlFor={`producto-${index}-descripcion`}>
                        Descripción<span className="req">*</span>
                    </label>
                    <AyudaCampo id={`producto-${index}-descripcion-ayuda`} titulo="Ayúdenos a identificar el producto">
                        <p>
                            Describa <strong>cómo es el producto</strong>: su forma o apariencia, color, tamaño,
                            empaque o etiqueta, y cualquier detalle que le haya llamado la atención.
                        </p>
                       
                    </AyudaCampo>
                    <textarea
                        id={`producto-${index}-descripcion`}
                        value={producto.descripcion}
                        onChange={(e) => actualizarCampo('descripcion', e.target.value)}
                        aria-invalid={!!errores.descripcion}
                        aria-describedby={`producto-${index}-descripcion-ayuda`}
                    />
                    {!errores.descripcion && <div className="help">Mínimo 5 caracteres.</div>}
                    {errores.descripcion && <div className="error-msg">{errores.descripcion}</div>}
                </div>

                <div className="row2">
                    <div className={`field${errores.tipoProducto ? ' error' : ''}`}>
                        <label>
                            <span className="field-label-row">
                                Tipo de producto<span className="req">*</span>
                                <AyudaContextual
                                    etiqueta="Ayuda sobre tipo de producto"
                                    texto="Selecciona la categoría que mejor describe el producto denunciado, según el catálogo del Ministerio de Salud. Por ejemplo, un termómetro digital o una silla de ruedas son 'Equipo y material biomédico'."
                                />
                            </span>
                        </label>
                        <select value={producto.tipoProducto} onChange={(e) => actualizarCampo('tipoProducto', e.target.value)}
                            aria-invalid={!!errores.tipoProducto}
                        >
                            <option value="">Seleccione...</option>
                            {TIPOS_PRODUCTO.map((tipo) => (
                                <option key={tipo} value={tipo}>
                                    {tipo}
                                </option>
                            ))}
                        </select>
                        {errores.tipoProducto && <div className="error-msg">{errores.tipoProducto}</div>}
                    </div>

                    <div className="field">
                        <label>
                            <span className="field-label-row">
                                Número de registro sanitario<span className="opt">(opcional)</span>
                                <AyudaContextual
                                    etiqueta="Ayuda sobre número de registro sanitario"
                                    texto="Es el código que el Ministerio de Salud asigna a un producto autorizado para su venta en Costa Rica. Si no lo tienes a mano o el producto no lo indica, puedes dejar este campo vacío."
                                />
                            </span>
                        </label>
                        <input
                            type="text"
                            value={producto.registroSanitario}
                            onChange={(e) => actualizarCampo('registroSanitario', e.target.value)}
                        />
                    </div>
                </div>

                <div className="row2">
                    <div className="field">
                        <label>
                            Marca<span className="opt">(opcional)</span>
                        </label>
                        <input type="text" value={producto.marca} onChange={(e) => actualizarCampo('marca', e.target.value)} />
                    </div>
                    <div className="field">
                        <label>
                            Número de lote<span className="opt">(opcional)</span>
                        </label>
                        <input type="text" value={producto.lote} onChange={(e) => actualizarCampo('lote', e.target.value)} />
                    </div>
                </div>

                <div className="row2">
                    <div className="field">
                        <label>
                            Fabricante o distribuidor<span className="opt">(opcional)</span>
                        </label>
                        <input
                            type="text"
                            value={producto.fabricante}
                            onChange={(e) => actualizarCampo('fabricante', e.target.value)}
                        />
                    </div>
                    <div className="field">
                        <label>
                            País de origen<span className="opt">(opcional)</span>
                        </label>
                        <input
                            type="text"
                            value={producto.paisOrigen}
                            onChange={(e) => actualizarCampo('paisOrigen', e.target.value)}
                        />
                    </div>
                </div>

                <div className="row2">
                    <div className="field">
                        <label>
                            Presentación<span className="opt">(opcional)</span>
                        </label>
                        <input
                            type="text"
                            value={producto.presentacion}
                            onChange={(e) => actualizarCampo('presentacion', e.target.value)}
                        />
                    </div>
                    <div className="field">
                        <label>
                            Fecha de compra<span className="opt">(opcional)</span>
                        </label>
                        <input
                            type="date"
                            value={producto.fechaCompra}
                            onChange={(e) => actualizarCampo('fechaCompra', e.target.value)}
                        />
                    </div>
                </div>

                <div className={`field${errores.motivos ? ' error' : ''}`}>
                    <span className="subsection-lbl">
                        Motivo(s) de denuncia<span className="req">*</span>
                    </span>
                    <SelectorMotivos
                        motivosSeleccionados={producto.motivos}
                        onChange={(motivos) => actualizarCampo('motivos', motivos)}
                    />
                    {errores.motivos && <div className="error-msg">{errores.motivos}</div>}
                </div>
            </div>
        </div>
    );
}