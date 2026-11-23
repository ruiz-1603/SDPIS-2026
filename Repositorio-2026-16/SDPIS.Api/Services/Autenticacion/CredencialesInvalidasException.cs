namespace SDPIS.Api.Services.Autenticacion;

// HU-001: usuario inexistente o clave incorrecta. Mensaje siempre generico
// (no se filtra si el usuario existe). El middleware la traduce a 401.
public class CredencialesInvalidasException(string message) : Exception(message);