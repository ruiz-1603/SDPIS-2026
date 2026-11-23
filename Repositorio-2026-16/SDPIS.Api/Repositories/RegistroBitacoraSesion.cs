namespace SDPIS.Api.Repositories;

// Evento de sesion a registrar en bitacora_sesion (HU-001 / HU-028).
// Nunca debe contener claves, hashes ni tokens.
public record RegistroBitacoraSesion(
    string CodigoUsuario,    // lo escrito en el login (cedula o correo); en cierres, la cedula
    long? IdUsuarioTemporal,
    string Resultado,        // EXITO | FALLO | DESHABILITADO | ROL_INVALIDO | CIERRE_MANUAL | CIERRE_INACTIVIDAD
    string? Motivo,
    string? DireccionIp,
    string? AgenteUsuario
);
