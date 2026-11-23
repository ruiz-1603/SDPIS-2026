namespace SDPIS.Api.Repositories;

// Revocacion de un token JWT (HU-028). Solo transporta el identificador del
// token (jti), la identificacion del usuario para trazabilidad y el motivo;
// nunca contiene el token mismo.
public record RegistroTokenRevocado(
    string Jti,
    string Identificacion,
    long? IdUsuarioTemporal,
    string Motivo        // CIERRE_MANUAL | CIERRE_INACTIVIDAD
);