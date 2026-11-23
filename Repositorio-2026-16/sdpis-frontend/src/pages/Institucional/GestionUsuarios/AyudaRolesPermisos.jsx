import { useSesion } from '../../../context/SesionContext';
import { PERMISOS, PERMISOS_ETIQUETA } from '../../../config/permisos';

// HU-015: guia de uso de "Roles y permisos". Es una seccion critica de
// seguridad, por eso explica que hace cada accion, quien puede hacerla y que
// buenas practicas seguir. El contenido se adapta al rol: el jefe de area no
// ve lo que solo aplica al superusuario (areas, cupos, renuncia).
//
// Mantener alineado con las reglas del backend (GestionUsuariosService y
// CatalogoPermisos): si una regla cambia, actualizar este texto.

const DELEGABLES_JEFE = [
    PERMISOS.ASIGNAR_DENUNCIAS,
    PERMISOS.VER_DENUNCIAS_AREA,
    PERMISOS.VER_FUNCIONARIOS_AREA,
    PERMISOS.VER_BITACORA_ADMINISTRATIVA,
    PERMISOS.CREAR_FUNCIONARIOS_AREA,
    PERMISOS.GESTIONAR_DISPONIBILIDAD,
];

const DELEGABLES_SUPERUSUARIO = [
    PERMISOS.VER_TODAS_AREAS,
    PERMISOS.VER_LISTA_AREAS,
    PERMISOS.GESTIONAR_CRITERIOS,
    PERMISOS.GESTIONAR_PLAZOS,
    PERMISOS.GESTIONAR_TERRITORIO,
    PERMISOS.CONFIRMAR_UVC,
];

function Seccion({ titulo, abierta = false, children }) {
    return (
        <details className="gu-ayuda__seccion" open={abierta}>
            <summary>{titulo}</summary>
            <div className="gu-ayuda__contenido">{children}</div>
        </details>
    );
}

