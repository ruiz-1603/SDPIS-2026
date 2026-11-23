using System.Text.RegularExpressions;
using SDPIS.Api.Models.DTOs;
using SDPIS.Api.Services.Autorizacion;

namespace SDPIS.Api.Services.Validation;

// HU-015: validacion de FORMA de las solicitudes (400). Las reglas de
// negocio (cupos, alcance por area, etc.) estan en GestionUsuariosService.
public static partial class GestionUsuariosValidator
{
    public const int TamanoPaginaMaximo = 100;
    public const int MaxJefesTope = 999; // limite tecnico de NUMBER(3)

    public static readonly IReadOnlySet<string> AccionesPermiso =
        new HashSet<string>(StringComparer.Ordinal) { "OTORGAR", "REVOCAR" };

    public static readonly IReadOnlySet<string> FiltrosDenuncia =
        new HashSet<string>(StringComparer.Ordinal) { "TODAS", "POR_ASIGNAR", "ASIGNADAS" };

    public static readonly IReadOnlySet<string> CategoriasBitacora =
        new HashSet<string>(StringComparer.Ordinal)
        {
            "FUNCIONARIOS", "PERMISOS", "CUPOS", "CONSULTA",
            "CRITERIOS", "PLAZOS", "GESTION_TERRITORIAL", "SESION"
        };

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex CorreoRegex();

    [GeneratedRegex(@"\p{L}")]
    private static partial Regex TieneLetraRegex();

    [GeneratedRegex(@"^[A-Za-z0-9-]{5,30}$")]
    private static partial Regex IdentificacionRegex();

    public static void ValidarCrear(CrearFuncionarioRequest? r)
    {
        var errores = new List<string>();
        if (r is null)
        {
            errores.Add("La solicitud esta vacia.");
            Lanzar(errores);
            return;
        }

        ValidarNombre(errores, r.Nombre, "El nombre");
        if (string.IsNullOrWhiteSpace(r.Correo) || r.Correo.Trim().Length > 150 || !CorreoRegex().IsMatch(r.Correo.Trim()))
            errores.Add("Debe indicar un correo institucional valido (maximo 150 caracteres).");
        if (!CatalogoPermisos.EsRolValido(r.Rol?.Trim()))
            errores.Add("El rol debe ser funcionario, jefe de área o superusuario.");
        if (r.AreaId <= 0)
            errores.Add("Debe indicar un area valida.");
        ValidarEspecialidad(errores, r.Especialidad);
        Lanzar(errores);
    }

    public static void ValidarActualizar(long id, ActualizarFuncionarioRequest? r)
    {
        var errores = new List<string>();
        if (id <= 0) errores.Add("Debe indicar un funcionario valido.");
        if (r is null || (r.Rol is null && r.Activo is null && r.Especialidad is null
                          && r.Nombre is null && r.Correo is null))
            errores.Add("Debe indicar al menos un cambio (nombre, correo, rol, estado o especialidad).");
        else
        {
            if (r.Rol is not null && r.Rol.Trim() is not (Roles.Validador or Roles.JefeArea))
                errores.Add("Aquí solo se permite el rol funcionario o jefe de área.");
            if (r.Nombre is not null)
                ValidarNombre(errores, r.Nombre, "El nombre");
            if (r.Correo is not null && (r.Correo.Trim().Length > 150 || !CorreoRegex().IsMatch(r.Correo.Trim())))
                errores.Add("Debe indicar un correo institucional valido (maximo 150 caracteres).");
            ValidarEspecialidad(errores, r.Especialidad);
        }
        Lanzar(errores);
    }

    public static void ValidarPermiso(long id, GestionarPermisoRequest? r)
    {
        var errores = new List<string>();
        if (id <= 0) errores.Add("Debe indicar un funcionario valido.");
        if (r is null || string.IsNullOrWhiteSpace(r.Codigo))
            errores.Add("Debe indicar el permiso.");
        else if (!CatalogoPermisos.Delegables.ContainsKey(r.Codigo.Trim()))
            errores.Add("El permiso indicado no es delegable.");
        if (r is null || !AccionesPermiso.Contains(r.Accion?.Trim().ToUpperInvariant() ?? ""))
            errores.Add("La accion debe ser OTORGAR o REVOCAR.");
        Lanzar(errores);
    }

