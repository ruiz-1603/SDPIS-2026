using SDPIS.Api.Models.DTOs;
using SDPIS.Api.Services.Validation;

namespace SDPIS.Api.Tests.Services;

// HU-014: validacion de FORMA de la configuracion de plazos (logica pura).
public class PlazosAtencionValidatorTests
{
    [Theory]
    [InlineData("ALTO", 1)]
    [InlineData("MEDIO", 90)]
    [InlineData("bajo", PlazosAtencionValidator.PlazoMaximoDias)]
    public void ValidarActualizar_SolicitudValida_NoLanzaExcepcion(string nivel, int dias)
    {
        var excepcion = Record.Exception(
            () => PlazosAtencionValidator.ValidarActualizar(nivel, new ActualizarPlazoRequest(dias)));

        Assert.Null(excepcion);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(PlazosAtencionValidator.PlazoMaximoDias + 1)]
    public void ValidarActualizar_DiasFueraDeRango_LanzaError(int dias)
    {
        var excepcion = Assert.Throws<DenunciaValidationException>(
            () => PlazosAtencionValidator.ValidarActualizar("ALTO", new ActualizarPlazoRequest(dias)));

        Assert.Contains(excepcion.Errores, e => e.Contains("plazo", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("CRITICO")]
    public void ValidarActualizar_NivelInvalido_LanzaError(string? nivel)
    {
        var excepcion = Assert.Throws<DenunciaValidationException>(
            () => PlazosAtencionValidator.ValidarActualizar(nivel, new ActualizarPlazoRequest(60)));

        Assert.Contains(excepcion.Errores, e => e.Contains("nivel", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ValidarActualizar_SolicitudNula_LanzaError()
    {
        Assert.Throws<DenunciaValidationException>(
            () => PlazosAtencionValidator.ValidarActualizar("ALTO", null));
    }
}
