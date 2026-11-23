using SDPIS.Api.Repositories;
using SDPIS.Api.Services.Criticidad;

namespace SDPIS.Api.Services;

// HU-011 "Evaluar criticidad de denuncias" (#62), subtarea #118: orquesta el
// calculo automatico de criticidad y sus dos disparadores.
//
// HU-010 permite ahora editar criterios individuales y la metadata
// (comercio electronico, etc.) por separado, cada operacion pudiendo
// disparar un recalculo. Por eso los triggers comparan contra el estado
// anterior y solo actuan en la TRANSICION hacia la condicion (nivel pasa a
// Alto, comercio electronico pasa a "Si"): de lo contrario, cada edicion que
// mantenga el mismo resultado generaria notificaciones/bitacora duplicadas.
public class EvaluacionCriticidadService(IEvaluacionCriticidadRepository repository) : IEvaluacionCriticidadService
{
    private const string NivelAlto = "ALTO";
    private const string ComercioElectronicoSi = "S";

    public async Task<ResultadoCalculoCriticidad> EvaluarAsync(
        long denunciaId, decimal puntajeTotal, string? nivelAnterior, CancellationToken ct)
    {
        var rangos = await repository.ObtenerRangosAsync(ct);
        var resultado = CriticidadCalculator.Calcular(puntajeTotal, rangos);

        await repository.RegistrarEvaluacionAsync(denunciaId, resultado, ct);

        // AC #4: nivel Alto no cambia el area -- el caso sigue en el ARS
        // correspondiente, solo se notifica a UVC. Solo en la transicion.
        if (resultado.Nivel == NivelAlto && nivelAnterior != NivelAlto)
            await repository.RegistrarNotificacionUvcAsync(denunciaId, ct);

        return resultado;
    }

    public Task LimpiarAsync(long denunciaId, CancellationToken ct)
        => repository.LimpiarEvaluacionAsync(denunciaId, ct);

    public Task EvaluarComercioElectronicoAsync(
        long denunciaId, string vinculadoComercioElectronico, string? vinculadoComercioElectronicoAnterior,
        CancellationToken ct)
    {
        // AC #5 y #8: el indicador de comercio electronico dispara la
        // reasignacion independientemente del puntaje/nivel calculado (no
        // suma al puntaje, ver AC #8). Solo en la transicion N -> S.
        //
        // TODO: HU-013 "Gestionar reglas de criticidad" (#64) debe
        // reemplazar RegistrarPendienteReasignacionUvcAsync por la
        // reasignacion real de area_actual_id hacia UVC.
        if (vinculadoComercioElectronico == ComercioElectronicoSi
            && vinculadoComercioElectronicoAnterior != ComercioElectronicoSi)
        {
            return repository.RegistrarPendienteReasignacionUvcAsync(denunciaId, ct);
        }

        return Task.CompletedTask;
    }
}