    public static void ValidarAreaAdicional(long id, GestionarAreaAdicionalRequest? r)
    {
        var errores = new List<string>();
        if (id <= 0) errores.Add("Debe indicar un funcionario valido.");
        if (r is null || r.AreaId <= 0) errores.Add("Debe indicar el area.");
        if (r is null || !AccionesPermiso.Contains(r.Accion?.Trim().ToUpperInvariant() ?? ""))
            errores.Add("La accion debe ser OTORGAR o REVOCAR.");
        Lanzar(errores);
    }

    public static void ValidarMaxJefes(long areaId, ConfigurarMaxJefesRequest? r)
    {
        var errores = new List<string>();
        if (areaId <= 0) errores.Add("Debe indicar un area valida.");
        if (r is null || r.MaxJefes < 1 || r.MaxJefes > MaxJefesTope)
            errores.Add($"El cupo de jefes debe estar entre 1 y {MaxJefesTope}.");
        Lanzar(errores);
    }

    public static void ValidarFiltroDenuncias(string filtro)
    {
        if (!FiltrosDenuncia.Contains(filtro))
            Lanzar(new List<string> { "El filtro debe ser TODAS, POR_ASIGNAR o ASIGNADAS." });
    }

    public static void ValidarConsultaBitacora(string? categoria, int pagina, int tamano,
        DateTime? desde = null, DateTime? hasta = null, string? texto = null)
    {
        var errores = new List<string>();
        if (desde.HasValue && hasta.HasValue && desde.Value.Date > hasta.Value.Date)
            errores.Add("La fecha inicial no puede ser posterior a la fecha final.");
        if (texto is not null && texto.Trim().Length > 100)
            errores.Add("La busqueda no puede superar 100 caracteres.");
        if (categoria is not null && !CategoriasBitacora.Contains(categoria))
            errores.Add("La categoria indicada no es valida.");
        if (pagina < 1)
            errores.Add("La pagina debe ser mayor o igual a 1.");
        if (tamano < 1 || tamano > TamanoPaginaMaximo)
            errores.Add($"El tamano de pagina debe estar entre 1 y {TamanoPaginaMaximo}.");
        Lanzar(errores);
    }

    public static void ValidarRolFiltro(string? rol)
    {
        if (rol is not null && !CatalogoPermisos.EsRolValido(rol))
            Lanzar(new List<string> { "El rol indicado no es valido." });
    }

    // TEMPORAL (eliminar al integrar MSI).
    public static void ValidarCredencialTemporal(long id, CrearCredencialTemporalRequest? r)
    {
        var errores = new List<string>();
        if (id <= 0) errores.Add("Debe indicar un funcionario valido.");
        if (r is null)
        {
            errores.Add("La solicitud esta vacia.");
            Lanzar(errores);
            return;
        }

        if (string.IsNullOrWhiteSpace(r.Identificacion) || !IdentificacionRegex().IsMatch(r.Identificacion.Trim()))
            errores.Add("La identificacion debe tener entre 5 y 30 caracteres (letras, numeros o guion).");
        ValidarNombre(errores, r.Nombre, "El nombre");
        ValidarNombre(errores, r.Apellido1, "El primer apellido");
        if (r.Apellido2 is not null && r.Apellido2.Trim().Length > 150)
            errores.Add("El segundo apellido no puede superar 150 caracteres.");

        var clave = r.Clave ?? "";
        if (clave.Length < 12 || !clave.Any(char.IsUpper) || !clave.Any(char.IsLower)
            || !clave.Any(char.IsDigit) || clave.All(char.IsLetterOrDigit))
            errores.Add("La clave temporal debe tener al menos 12 caracteres, con mayuscula, minuscula, numero y simbolo.");
        Lanzar(errores);
    }

    private static void ValidarNombre(List<string> errores, string? valor, string campo)
    {
        if (string.IsNullOrWhiteSpace(valor) || !TieneLetraRegex().IsMatch(valor))
            errores.Add($"{campo} es obligatorio y debe contener letras.");
        else if (valor.Trim().Length > 150)
            errores.Add($"{campo} no puede superar 150 caracteres.");
    }

    private static void ValidarEspecialidad(List<string> errores, string? especialidad)
    {
        if (especialidad is not null && especialidad.Trim().Length > 100)
            errores.Add("La especialidad no puede superar 100 caracteres.");
    }

    private static void Lanzar(List<string> errores)
    {
        if (errores.Count > 0)
            throw new DenunciaValidationException(errores);
    }
}
