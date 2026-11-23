using System.Globalization;
using SDPIS.Api.Models.DTOs;

namespace SDPIS.Api.Services.Validation;

public static class DisponibilidadValidator
{
    private static readonly string[] MotivosValidos = { "vacaciones", "incapacidad", "ausencia" };

    public static DateOnly ValidarYNormalizar(RegistrarIndisponibilidadRequest request)
    {
        var errores = new List<string>();

        if (request.FuncionarioId <= 0)
            errores.Add("El identificador del funcionario es obligatorio.");

        if (string.IsNullOrWhiteSpace(request.Motivo) ||
            !MotivosValidos.Contains(request.Motivo.Trim().ToLowerInvariant()))
        {
            errores.Add("El motivo de indisponibilidad debe ser vacaciones, incapacidad o ausencia.");
        }

        if (!DateOnly.TryParseExact(request.FechaInicio, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var fechaInicio))
        {
            errores.Add("La fecha de inicio de indisponibilidad es obligatoria y debe tener formato yyyy-MM-dd.");
        }

        if (request.FechaFinEstimada is not null &&
            !DateOnly.TryParseExact(request.FechaFinEstimada, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out _))
        {
            errores.Add("La fecha estimada de disponibilidad debe tener formato yyyy-MM-dd.");
        }

        if (errores.Count > 0)
            throw new DenunciaValidationException(errores);

        var motivo = request.Motivo.Trim().ToLowerInvariant();

        if (request.FechaFinEstimada is { } fechaFinTxt &&
            DateOnly.TryParseExact(fechaFinTxt, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var fechaFin) &&
            fechaFin <= fechaInicio)
        {
            errores.Add("La fecha estimada de disponibilidad debe ser posterior a la fecha de inicio.");
        }

        if (errores.Count > 0)
            throw new DenunciaValidationException(errores);

        return fechaInicio;
    }

    public static DateOnly ValidarYNormalizarActualizacion(ActualizarIndisponibilidadRequest request)
    {
        var errores = new List<string>();

        if (request.IdPeriodo <= 0)
            errores.Add("El identificador del periodo es obligatorio.");

        if (request.FuncionarioId <= 0)
            errores.Add("El identificador del funcionario es obligatorio.");

        if (string.IsNullOrWhiteSpace(request.Motivo) ||
            !MotivosValidos.Contains(request.Motivo.Trim().ToLowerInvariant()))
        {
            errores.Add("El motivo de indisponibilidad debe ser vacaciones, incapacidad o ausencia.");
        }

        if (!DateOnly.TryParseExact(request.FechaInicio, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var fechaInicio))
        {
            errores.Add("La fecha de inicio de indisponibilidad es obligatoria y debe tener formato yyyy-MM-dd.");
        }

        if (request.FechaFinEstimada is not null &&
            !DateOnly.TryParseExact(request.FechaFinEstimada, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out _))
        {
            errores.Add("La fecha estimada de disponibilidad debe tener formato yyyy-MM-dd.");
        }

        if (errores.Count > 0)
            throw new DenunciaValidationException(errores);

        if (request.FechaFinEstimada is { } fechaFinTxt &&
            DateOnly.TryParseExact(fechaFinTxt, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var fechaFin) &&
            fechaFin <= fechaInicio)
        {
            errores.Add("La fecha estimada de disponibilidad debe ser posterior a la fecha de inicio.");
        }

        if (errores.Count > 0)
            throw new DenunciaValidationException(errores);

        return fechaInicio;
    }
}
