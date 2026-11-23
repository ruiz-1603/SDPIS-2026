namespace SDPIS.Api.Services.Autenticacion;

// HU-028: confirmacion de cierre de sesion. Respuesta generica: nunca revela
// detalles internos del mecanismo de autenticacion ni del MSI.
public record ResultadoCierreSesion(string Mensaje);