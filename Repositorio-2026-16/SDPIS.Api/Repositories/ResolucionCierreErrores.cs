using SDPIS.Api.Services.GestionUsuarios;
using SDPIS.Api.Services.Validation;

namespace SDPIS.Api.Repositories;

// HU-017: traduce los errores de negocio de sp_resolver_solicitud_cierre
// (-20170 a -20175) a las excepciones que ya convierte el middleware:
//   -20170 decision invalida / -20171 rechazo sin observaciones -> 400
//   -20172 solicitud inexistente                                -> 404
//   -20173 no es la jefatura del area de la solicitud           -> 403
//   -20174 ya resuelta / -20175 denuncia no esta EN_REVISION     -> 422
// Cualquier otro -20xxx se trata como hasta ahora (422).
// Separado del repositorio para poder probarlo sin Oracle.
public static class ResolucionCierreErrores
{
    public static Exception Mapear(int numeroOracle, string mensajeOracle, Exception inner)
    {
        var mensaje = Limpiar(mensajeOracle);
        return numeroOracle switch
        {
            20170 or 20171 => new DenunciaValidationException([mensaje]),
            20172 => new RecursoNoEncontradoException(mensaje),
            20173 => new PermisoDenegadoException(mensaje),
            20174 or 20175 => new ReglaNegocioException(mensaje),
            _ => new DenunciaBaseDatosException(mensaje, inner),
        };
    }

    // "ORA-20173: texto\nORA-06512: at ..." -> "texto".
    public static string Limpiar(string mensajeOracle)
    {
        var primeraLinea = mensajeOracle.Split('\n')[0].Trim();
        var separador = primeraLinea.IndexOf(": ", StringComparison.Ordinal);
        return primeraLinea.StartsWith("ORA-", StringComparison.Ordinal) && separador > 0
            ? primeraLinea[(separador + 2)..].Trim()
            : primeraLinea;
    }
}
