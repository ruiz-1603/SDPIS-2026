namespace SDPIS.Api.Services.Autenticacion;

// Resultado del proveedor: un descriptor para la API. Mensaje solo se
// incluye cuando es seguro mostrarlo al cliente (p. ej. cuenta bloqueada);
// para credenciales invalidas el mensaje siempre es el generico del API.
// IdUsuarioAuditoria identifica al usuario en los fallos cuando existe: solo
// se usa para bitacora_sesion, nunca se devuelve al cliente.
public record ResultadoAutenticacion(
    EstadoAutenticacion Estado,
    string? CodigoOrigen,
    string? Mensaje,
    DatosUsuario? DatosUsuario,
    long? IdUsuarioAuditoria = null
);
