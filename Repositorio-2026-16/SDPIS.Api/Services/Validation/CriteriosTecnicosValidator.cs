using SDPIS.Api.Models.DTOs;

namespace SDPIS.Api.Services.Validation;

// Valida presencia/formato basico (HU-010). Los valores de dano_potencial_salud
// y requiere_criterio_especializado se validan contra el catalogo estandarizado
// (criterio_valor) a nivel de base de datos, via la FK de respuesta_criterio,
// para no duplicar aqui una lista que ya vive en 09_catalogo_criterios_tecnicos.sql.
public static class CriteriosTecnicosValidator
{
    public static void Validar(RegistrarCriteriosTecnicosRequest request)
    {
        var errores = new List<string>();

        if (request.FuncionarioValidadorId <= 0)
            errores.Add("Debe indicar el funcionario validador que registra los criterios.");

        ValidarMetadata(errores, request.VinculadoComercioElectronico, request.ObservacionesTecnicas);
        ValidarValorCriterio(errores, "Dano potencial a la salud", request.DanoPotencialSalud);
        ValidarValorCriterio(errores, "Criterio tecnico especializado UVC", request.RequiereCriterioEspecializado);

        if (errores.Count > 0)
            throw new DenunciaValidationException(errores);
    }

    // Correccion de alcance HU-010: actualizar es por criterio
    // individual, no por evaluacion completa (ver ActualizarCriterioTecnicoRequest).
    public static void Validar(ActualizarCriterioTecnicoRequest request)
    {
        var errores = new List<string>();

        if (request.FuncionarioId <= 0)
            errores.Add("Debe indicar el funcionario que ejecuta la modificacion.");

        if (!ValidadorTexto.EsValido(request.RolFuncionarioSimulado, longitudMinima: 3))
            errores.Add("Debe indicar el rol del funcionario que ejecuta la modificacion.");

        ValidarValorCriterio(errores, "seleccionado", request.ValorSeleccionado);

        if (errores.Count > 0)
            throw new DenunciaValidationException(errores);
    }

    // Metadata de la evaluacion (comercio electronico/observaciones),
    // separada de los criterios individuales (ver ActualizarMetadataCriteriosTecnicosRequest).
    public static void Validar(ActualizarMetadataCriteriosTecnicosRequest request)
    {
        var errores = new List<string>();

        if (request.FuncionarioId <= 0)
            errores.Add("Debe indicar el funcionario que ejecuta la modificacion.");

        if (!ValidadorTexto.EsValido(request.RolFuncionarioSimulado, longitudMinima: 3))
            errores.Add("Debe indicar el rol del funcionario que ejecuta la modificacion.");

        ValidarMetadata(errores, request.VinculadoComercioElectronico, request.ObservacionesTecnicas);

        if (errores.Count > 0)
            throw new DenunciaValidationException(errores);
    }

    public static void ValidarJustificacionEdicion(string? justificacion)
    {
        if (!ValidadorTexto.EsValido(justificacion, longitudMinima: 10))
        {
            throw new DenunciaValidationException([
                "Debe indicar la justificación del cambio (mínimo 10 caracteres) para editar un criterio ya registrado."
            ]);
        }
    }

    public static void ValidarRolSuperusuario(string rolFuncionarioSimulado)
    {
        if (!string.Equals(rolFuncionarioSimulado, "superusuario", StringComparison.OrdinalIgnoreCase))
        {
            throw new DenunciaValidationException([
                "Solo un superusuario puede modificar los criterios tecnicos registrados."
            ]);
        }
    }

    private static void ValidarValorCriterio(List<string> errores, string etiqueta, string valor)
    {
        if (!ValidadorTexto.EsValido(valor, longitudMinima: 2))
            errores.Add($"Debe indicar un valor valido para el criterio \"{etiqueta}\".");
    }

    private static void ValidarMetadata(
        List<string> errores,
        string vinculadoComercioElectronico,
        string? observacionesTecnicas)
    {
        if (vinculadoComercioElectronico is not ("S" or "N"))
            errores.Add("Debe indicar si la denuncia esta vinculada a comercio electronico (S/N).");

        if (observacionesTecnicas is { Length: > 1000 })
            errores.Add("Las observaciones tecnicas no pueden superar los 1000 caracteres.");
    }
}