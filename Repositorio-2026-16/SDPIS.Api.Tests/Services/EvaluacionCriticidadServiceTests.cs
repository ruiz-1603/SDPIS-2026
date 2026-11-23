using SDPIS.Api.Services;
using Xunit;

namespace SDPIS.Api.Tests.Services;

public class EvaluacionCriticidadServiceTests
{
    // Criterios de aceptacion #6, #7: el resultado siempre se registra,
    // sin importar el nivel obtenido.
    [Theory]
    [InlineData(6, "ALTO")]
    [InlineData(4, "MEDIO")]
    [InlineData(2, "BAJO")]
    public async Task EvaluarAsync_ConCualquierPuntaje_RegistraElResultado(decimal puntajeTotal, string nivelEsperado)
    {
        var repository = new FakeEvaluacionCriticidadRepository();
        var service = new EvaluacionCriticidadService(repository);

        var resultado = await service.EvaluarAsync(9001, puntajeTotal, nivelAnterior: null, CancellationToken.None);

        Assert.True(repository.SeLlamoRegistrarEvaluacion);
        Assert.Equal(9001, repository.UltimoDenunciaId);
        Assert.Equal(nivelEsperado, resultado.Nivel);
        Assert.Equal(nivelEsperado, repository.UltimoResultadoRegistrado?.Nivel);
    }

    // Criterio de aceptacion #4: nivel Alto notifica a UVC cuando antes NO
    // era Alto (primera evaluacion, o transicion desde Medio/Bajo).
    [Theory]
    [InlineData(null)]
    [InlineData("MEDIO")]
    [InlineData("BAJO")]
    public async Task EvaluarAsync_ConNivelQuePasaAAlto_NotificaAUvc(string? nivelAnterior)
    {
        var repository = new FakeEvaluacionCriticidadRepository();
        var service = new EvaluacionCriticidadService(repository);

        await service.EvaluarAsync(9001, 6, nivelAnterior, CancellationToken.None);

        Assert.True(repository.SeLlamoRegistrarNotificacionUvc);
    }

    // HU-010 ahora permite editar criterios individuales, lo que puede
    // disparar varios recalculos mientras el nivel se mantiene en Alto: no
    // se debe notificar de nuevo si ya era Alto antes de este recalculo.
    [Fact]
    public async Task EvaluarAsync_ConNivelQueYaEraAlto_NoNotificaDeNuevo()
    {
        var repository = new FakeEvaluacionCriticidadRepository();
        var service = new EvaluacionCriticidadService(repository);

        await service.EvaluarAsync(9001, 6, nivelAnterior: "ALTO", CancellationToken.None);

        Assert.False(repository.SeLlamoRegistrarNotificacionUvc);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(2)]
    public async Task EvaluarAsync_ConNivelMedioOBajo_NoNotificaAUvc(decimal puntajeTotal)
    {
        var repository = new FakeEvaluacionCriticidadRepository();
        var service = new EvaluacionCriticidadService(repository);

        await service.EvaluarAsync(9001, puntajeTotal, nivelAnterior: null, CancellationToken.None);

        Assert.False(repository.SeLlamoRegistrarNotificacionUvc);
    }

    [Fact]
    public async Task EvaluarAsync_ConPuntajeSinRangoConfigurado_LanzaExcepcionYNoRegistraNada()
    {
        var repository = new FakeEvaluacionCriticidadRepository();
        var service = new EvaluacionCriticidadService(repository);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.EvaluarAsync(9001, 100, nivelAnterior: null, CancellationToken.None));

        Assert.False(repository.SeLlamoRegistrarEvaluacion);
        Assert.False(repository.SeLlamoRegistrarNotificacionUvc);
    }

    // HU-011: si tras editar un criterio la evaluacion queda
    // incompleta, se limpia el nivel en vez de calcularlo sobre datos
    // parciales.
    [Fact]
    public async Task LimpiarAsync_LlamaAlRepositorio()
    {
        var repository = new FakeEvaluacionCriticidadRepository();
        var service = new EvaluacionCriticidadService(repository);

        await service.LimpiarAsync(9001, CancellationToken.None);

        Assert.True(repository.SeLlamoLimpiarEvaluacion);
        Assert.Equal(9001, repository.UltimoDenunciaId);
    }

    // Criterio de aceptacion #5: solo la transicion N -> S (o primera vez)
    // dispara el TODO de reasignacion a UVC.
    [Theory]
    [InlineData(null)]
    [InlineData("N")]
    public async Task EvaluarComercioElectronicoAsync_ConTransicionASi_RegistraPendienteDeReasignacion(
        string? anterior)
    {
        var repository = new FakeEvaluacionCriticidadRepository();
        var service = new EvaluacionCriticidadService(repository);

        await service.EvaluarComercioElectronicoAsync(9001, "S", anterior, CancellationToken.None);

        Assert.True(repository.SeLlamoRegistrarPendienteReasignacionUvc);
    }

    // Si ya era "S" antes (p.ej. se guardo la metadata de nuevo sin cambiar
    // ese campo), no se debe repetir la constancia en bitacora.
    [Fact]
    public async Task EvaluarComercioElectronicoAsync_ConTransicionYaEraSi_NoRegistraDeNuevo()
    {
        var repository = new FakeEvaluacionCriticidadRepository();
        var service = new EvaluacionCriticidadService(repository);

        await service.EvaluarComercioElectronicoAsync(9001, "S", "S", CancellationToken.None);

        Assert.False(repository.SeLlamoRegistrarPendienteReasignacionUvc);
    }

    [Fact]
    public async Task EvaluarComercioElectronicoAsync_ConNo_NoRegistraPendienteDeReasignacion()
    {
        var repository = new FakeEvaluacionCriticidadRepository();
        var service = new EvaluacionCriticidadService(repository);

        await service.EvaluarComercioElectronicoAsync(9001, "N", "S", CancellationToken.None);

        Assert.False(repository.SeLlamoRegistrarPendienteReasignacionUvc);
    }
}
