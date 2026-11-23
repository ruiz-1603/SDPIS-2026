using SDPIS.Api.Models.DTOs;

namespace SDPIS.Api.Services.Validation;

// HU-014: validacion de FORMA de la solicitud (400). La regla "alto menor
// que medio y bajo" depende de los demas niveles y vive en PlazosAtencionService.
public static class PlazosAtencionValidator
{
    public const int PlazoMinimoDias = 1;
    public const int PlazoMaximoDias = 365;

    public static readonly IReadOnlySet<string> Niveles =
        new HashSet<string>(StringComparer.Ordinal) { "ALTO", "MEDIO", "BAJO" };

    public static void ValidarActualizar(string? nivel, ActualizarPlazoRequest? r)
    {
        var errores = new List<string>();
        if (nivel is null || !Niveles.Contains(nivel.Trim().ToUpperInvariant()))
            errores.Add("El nivel de criticidad debe ser ALTO, MEDIO o BAJO.");
        if (r is null || r.PlazoDias < PlazoMinimoDias || r.PlazoDias > PlazoMaximoDias)
            errores.Add($"El plazo debe ser un numero entero de dias entre {PlazoMinimoDias} y {PlazoMaximoDias}.");
        if (errores.Count > 0)
            throw new DenunciaValidationException(errores);
    }
}
