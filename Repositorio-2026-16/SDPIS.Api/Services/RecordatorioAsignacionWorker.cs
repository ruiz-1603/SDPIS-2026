using SDPIS.Api.Repositories;

namespace SDPIS.Api.Services;

public class RecordatorioAsignacionWorker(
    IServiceProvider serviceProvider,
    ILogger<RecordatorioAsignacionWorker> logger) : BackgroundService
{
    // frecuencia de revision: cada hora (por confirmar)
    private readonly TimeSpan _frecuenciaRevision = TimeSpan.FromHours(1);
    private readonly int _diasLimiteSla = 3;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("RecordatorioAsignacionWorker iniciado. Revisando SLA de asignación cada {Minutos} minuto(s).", _frecuenciaRevision.TotalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                logger.LogInformation("Ejecutando revisión de denuncias atrasadas...");
                
                using var scope = serviceProvider.CreateScope();
                var asignacionRepo = scope.ServiceProvider.GetRequiredService<IAsignacionRepository>();

                var denunciasAtrasadas = await asignacionRepo.ObtenerDenunciasAtrasadasAsignacionAsync(_diasLimiteSla, stoppingToken);

                if (denunciasAtrasadas.Count > 0)
                {
                    logger.LogWarning("Se encontraron {Cantidad} denuncias atrasadas. Generando recordatorios para jefaturas...", denunciasAtrasadas.Count);

                    foreach (var denuncia in denunciasAtrasadas)
                    {
                        await asignacionRepo.GenerarRecordatorioAtrasoAsync(denuncia.IdDenuncia, denuncia.Consecutivo, stoppingToken);
                        
                        logger.LogInformation("Recordatorio generado para la denuncia {Consecutivo} (Área {AreaId}).", denuncia.Consecutivo, denuncia.AreaActualId);
                    }
                }
                else
                {
                    logger.LogInformation("No se encontraron denuncias atrasadas en esta revisión.");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ocurrió un error durante la ejecución del RecordatorioAsignacionWorker.");
            }
            await Task.Delay(_frecuenciaRevision, stoppingToken);
        }

        logger.LogInformation("RecordatorioAsignacionWorker detenido.");
    }
}
