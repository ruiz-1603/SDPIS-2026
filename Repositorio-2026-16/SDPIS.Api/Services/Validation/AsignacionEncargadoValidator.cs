using SDPIS.Api.Models.DTOs;

namespace SDPIS.Api.Services.Validation;

// Valida presencia/formato básico (HU-012). Las reglas de negocio
// de disponibilidad (HU-007), rol del encargado y jefe de área nunca
// encargado las valida el servicio contra la base de datos y los
// SP sp_asignar_funcionario_encargado / sp_reasignar_denuncia_area,
// para no duplicar aquí reglas que ya viven en el SQL.
public static class AsignacionEncargadoValidator
{
    public static void Validar(AsignarEncargadoRequest request)
    {
        var errores = new List<string>();

        if (request.EncargadoId <= 0)
            errores.Add("Debe indicar el funcionario encargado a asignar.");

        if (request.JefeAreaId <= 0)
            errores.Add("Debe indicar el jefe de area que ejecuta la asignacion.");

        if (request.Motivo is { Length: > 500 })
            errores.Add("El motivo no puede superar los 500 caracteres.");

        if (errores.Count > 0)
            throw new DenunciaValidationException(errores);
    }

    public static void Validar(ReasignarAreaRequest request)
    {
        var errores = new List<string>();

        if (request.AreaNuevaId <= 0)
            errores.Add("Debe indicar el area a la que se reasigna la denuncia.");

        if (request.JefeAreaId <= 0)
            errores.Add("Debe indicar el jefe de area que ejecuta la reasignacion.");

        if (request.Motivo is { Length: > 500 })
            errores.Add("El motivo no puede superar los 500 caracteres.");

        if (errores.Count > 0)
            throw new DenunciaValidationException(errores);
    }
}