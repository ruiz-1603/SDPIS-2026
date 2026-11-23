namespace SDPIS.Api.Services.Autenticacion;

// HU-001: solicitud de inicio de sesion mal formada (vacios, longitud).
// El middleware la traduce a 400 Bad Request.
public class AutenticacionValidationException(IEnumerable<string> errores)
    : Exception("La solicitud de inicio de sesion contiene datos invalidos.")
{
    public IReadOnlyList<string> Errores { get; } = errores.ToList();
}