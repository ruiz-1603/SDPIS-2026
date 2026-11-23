using SDPIS.Api.Models.DTOs;

namespace SDPIS.Api.Services.Validation;

// Valida las entradas de la solicitud de cierre (HU-018). Las longitudes
// reflejan las columnas de solicitud_cierre.
public static class SolicitudCierreValidator
{
    public const int LongitudMaximaTexto = 2000;

    public static void ValidarSolicitud(long denunciaId, SolicitarCierreRequest? request)
    {
        var errores = new List<string>();

        if (denunciaId <= 0)
            errores.Add("Debe indicar una denuncia válida.");

        if (request is null)
        {
            errores.Add("Debe indicar la información de la solicitud de cierre.");
            throw new DenunciaValidationException(errores);
        }

        ValidarTexto(errores, request.ResolucionFinal, "la resolución final");
        ValidarTexto(errores, request.ObservacionesFinales, "las observaciones finales");
        ValidarTexto(errores, request.AccionesRealizadas, "la información de seguimiento y acciones realizadas");

        if (errores.Count > 0)
            throw new DenunciaValidationException(errores);
    }

    // campo va en minuscula y con articulo: "la resolución final".
    private static void ValidarTexto(List<string> errores, string? valor, string campo)
    {
        if (string.IsNullOrWhiteSpace(valor))
            errores.Add($"Debe indicar {campo}.");
        else if (valor.Trim().Length > LongitudMaximaTexto)
            errores.Add($"El texto de {campo} no puede superar {LongitudMaximaTexto} caracteres.");
    }
}
