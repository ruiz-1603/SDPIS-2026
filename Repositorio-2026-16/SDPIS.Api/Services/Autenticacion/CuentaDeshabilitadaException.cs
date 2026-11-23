namespace SDPIS.Api.Services.Autenticacion;

// HU-001: clave correcta pero cuenta desactivada o rol no configurado.
// El middleware la traduce a 403 (se revelo la clave, es seguro informar).
public class CuentaDeshabilitadaException(string message) : Exception(message);