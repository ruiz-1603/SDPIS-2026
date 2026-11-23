import { useSesion } from '../../context/SesionContext';
import { VISTAS_INSTITUCIONALES, ROLES_ETIQUETA } from '../../config/permisos';
import logoBlancoSDPIS from '../../assets/images/logoBlanco_SDPIS.png';
import './BienvenidaInstitucional.css';

// Descripciones amigables para las tarjetas de acceso directo
const DESCRIPCIONES_VISTAS = {
    'institucional-asignaciones': 'Asigne las denuncias pendientes a funcionarios validadores disponibles.',
    'institucional-encargado': 'Asigne funcionarios encargados de seguimiento y confirme casos de la UVC.',
    institucional: 'Consulte y registre las novedades de disponibilidad laboral del personal.',
    'institucional-bitacora': 'Consulte el historial cronológico y la trazabilidad de las denuncias.',
    'institucional-criterios-tecnicos': 'Evalúe el nivel de riesgo y registre los criterios técnicos de análisis.',
    'institucional-gestion-usuarios': 'Administre funcionarios, áreas, cupos de jefatura y permisos internos.',
};

const ICONOS_VISTAS = {
    'institucional-asignaciones': '⇄',
    'institucional-encargado': '↦',
    institucional: '◷',
    'institucional-bitacora': '↬',
    'institucional-criterios-tecnicos': '△',
    'institucional-gestion-usuarios': '⚿',
};

export function BienvenidaInstitucional({ onNavegar }) {
    const { sesion, rolEfectivo, tieneAlgunPermiso } = useSesion();

    const usuario = sesion?.usuario;
    const nombreCompleto = usuario
        ? `${usuario.nombre} ${usuario.apellido1}${usuario.apellido2 ? ` ${usuario.apellido2}` : ''}`
        : 'Funcionario';

    const rol = rolEfectivo ?? usuario?.rol;
    const rolEtiqueta = rol ? (ROLES_ETIQUETA[rol] ?? rol) : 'Funcionario';

    // Obtener las secciones permitidas para este perfil
    const modalesDisponibles = Object.entries(VISTAS_INSTITUCIONALES)
        .filter(([clave, config]) => clave !== 'institucional-inicio' && tieneAlgunPermiso(config.permisos))
        .map(([clave, config]) => ({
            vista: clave,
            titulo: config.titulo,
            descripcion: DESCRIPCIONES_VISTAS[clave] ?? 'Acceda al módulo correspondiente para realizar sus gestiones.',
            icono: ICONOS_VISTAS[clave] ?? '▧',
        }));

    return (
        <div className="bienvenida-container">
            {/* Banner de bienvenida principal */}
            <div className="bienvenida-hero">
          
                <div className="bienvenida-hero__contenido">
                    <div className="bienvenida-hero__badge">
                        <span className="bienvenida-hero__badge-dot" />
                        <span>Perfil: {rolEtiqueta}</span>
                    </div>
                    <h1 className="bienvenida-hero__titulo">
                        Bienvenido(a), <span className="bienvenida-hero__nombre">{nombreCompleto}</span>
                    </h1>
                    <p className="bienvenida-hero__subtitulo">
                        Sistema de Denuncias de Productos de Interés Sanitario (SDPIS). Desde este portal
                        puede tramitar, supervisar y dar trazabilidad a los expedientes sanitarios bajo su responsabilidad.
                    </p>
                </div>
            </div>

            {/* Guía visual hacia el panel izquierdo */}
            <div className="bienvenida-guia" role="region" aria-label="Indicación de navegación">
                <div className="bienvenida-guia__icono" aria-hidden="true">
                    ←
                </div>
                <div className="bienvenida-guia__texto">
                    <h2>Seleccione una opción en el panel izquierdo</h2>
                    <p>
                        Utilice el menú de navegación situado a la izquierda para acceder a los módulos
                        habilitados según su rol y permisos institucionales.
                    </p>
                </div>
            </div>

           
        </div>
    );
}
