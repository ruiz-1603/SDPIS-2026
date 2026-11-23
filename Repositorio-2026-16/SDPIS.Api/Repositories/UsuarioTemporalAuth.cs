namespace SDPIS.Api.Repositories;

// Huella del registro de usuario_temporal que necesita la autenticacion.
// Credenciales e identidad vienen de usuario_temporal; Rol y Activo vienen
// SOLO del funcionario (permisos internos del SDPIS).
// ClaveHash nunca sale del repositorio ni se incluye en respuestas.
public record UsuarioTemporalAuth(
    long IdUsuarioTemporal,
    string Identificacion,
    string Nombre,
    string Apellido1,
    string? Apellido2,
    string? CorreoInstitucional,
    string ClaveHash,
    long FuncionarioId,
    long AreaId,
    string? Rol,
    bool Activo
);
