namespace SDPIS.Api.Models.DTOs;

// HU-001: respuesta de un inicio de sesion exitoso. Contiene el token JWT
// (guardado en sessionStorage por el frontend) y los datos publicos del
// usuario autenticado.
public record IniciarSesionResponse(
    string Token,
    string ExpiraEn,
    UsuarioAutenticadoDto Usuario
);

// Datos publicos del usuario (sin clave ni hash). FuncionarioId y AreaId son
// los del funcionario al que pertenece el usuario (los que usan las paginas).
public record UsuarioAutenticadoDto(
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