export function AyudaRolesPermisos() {
    const { tienePermiso } = useSesion();
    const esSuper = tienePermiso(PERMISOS.ADMINISTRAR_SISTEMA);

    return (
        <div className="gu-panel gu-ayuda">
            <div className="gu-ayuda__intro">
                <span className="gu-ayuda__icono" aria-hidden="true">?</span>
                <div>
                    <h3>Guía de roles y permisos</h3>
                    <p>
                        Esta sección controla <strong>quién puede ver y hacer qué</strong> dentro del SDPIS. Cada cambio
                        rige de inmediato y queda registrado en el historial administrativo. Ante la duda, no otorgue un
                        permiso: siempre puede otorgarlo después.
                    </p>
                </div>
            </div>

            <Seccion titulo="1. Roles del sistema y qué ve cada uno" abierta>
                <dl className="gu-ayuda__lista">
                    <dt>Funcionario</dt>
                    <dd>
                        Trabaja las denuncias que tiene asignadas: puede actuar como validador (registra criterios técnicos)
                        o como encargado (le da seguimiento y solicita el cierre). Solo ve sus propias denuncias.
                    </dd>
                    <dt>Jefe de área</dt>
                    <dd>
                        Ve todas las denuncias y los funcionarios de <strong>su área</strong>. Nunca tiene denuncias
                        asignadas: tiene denuncias <em>por asignar</em>. Conserva siempre el permiso de asignar.
                    </dd>
                    <dt>Superusuario</dt>
                    <dd>
                        Administra el sistema completo: todas las áreas, los jefes, los cupos y los criterios generales.
                        Solo puede haber 2 superusuarios activos al mismo tiempo.
                    </dd>
                </dl>
            </Seccion>

            <Seccion titulo="2. Permisos específicos (sin cambiar el rol)">
                <p>
                    Permiten que un funcionario haga una tarea puntual <strong>sin convertirlo en jefe</strong>. Sigue
                    siendo funcionario y conserva sus denuncias. Se otorgan y revocan desde el botón <strong>Permisos</strong>.
                </p>
                <h4>{esSuper ? 'Qué puede delegar el jefe de área (y usted)' : 'Qué puede delegar usted'}</h4>
                <ul className="gu-ayuda__chips">
                    {DELEGABLES_JEFE.map((p) => <li key={p}>{PERMISOS_ETIQUETA[p]}</li>)}
                </ul>
                {esSuper && (
                    <>
                        <h4>Qué solo puede delegar un superusuario</h4>
                        <ul className="gu-ayuda__chips gu-ayuda__chips--navy">
                            {DELEGABLES_SUPERUSUARIO.map((p) => <li key={p}>{PERMISOS_ETIQUETA[p]}</li>)}
                        </ul>
                    </>
                )}
                <ul>
                    <li>Quien recibe un permiso <strong>no puede volver a delegarlo</strong>.</li>
                    <li>
                        Nunca se delegan: administrar el sistema, delegar permisos ni aprobar cierres (es exclusivo de
                        la jefatura y del superusuario).
                    </li>
                    <li>El permiso de asignar del jefe de área es permanente: no se le puede revocar.</li>
                    {!esSuper && <li>Solo puede delegar a funcionarios de su propia área.</li>}
                </ul>
                <h4>Funcionarios en varias áreas</h4>
                <ul>
                    <li>
                        Un funcionario tiene un <strong>área base</strong>. Un superusuario puede otorgarle además
                        <strong> áreas adicionales</strong> (botón Permisos → Áreas de trabajo) para que atienda
                        denuncias de varias áreas a la vez.
                    </li>
                    <li>
                        {esSuper
                            ? 'Solo un superusuario otorga o revoca áreas adicionales, y solo a funcionarios (no a jefes ni superusuarios). No se puede quitar un área donde el funcionario aún tiene denuncias activas.'
                            : 'Solo un superusuario otorga o revoca áreas adicionales. Usted puede ver en su lista a los funcionarios de otras áreas que también trabajan en la suya, pero solo ve y asigna sus denuncias de su área, y los administra su jefe de área base.'}
                    </li>
                    <li>
                        Al dar de baja al funcionario o cambiarle el rol, sus áreas adicionales se retiran. Cada cambio
                        queda en el historial administrativo (categoría Permisos).
                    </li>
                </ul>
            </Seccion>

            <Seccion titulo="3. Registrar funcionarios">
                <ul>
                    {esSuper ? (
                        <>
                            <li>Puede registrar funcionarios, jefes de área y superusuarios en cualquier área.</li>
                            <li>Los jefes respetan el <strong>cupo de jefes</strong> del área (se edita en la pestaña Áreas).</li>
                            <li>Los superusuarios tienen un máximo fijo de 2 activos.</li>
                        </>
                    ) : (
                        <li>Puede registrar funcionarios en su área, sin cantidad máxima. No puede crear jefes ni superusuarios.</li>
                    )}
                    <li>
                        El <strong>correo institucional</strong> debe ser único y correcto: es el dato con que el MSI
                        enlazará a la persona cuando se integre.
                    </li>
                    <li>
                        El alta es interna del SDPIS. El acceso (usuario y clave) lo gestiona el MSI; la opción
                        &laquo;Crear credencial local&raquo; es solo provisional para pruebas.
                    </li>
                </ul>
            </Seccion>

            <Seccion titulo="4. Corregir datos, dar de baja y reactivar">
                <ul>
                    <li>
                        Un error de digitación en el nombre o el correo se corrige desde <strong>Editar</strong>; no cree un
                        registro nuevo. El correo solo se puede corregir hasta que se integre el MSI.
                    </li>
                    <li>
                        <strong>Los funcionarios no se eliminan</strong>: se dan de baja (Editar → desmarcar Activo). Así se
                        conserva el historial de lo que hicieron.
                    </li>
                    <li>No se puede dar de baja a alguien con denuncias activas: reasígnelas primero.</li>
                    <li>
                        Al dar de baja se le revocan todos los permisos delegados. Si luego se reactiva,
                        <strong> no los recupera</strong>: hay que otorgarlos de nuevo de forma explícita.
                    </li>
                    <li>No se puede dar de baja ni quitar el rol al <strong>último jefe activo</strong> de un área.</li>
                    <li>
                        Después de una baja, solicite al administrador del MSI que <strong>deshabilite la cuenta</strong> de
                        la persona para el SDPIS.
                    </li>
                </ul>
            </Seccion>

            {esSuper && (
                <Seccion titulo="5. Áreas y cupo de jefes">
                    <ul>
                        <li>Cada área tiene un máximo de jefes activos (2 por defecto) que usted puede ampliar o reducir.</li>
                        <li>El cupo no puede quedar por debajo de los jefes activos que ya tiene el área.</li>
                        <li>Las áreas rectoras (AR/ARS) y la UVC pueden tener jefes de área.</li>
                    </ul>
                </Seccion>
            )}

            {esSuper && (
                <Seccion titulo="6. Renuncia del superusuario">
                    <ul>
                        <li>
                            Usted puede renunciar a su rol para liberar una de las 2 plazas. Debe quedar al menos otro
                            superusuario activo.
                        </li>
                        <li>
                            Un superusuario normalmente no lleva denuncias. Si por algún motivo hubiera denuncias asignadas
                            a su nombre, el sistema le pedirá reasignarlas antes de renunciar, porque un jefe de área no
                            puede tener denuncias asignadas.
                        </li>
                        <li>
                            Al renunciar queda como <strong>jefe de área en renuncia</strong> (por defecto en su misma área).
                            No ocupa cupo de jefes y aparece marcado así en las listas.
                        </li>
                        <li>
                            El otro superusuario decide después si le <strong>retira el rol de jefe</strong> (Editar → rol
                            Funcionario) o si le <strong>da de baja</strong>. Puede encontrarlos con el filtro de rol
                            &laquo;Superusuarios en renuncia&raquo;.
                        </li>
                        <li>Nadie puede editar a otro superusuario activo; solo él mismo puede renunciar.</li>
                        <li>
                            <strong>Si un superusuario va a dejar la institución, debe renunciar antes de irse.</strong> Así
                            el otro superusuario puede darle de baja y la plaza queda libre para su reemplazo. Si se va sin
                            renunciar, su registro sigue ocupando una de las 2 plazas y solo puede corregirse directamente
                            en la base de datos.
                        </li>
                    </ul>
                </Seccion>
            )}

            <Seccion titulo={`${esSuper ? '7' : '5'}. Historial administrativo`}>
                <ul>
                    <li>
                        Registra altas, cambios, bajas, permisos{esSuper ? ', cupos' : ''}, inicios y cierres de sesión.
                        Es <strong>inmutable</strong>: nadie puede modificarlo ni borrarlo.
                    </li>
                    <li>
                        Use el filtro de periodo y la búsqueda por funcionario para encontrar un evento; por defecto muestra
                        los últimos 30 días.
                    </li>
                    <li>
                        Las consultas a listas se registran una vez al día por persona y se ocultan por defecto
                        (&laquo;Incluir consultas a listas&raquo;).
                    </li>
                    {esSuper && (
                        <li>
                            Solo usted ve el detalle técnico de las sesiones (equipo o dirección IP y usuario digitado en
                            intentos fallidos).
                        </li>
                    )}
                </ul>
            </Seccion>

            <Seccion titulo={`${esSuper ? '8' : '6'}. Buenas prácticas de seguridad`}>
                <ol className="gu-ayuda__practicas">
                    <li><strong>Mínimo privilegio:</strong> otorgue solo lo necesario para la tarea, nunca &laquo;por si acaso&raquo;.</li>
                    <li><strong>Revise periódicamente</strong> los permisos delegados de su {esSuper ? 'sistema' : 'área'} y revoque los que ya no se usan.</li>
                    <li><strong>Cambio de funciones:</strong> cuando alguien cambia de tarea o de área, ajuste sus permisos ese mismo día.</li>
                    <li>
                        <strong>Cuando alguien deja su puesto o la institución:</strong> dele de baja en el SDPIS ese mismo
                        día (Editar → desmarcar Activo) y solicite al administrador del MSI que deshabilite su cuenta.
                        {esSuper && ' Si quien se va es un superusuario, primero debe renunciar a su rol.'}
                    </li>
                    <li><strong>Credenciales:</strong> nunca comparta su usuario ni use la sesión de otra persona; cierre sesión al terminar.</li>
                    {esSuper && (
                        <li>
                            <strong>Intentos fallidos:</strong> revise en el historial (categoría Sesiones) los intentos
                            repetidos y repórtelos si no los reconoce.
                        </li>
                    )}
                </ol>
            </Seccion>
        </div>
    );
}