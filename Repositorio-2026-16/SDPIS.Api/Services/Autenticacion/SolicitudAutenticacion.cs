namespace SDPIS.Api.Services.Autenticacion;

// Credenciales tal como llegan del cliente, listas para validar.
public record SolicitudAutenticacion(
    string CodigoUsuario,
    string Clave
);