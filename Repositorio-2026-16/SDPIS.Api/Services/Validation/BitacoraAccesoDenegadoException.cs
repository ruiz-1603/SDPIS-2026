namespace SDPIS.Api.Services.Validation;

// HU-016: el funcionario no tiene permiso institucional para consultar o
// comentar la bitacora de la denuncia. El middleware la traduce a 403.
public class BitacoraAccesoDenegadoException(string message) : Exception(message);
