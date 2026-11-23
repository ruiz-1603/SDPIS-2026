using SDPIS.Api.Services.Criticidad;

namespace SDPIS.Api.Repositories;

public interface IEvaluacionCriticidadRepository
{
    // Catalogo nivel_criticidad (rangos sembrados para HU-011, ver AC #9).
    Task<IReadOnlyList<RangoCriticidad>> ObtenerRangosAsync(CancellationToken ct);

    // Persiste nivel_criticidad y fecha_limite en la evaluacion mas reciente
    // de la denuncia, y deja constancia en bitacora (AC #6, #7).
    Task RegistrarEvaluacionAsync(long denunciaId, ResultadoCalculoCriticidad resultado, CancellationToken ct);

    // Evaluacion incompleta (falta algun criterio puntuable, solo posible en
    // evaluaciones de cuando se permitia eliminar criterios): limpia nivel_criticidad y
    // fecha_limite en vez de dejar un valor calculado sobre datos parciales.
    Task LimpiarEvaluacionAsync(long denunciaId, CancellationToken ct);

    // AC #4: nivel Alto -> notificar a UVC sin cambiar de area. Por decision
    // del equipo, "notificar" hoy es dejar constancia (bitacora/registro),
    // no un envio de correo real: no hay infraestructura SMTP en el proyecto.
    Task RegistrarNotificacionUvcAsync(long denunciaId, CancellationToken ct);

    // AC #5: vinculado a comercio electronico -> disparo de reasignacion a UVC.
    // TODO: HU-013 "Gestionar reglas de criticidad" (#64) debe reemplazar esto
    // por la reasignacion real de area_actual_id. Por ahora solo se deja
    // constancia en bitacora de que la denuncia quedo pendiente.
    Task RegistrarPendienteReasignacionUvcAsync(long denunciaId, CancellationToken ct);
}
