using SDPIS.Api.Repositories;
using SDPIS.Api.Services.Criticidad;

namespace SDPIS.Api.Tests.Services;

// Fake en memoria de IEvaluacionCriticidadRepository (ver FakeCriteriosTecnicosRepository
// para el mismo criterio: el proyecto no tiene libreria de mocking instalada).
public class FakeEvaluacionCriticidadRepository : IEvaluacionCriticidadRepository
{
    public List<RangoCriticidad> RangosARetornar { get; set; } =
    [
        new("BAJO", 0, 2, 30),
        new("MEDIO", 3, 4, 15),
        new("ALTO", 5, 6, 5),
    ];

    public bool SeLlamoRegistrarEvaluacion { get; private set; }
    public bool SeLlamoLimpiarEvaluacion { get; private set; }
    public bool SeLlamoRegistrarNotificacionUvc { get; private set; }
    public bool SeLlamoRegistrarPendienteReasignacionUvc { get; private set; }
    public long? UltimoDenunciaId { get; private set; }
    public ResultadoCalculoCriticidad? UltimoResultadoRegistrado { get; private set; }

    public Task<IReadOnlyList<RangoCriticidad>> ObtenerRangosAsync(CancellationToken ct)
    {
        return Task.FromResult<IReadOnlyList<RangoCriticidad>>(RangosARetornar);
    }

    public Task RegistrarEvaluacionAsync(long denunciaId, ResultadoCalculoCriticidad resultado, CancellationToken ct)
    {
        SeLlamoRegistrarEvaluacion = true;
        UltimoDenunciaId = denunciaId;
        UltimoResultadoRegistrado = resultado;
        return Task.CompletedTask;
    }

    public Task LimpiarEvaluacionAsync(long denunciaId, CancellationToken ct)
    {
        SeLlamoLimpiarEvaluacion = true;
        UltimoDenunciaId = denunciaId;
        return Task.CompletedTask;
    }

    public Task RegistrarNotificacionUvcAsync(long denunciaId, CancellationToken ct)
    {
        SeLlamoRegistrarNotificacionUvc = true;
        return Task.CompletedTask;
    }

    public Task RegistrarPendienteReasignacionUvcAsync(long denunciaId, CancellationToken ct)
    {
        SeLlamoRegistrarPendienteReasignacionUvc = true;
        return Task.CompletedTask;
    }
}
