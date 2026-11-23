namespace SDPIS.Api.Services.Autenticacion;

// Nombres de los claims propios del JWT de sesion del SDPIS (HU-001).
// Son un contrato con el frontend y con otras historias (p. ej. permisos
// internos): NO renombrarlos sin avisar a quienes los consumen.
public static class ClaimsSesion
{
    public const string Identificacion = "identificacion";
    public const string Nombre = "nombre";
    public const string Apellido1 = "apellido1";
    public const string Apellido2 = "apellido2";
    public const string Correo = "correo";
    public const string Rol = "rol";
    public const string FuncionarioId = "funcionarioId";
    public const string AreaId = "areaId";
}
