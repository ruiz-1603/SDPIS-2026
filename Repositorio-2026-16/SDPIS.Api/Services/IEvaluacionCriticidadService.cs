using SDPIS.Api.Services.Criticidad;

namespace SDPIS.Api.Services;

public interface IEvaluacionCriticidadService
{
    // Calcula el nivel a partir del puntaje y lo persiste. nivelAnterior es
    // el nivel que tenia la evaluacion antes de este recalculo (null si es
    // el primer calculo): solo notifica a UVC si el nivel PASA a Alto, no
    // si ya lo era (HU-010 ahora permite editar criterios individuales, y
    // cada edicion dispara un recalculo -- sin esto se notificaria de mas).
    Task<ResultadoCalculoCriticidad> EvaluarAsync(
        long denunciaId, decimal puntajeTotal, string? nivelAnterior, CancellationToken ct);

    // La evaluacion quedo incompleta (falta un criterio puntuable tras una
    // edicion individual): limpia el nivel/plazo calculados en
    // vez de dejar un resultado basado en datos parciales.
    Task LimpiarAsync(long denunciaId, CancellationToken ct);

    // AC #5: dispara el stub de reasignacion pendiente solo cuando el
    // indicador de comercio electronico PASA a "Si" (no si ya lo era).
    Task EvaluarComercioElectronicoAsync(
        long denunciaId, string vinculadoComercioElectronico, string? vinculadoComercioElectronicoAnterior,
        CancellationToken ct);
}
