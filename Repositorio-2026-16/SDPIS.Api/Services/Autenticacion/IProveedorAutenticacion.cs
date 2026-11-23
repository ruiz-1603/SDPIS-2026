namespace SDPIS.Api.Services.Autenticacion;

// Punto de extension: cada origen de credenciales (Local o MSI) valida
// contra su propio sistema y devuelve un ResultadoAutenticacion estandar.
public interface IProveedorAutenticacion
{
    Task<ResultadoAutenticacion> AutenticarAsync(SolicitudAutenticacion solicitud, CancellationToken ct);
}