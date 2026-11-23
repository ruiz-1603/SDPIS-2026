using SDPIS.Api.Repositories;

namespace SDPIS.Api.Services.Autorizacion;

// Permiso que se puede delegar a un funcionario sin darle el rol completo.
//   NivelDelegante = jefe_area    -> lo delega el jefe del area del
//                                    destinatario (o el superusuario).
//   NivelDelegante = superusuario -> solo lo delega un superusuario.
public sealed record PermisoDelegable(string Codigo, string NivelDelegante, string Grupo, string Descripcion);

// Permisos efectivos de un funcionario en este momento.
public sealed record ContextoPermisos(
    long FuncionarioId,
    string? Rol,
    bool Activo,
    long AreaId,
    bool PermiteAsignar,
    IReadOnlyList<string> Delegados,
    IReadOnlySet<string> Efectivos,
    string Alcance)
{
    public bool Tiene(string codigo) => Efectivos.Contains(codigo);
    public bool EsRol(string rol) => string.Equals(Rol, rol, StringComparison.Ordinal);
}

// HU-015: UNICO lugar donde se define que permite cada rol y que se puede
// delegar. Cambiar lo que ve un rol = editar este archivo (no la BD).
public static class CatalogoPermisos
{
    public static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> PorRol =
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal)
        {
            [Roles.Validador] = Conjunto(
                Permisos.VerDashboard,
                Permisos.VerDenunciasPropias,
                Permisos.RegistrarCriterios,
                Permisos.RegistrarSeguimiento,
                Permisos.SolicitarCierre),

            [Roles.JefeArea] = Conjunto(
                Permisos.VerDashboard,
                Permisos.VerDenunciasArea,
                Permisos.VerFuncionariosArea,
                Permisos.CrearFuncionariosArea,
                Permisos.DelegarPermisosArea,
                Permisos.AsignarDenuncias,
                Permisos.GestionarDisponibilidad,
                Permisos.RegistrarSeguimiento,
                Permisos.AprobarCierre,
                Permisos.VerBitacoraAdministrativa),

            [Roles.Superusuario] = Conjunto(
                Permisos.VerDashboard,
                Permisos.VerTodasAreas,
                Permisos.VerListaAreas,
                Permisos.VerDenunciasArea,
                Permisos.VerFuncionariosArea,
                Permisos.CrearFuncionariosArea,
                Permisos.DelegarPermisosArea,
                Permisos.AdministrarSistema,
                Permisos.AsignarDenuncias,
                Permisos.GestionarDisponibilidad,
                Permisos.RegistrarSeguimiento,
                Permisos.GestionarCriterios,
                Permisos.GestionarPlazos,
                Permisos.GestionarTerritorio,
                Permisos.ConfirmarUvc,
                Permisos.AprobarCierre,
                Permisos.VerBitacoraAdministrativa),
        };

    // NO delegables a proposito: ADMINISTRAR_SISTEMA, DELEGAR_PERMISOS_AREA
    // (evita cadenas de delegacion) y APROBAR_CIERRE (HU-017: jefatura y
    // superusuario).
    public static readonly IReadOnlyDictionary<string, PermisoDelegable> Delegables =
        new PermisoDelegable[]
        {
            new(Permisos.AsignarDenuncias, Roles.JefeArea, GruposPermiso.Operativo,
                "Asignar denuncias del área a los funcionarios."),
            new(Permisos.VerDenunciasArea, Roles.JefeArea, GruposPermiso.Supervision,
                "Ver todas las denuncias del area (no solo las propias)."),
            new(Permisos.VerFuncionariosArea, Roles.JefeArea, GruposPermiso.Supervision,
                "Ver la lista de funcionarios del area y sus denuncias."),
            new(Permisos.VerBitacoraAdministrativa, Roles.JefeArea, GruposPermiso.Supervision,
                "Consultar la bitacora administrativa del area."),
            new(Permisos.CrearFuncionariosArea, Roles.JefeArea, GruposPermiso.Administrativo,
                "Crear, editar y dar de baja funcionarios del área."),
            new(Permisos.GestionarDisponibilidad, Roles.JefeArea, GruposPermiso.Administrativo,
                "Gestionar la disponibilidad de los funcionarios."),
            new(Permisos.VerTodasAreas, Roles.Superusuario, GruposPermiso.Supervision,
                "Consultar denuncias y funcionarios de todas las areas."),
            new(Permisos.VerListaAreas, Roles.Superusuario, GruposPermiso.Supervision,
                "Ver la lista de areas."),
            new(Permisos.GestionarCriterios, Roles.Superusuario, GruposPermiso.Administrativo,
                "Gestionar reglas de criticidad (HU-013)."),
            new(Permisos.GestionarPlazos, Roles.Superusuario, GruposPermiso.Administrativo,
                "Gestionar plazos de atencion (HU-014)."),
            new(Permisos.GestionarTerritorio, Roles.Superusuario, GruposPermiso.Administrativo,
                "Gestionar areas y cobertura territorial (HU-031)."),
            new(Permisos.ConfirmarUvc, Roles.Superusuario, GruposPermiso.Operativo,
                "Confirmar atencion o devolver denuncias de la UVC."),
        }.ToDictionary(p => p.Codigo, StringComparer.Ordinal);

    public static bool EsRolValido(string? rol) => rol is not null && PorRol.ContainsKey(rol);

    public static bool RolIncluye(string? rol, string codigo)
        => rol is not null && PorRol.TryGetValue(rol, out var permisos) && permisos.Contains(codigo);

    public static IReadOnlySet<string> PermisosDeRol(string rol)
        => PorRol.TryGetValue(rol, out var permisos) ? permisos : Conjunto();

    // Rol + permite_asignar + delegados. Inactivo = sin permisos.
    // Los codigos delegados que ya no existan en el catalogo se ignoran.
    public static ContextoPermisos Construir(FuncionarioPermisosInfo info)
    {
        var efectivos = new HashSet<string>(StringComparer.Ordinal);

        if (info.Activo)
        {
            if (info.Rol is not null && PorRol.TryGetValue(info.Rol, out var deRol))
                efectivos.UnionWith(deRol);
            if (info.PermiteAsignar)
                efectivos.Add(Permisos.AsignarDenuncias);
            foreach (var codigo in info.Delegados)
                if (Delegables.ContainsKey(codigo))
                    efectivos.Add(codigo);
        }

        return new ContextoPermisos(info.FuncionarioId, info.Rol, info.Activo, info.AreaId,
            info.PermiteAsignar, info.Delegados, efectivos, CalcularAlcance(efectivos));
    }

    public static string CalcularAlcance(IReadOnlySet<string> efectivos)
    {
        if (efectivos.Contains(Permisos.VerTodasAreas)) return AlcanceDenuncias.Todas;
        if (efectivos.Contains(Permisos.VerDenunciasArea)) return AlcanceDenuncias.Area;
        if (efectivos.Contains(Permisos.VerDenunciasPropias)) return AlcanceDenuncias.Propias;
        return AlcanceDenuncias.Ninguna;
    }

    private static IReadOnlySet<string> Conjunto(params string[] codigos)
        => new HashSet<string>(codigos, StringComparer.Ordinal);
}
