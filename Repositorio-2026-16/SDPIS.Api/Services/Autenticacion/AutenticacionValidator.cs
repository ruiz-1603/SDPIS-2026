using SDPIS.Api.Models.DTOs;

namespace SDPIS.Api.Services.Autenticacion;

public static class AutenticacionValidator
{
    private const int LongitudMaximaClave = 128;
    private const int LongitudMaximaCodigo = 150;

    // Validacion de forma: campos obligatorios y limites. La verificacion
    // de credenciales (validez) es responsabilidad del proveedor.
    public static void Validar(IniciarSesionRequest request)
    {
        var errores = new List<string>();

        var codigo = request?.CodigoUsuario?.Trim() ?? "";
        if (codigo.Length == 0)
            errores.Add("El usuario o correo institucional es obligatorio.");
        else if (codigo.Length > LongitudMaximaCodigo)
            errores.Add("El usuario o correo institucional supera la longitud maxima permitida.");

        var clave = request?.Clave ?? "";
        if (clave.Length == 0)
            errores.Add("La contrasena es obligatoria.");
        else if (clave.Length > LongitudMaximaClave)
            errores.Add("La contrasena supera la longitud maxima permitida.");

        if (errores.Count > 0)
            throw new AutenticacionValidationException(errores);
    }
}