using SDPIS.Api.Models.DTOs;
using SDPIS.Api.Repositories;

namespace SDPIS.Api.Services.Validation;

// Valida entradas de la bitacora (HU-016). Los tipos permitidos son los de
// TiposBitacora, que reflejan el CHECK de bitacora.tipo_registro.
public static class BitacoraValidator
{
    public const int LongitudMaximaContenido = 2000;
    public const int TamanoPaginaMaximo = 100;

    private static readonly IReadOnlySet<string> TiposValidos = TiposBitacora.Todos;

    public static void ValidarConsulta(long denunciaId, long funcionarioId, string? tipoRegistro, int pagina, int tamano)
    {
        var errores = new List<string>();

        if (denunciaId <= 0)
            errores.Add("Debe indicar una denuncia valida.");
        if (funcionarioId <= 0)
            errores.Add("Debe indicar el funcionario que consulta la bitacora.");
        if (tipoRegistro is not null && !TiposValidos.Contains(tipoRegistro))
            errores.Add("El tipo de registro indicado no es valido.");
        if (pagina < 1)
            errores.Add("La pagina debe ser mayor o igual a 1.");
        if (tamano < 1 || tamano > TamanoPaginaMaximo)
            errores.Add($"El tamano de pagina debe estar entre 1 y {TamanoPaginaMaximo}.");

        if (errores.Count > 0)
            throw new DenunciaValidationException(errores);
    }

    public static void ValidarComentario(long denunciaId, AgregarComentarioBitacoraRequest request)
    {
        var errores = new List<string>();

        if (denunciaId <= 0)
            errores.Add("Debe indicar una denuncia valida.");
        if (request.FuncionarioId <= 0)
            errores.Add("Debe indicar el funcionario que agrega el comentario.");
        ValidarContenido(errores, request.Contenido);

        if (errores.Count > 0)
            throw new DenunciaValidationException(errores);
    }

    // Entrada del servicio reutilizable consumido por las demas HUs.
    public static void ValidarRegistro(long denunciaId, string tipoRegistro, string contenido)
    {
        var errores = new List<string>();

        if (denunciaId <= 0)
            errores.Add("Debe indicar una denuncia valida.");
        if (!TiposValidos.Contains(tipoRegistro))
            errores.Add("El tipo de registro indicado no es valido.");
        ValidarContenido(errores, contenido);

        if (errores.Count > 0)
            throw new DenunciaValidationException(errores);
    }

    private static void ValidarContenido(List<string> errores, string? contenido)
    {
        if (string.IsNullOrWhiteSpace(contenido))
            errores.Add("El contenido no puede estar vacio.");
        else if (contenido.Trim().Length > LongitudMaximaContenido)
            errores.Add($"El contenido no puede superar {LongitudMaximaContenido} caracteres.");
    }
}
