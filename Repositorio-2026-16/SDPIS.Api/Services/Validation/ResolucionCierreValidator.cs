using SDPIS.Api.Models.DTOs;

namespace SDPIS.Api.Services.Validation;

// Valida las entradas de la decision de la jefatura (HU-017). La longitud
// refleja decision_cierre.observaciones_jefatura.
public static class ResolucionCierreValidator
{
    public const int LongitudMaximaObservaciones = 2000;

    // Aprobar: las observaciones son opcionales.
    public static void ValidarAprobacion(long solicitudId, ResolverCierreRequest? request)
        => Validar(solicitudId, request?.Observaciones, obligatorias: false);

    // Rechazar (devolver para ajustes): las observaciones son obligatorias.
    public static void ValidarRechazo(long solicitudId, ResolverCierreRequest? request)
        => Validar(solicitudId, request?.Observaciones, obligatorias: true);

    private static void Validar(long solicitudId, string? observaciones, bool obligatorias)
    {
        var errores = new List<string>();

        if (solicitudId <= 0)
            errores.Add("Debe indicar una solicitud de cierre válida.");

        if (string.IsNullOrWhiteSpace(observaciones))
        {
            if (obligatorias)
                errores.Add("Debe indicar las observaciones de la jefatura para rechazar la resolución.");
        }
        else if (observaciones.Trim().Length > LongitudMaximaObservaciones)
        {
            errores.Add($"Las observaciones de la jefatura no pueden superar {LongitudMaximaObservaciones} caracteres.");
        }

        if (errores.Count > 0)
            throw new DenunciaValidationException(errores);
    }
}
