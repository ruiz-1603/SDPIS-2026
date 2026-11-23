using SDPIS.Api.Services;
using SDPIS.Api.Services.Criticidad;

namespace SDPIS.Api.Tests.Services;

// Fake de IEvaluacionCriticidadService para aislar CriteriosTecnicosServiceTests
// del calculo real de EvaluacionCriticidadService (ese calculo ya tiene sus
// propias pruebas en EvaluacionCriticidadServiceTests).
public class FakeEvaluacionCriticidadService : IEvaluacionCriticidadService
{
    public ResultadoCalculoCriticidad ResultadoARetornar { get; set; } = new("BAJO", 0);

    public bool SeLlamoEvaluar { get; private set; }
    public bool SeLlamoLimpiar { get; private set; }
    public bool SeLlamoEvaluarComercioElectronico { get; private set; }

    public long? UltimoDenunciaId { get; private set; }
    public decimal? UltimoPuntajeTotal { get; private set; }
    public string? UltimoNivelAnterior { get; private set; }
    public string? UltimoVinculadoComercioElectronico { get; private set; }
    public string? UltimoVinculadoComercioElectronicoAnterior { get; private set; }

    public Task<ResultadoCalculoCriticidad> EvaluarAsync(
        long denunciaId, decimal puntajeTotal, string? nivelAnterior, CancellationToken ct)
    {
        SeLlamoEvaluar = true;
        UltimoDenunciaId = denunciaId;
        UltimoPuntajeTotal = puntajeTotal;
        UltimoNivelAnterior = nivelAnterior;
        return Task.FromResult(ResultadoARetornar);
    }

    public Task LimpiarAsync(long denunciaId, CancellationToken ct)
    {
        SeLlamoLimpiar = true;
        UltimoDenunciaId = denunciaId;
        return Task.CompletedTask;
    }

    public Task EvaluarComercioElectronicoAsync(
        long denunciaId, string vinculadoComercioElectronico, string? vinculadoComercioElectronicoAnterior,
        CancellationToken ct)
    {
        SeLlamoEvaluarComercioElectronico = true;
        UltimoDenunciaId = denunciaId;
        UltimoVinculadoComercioElectronico = vinculadoComercioElectronico;
        UltimoVinculadoComercioElectronicoAnterior = vinculadoComercioElectronicoAnterior;
        return Task.CompletedTask;
    }
}
