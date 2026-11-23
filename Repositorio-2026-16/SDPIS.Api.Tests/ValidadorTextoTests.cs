using SDPIS.Api.Services.Validation;
using Xunit;

namespace SDPIS.Test.Validation;

public class ValidadorTextoTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EsValido_ConTextoNuloOVacio_RetornaFalso(string? texto)
    {
        Assert.False(ValidadorTexto.EsValido(texto));
    }

    [Fact]
    public void EsValido_ConSoloPuntuacion_RetornaFalso()
    {
        // No contiene ningun caracter alfanumerico -> invalido aunque tenga longitud suficiente.
        Assert.False(ValidadorTexto.EsValido("!!!...---"));
    }

    [Theory]
    [InlineData("ab", 3)]   // 2 caracteres, minimo 3 -> invalido
    [InlineData("abc", 3)]  // exactamente el minimo -> valido
    public void EsValido_RespetaLongitudMinima(string texto, int longitudMinima)
    {
        var esperado = texto.Trim().Length >= longitudMinima;
        Assert.Equal(esperado, ValidadorTexto.EsValido(texto, longitudMinima));
    }

    [Fact]
    public void EsValido_ConEspaciosAlrededor_LosIgnoraParaLaLongitud()
    {
        // "  hi  " tiene 6 caracteres crudos pero solo 2 utiles -> invalido con minimo 3
        Assert.False(ValidadorTexto.EsValido("  hi  ", longitudMinima: 3));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData("  Perfume  ", "Perfume")]
    public void NormalizarABlancoNulo_ConvierteBlancosAnullYRecortaEspacios(string? entrada, string? esperado)
    {
        Assert.Equal(esperado, ValidadorTexto.NormalizarABlancoNulo(entrada));
    }
}
