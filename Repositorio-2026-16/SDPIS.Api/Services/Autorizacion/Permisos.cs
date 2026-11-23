namespace SDPIS.Api.Services.Autorizacion;

// HU-015: codigos de permisos internos del SDPIS. Son contrato con el
// frontend (menu y pantallas) y con funcionario_permiso.codigo_permiso:
// no renombrar sin avisar y sin migrar las filas existentes.
public static class Permisos
{
    public const string VerDashboard = "VER_DASHBOARD";
    public const string VerDenunciasPropias = "VER_DENUNCIAS_PROPIAS";
    public const string VerDenunciasArea = "VER_DENUNCIAS_AREA";
    public const string VerTodasAreas = "VER_TODAS_AREAS";
    public const string VerListaAreas = "VER_LISTA_AREAS";
    public const string VerFuncionariosArea = "VER_FUNCIONARIOS_AREA";
    public const string CrearFuncionariosArea = "CREAR_FUNCIONARIOS_AREA";
    public const string DelegarPermisosArea = "DELEGAR_PERMISOS_AREA";
    public const string AdministrarSistema = "ADMINISTRAR_SISTEMA";
    public const string AsignarDenuncias = "ASIGNAR_DENUNCIAS";
    public const string GestionarDisponibilidad = "GESTIONAR_DISPONIBILIDAD";
    public const string RegistrarCriterios = "REGISTRAR_CRITERIOS";
    public const string RegistrarSeguimiento = "REGISTRAR_SEGUIMIENTO";
    public const string SolicitarCierre = "SOLICITAR_CIERRE";
    public const string AprobarCierre = "APROBAR_CIERRE";
    public const string GestionarCriterios = "GESTIONAR_CRITERIOS";
    public const string GestionarPlazos = "GESTIONAR_PLAZOS";
    public const string GestionarTerritorio = "GESTIONAR_TERRITORIO";
    public const string ConfirmarUvc = "CONFIRMAR_UVC";
    public const string VerBitacoraAdministrativa = "VER_BITACORA_ADMINISTRATIVA";
}

// Roles SDPIS (coinciden con el CHECK de funcionario.rol).
public static class Roles
{
    public const string Validador = "validador";
    public const string JefeArea = "jefe_area";
    public const string Superusuario = "superusuario";
}

// Regla de visibilidad de denuncias (HU-015 / HU-016).
public static class AlcanceDenuncias
{
    public const string Todas = "TODAS";      // superusuario
    public const string Area = "AREA";        // jefe de area o delegado
    public const string Propias = "PROPIAS";  // validador
    public const string Ninguna = "NINGUNA";
}

// Agrupacion de permisos delegables en pantalla ("acceso de supervision",
// "acceso administrativo", "operativo").
public static class GruposPermiso
{
    public const string Supervision = "SUPERVISION";
    public const string Administrativo = "ADMINISTRATIVO";
    public const string Operativo = "OPERATIVO";
}

// Parametros fijos de la HU.
public static class ReglasGestion
{
    // HU-015: maximo de superusuarios activos simultaneos (fijo).
    public const int MaxSuperusuarios = 2;

    // Tipos de area que admiten jefe de area: rectoras (AR/ARS) y la UVC.
    public static readonly IReadOnlySet<string> TiposAreaConJefe =
        new HashSet<string>(StringComparer.Ordinal) { "AR", "ARS", "UVC" };

    // Marca del superusuario que renuncio a su rol: queda como jefe de area
    // "en renuncia". Se guarda como una fila de funcionario_permiso (sin
    // cambiar la BD); NO es un permiso (CatalogoPermisos la ignora).
    // Efectos: no ocupa cupo de jefes y se muestra aparte para una
    // posterior baja o retiro del rol. Se borra al quitarle el rol de jefe
    // o al darlo de baja. fecha_otorgamiento = fecha de la renuncia.
    public const string MarcaRenunciaSuperusuario = "RENUNCIA_SUPERUSUARIO";
}
