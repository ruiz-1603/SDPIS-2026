namespace SDPIS.Api.Services.Autenticacion;

// Datos del usuario autenticado que el proveedor devuelve (sin clave ni hash).
// FuncionarioId y AreaId vienen del funcionario al que pertenece el usuario.
public record DatosUsuario(
    long IdUsuario,
    string Identificacion,
    string Nombre,
    string Apellido1,
    string? Apellido2,
    string? CorreoInstitucional,
    string Rol,
    long FuncionarioId,
    long AreaId
);
