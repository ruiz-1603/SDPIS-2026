namespace SDPIS.Api.Services.Autenticacion;

// Emite tokens JWT para sesiones del SDPIS (HU-001). La clave de firma vive
// en user-secrets o en variables de entorno, nunca en el repositorio.
public interface IServicioJwt
{
    string GenerarToken(DatosUsuario usuario);

    // Tiempo de validez del token esta configurado (Autenticacion:MinutosExpiracion).
    int MinutosExpiracion { get; }
}