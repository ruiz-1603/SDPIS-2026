using SDPIS.Api.Repositories;
using SDPIS.Api.Services.Criticidad;
using Xunit;

namespace SDPIS.Api.Tests.Services.Criticidad;

public class CriticidadCalculatorTests
{
    // Rangos de puntaje confirmados en el issue #62 de HU-011 (>=5 ALTO,
    // >=3 MEDIO, resto BAJO). Los PlazoDias replican los plazos oficiales
    // sembrados en 11_seed_nivel_criticidad.sql (ALTO 60, MEDIO 90, BAJO 120);
    // la calculadora es agnostica a su valor, solo propaga el del rango que
    // matchea.
    private static readonly List<RangoCriticidad> RangosConfirmados =
    [
        new("BAJO", 0, 2, 120),
        new("MEDIO", 3, 4, 90),
        new("ALTO", 5, 6, 60),
    ];

    // Criterios de aceptacion #1, #2, #7, #8: clasificar segun los umbrales
    // institucionales confirmados.
    [Theory]
    [InlineData(6, "ALTO")]
    [InlineData(5, "ALTO")]
    [InlineData(4, "MEDIO")]
    [InlineData(3, "MEDIO")]
    [InlineData(2, "BAJO")]
    [InlineData(0, "BAJO")]
    public void Calcular_ConPuntajeTotal_AsignaElNivelCorrespondiente(decimal puntajeTotal, string nivelEsperado)
    {
        var resultado = CriticidadCalculator.Calcular(puntajeTotal, RangosConfirmados);

        Assert.Equal(nivelEsperado, resultado.Nivel);
    }

    // Criterio de aceptacion #10: el plazo del rango asignado es el insumo
    // para la definicion automatica de plazos de atencion.
    [Fact]
    public void Calcular_ConPuntajeTotal_DevuelvePlazoDelRangoCorrespondiente()
    {
        var resultado = CriticidadCalculator.Calcular(5, RangosConfirmados);

        Assert.Equal(60, resultado.PlazoDias);
    }

    [Fact]
    public void Calcular_SinRangoQueCubraElPuntaje_LanzaExcepcion()
    {
        Assert.Throws<InvalidOperationException>(
            () => CriticidadCalculator.Calcular(100, RangosConfirmados));
    }

    [Fact]
    public void Calcular_SinRangosConfigurados_LanzaExcepcion()
    {
        Assert.Throws<InvalidOperationException>(
            () => CriticidadCalculator.Calcular(5, []));
    }
}
