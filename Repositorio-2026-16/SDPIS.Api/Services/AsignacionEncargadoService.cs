using SDPIS.Api.Models.DTOs;
using SDPIS.Api.Repositories;
using SDPIS.Api.Services.Validation;

namespace SDPIS.Api.Services;

// HU-012 "Asignar denuncia a un funcionario encargado o reasignar la
// denuncia a otra área". Orquesta la banda de asignación de encargados
// (distinto del validador de HU-009) y la reasignación entre áreas,
// incluida la reasignación real a UVC por comercio electrónico (AC #5,
// antes TODO de HU-013; por cambio de nombre ahora es HU-012).
public class AsignacionEncargadoService(
    IAsignacionEncargadoRepository repository,
    IDisponibilidadService disponibilidadService) : IAsignacionEncargadoService
{
    public Task<List<DenunciaParaEncargadoDto>> ObtenerDenunciasBandejaAsync(long areaId, CancellationToken ct)
        => repository.ObtenerDenunciasBandejaAsync(areaId, ct);

    public Task<List<FuncionarioEncargadoDto>> ObtenerEncargadosDisponiblesAsync(long areaId, CancellationToken ct)
        // La regla de disponibilidad (HU-007) la aplica el query con
        // periodo_indisponibilidad; los jefes de área quedan excluidos
        // por la propia regla HU-012.
        => repository.ObtenerEncargadosDisponiblesAsync(areaId, ct);

    public Task<List<AreaReasignacionDto>> ObtenerAreasDestinoAsync(CancellationToken ct)
        => repository.ObtenerAreasDestinoAsync(ct);

    public Task<List<HistorialAsignacionDto>> ObtenerHistorialAsync(long denunciaId, CancellationToken ct)
        => repository.ObtenerHistorialAsync(denunciaId, ct);

    public async Task AsignarEncargadoAsync(long denunciaId, AsignarEncargadoRequest request, CancellationToken ct)
    {
        AsignacionEncargadoValidator.Validar(request);

        // Regla de negocio HU-012 + HU-007: no se asigna un encargado con
        // indisponibilidad vigente. Reusamos el servicio de disponibilidad.
        var disponible = await disponibilidadService.EsDisponibleAsync(request.EncargadoId, DateOnly.FromDateTime(DateTime.Today), ct);
        if (!disponible)
        {
            throw new DenunciaBaseDatosException(
                "El funcionario seleccionado no esta disponible; no se puede asignar como encargado.",
                new InvalidOperationException($"Funcionario {request.EncargadoId} con indisponibilidad vigente."));
        }

        await repository.AsignarEncargadoAsync(
            denunciaId, request.EncargadoId, request.JefeAreaId, Normalizar(request.Motivo), ct);
    }

    public async Task ReasignarAreaAsync(long denunciaId, ReasignarAreaRequest request, CancellationToken ct)
    {
        AsignacionEncargadoValidator.Validar(request);

        await repository.ReasignarAreaAsync(
            denunciaId, request.AreaNuevaId, request.JefeAreaId, Normalizar(request.Motivo), ct);
    }

    // AC #5: reasignación real a UVC. Se dispara desde la bandeja de HU-012
    // sobre cualquier denuncia ya evaluada en criticidad (no solo las de
    // comercio electrónico). El SP resuelve el área UVC automáticamente al
    // pasar area NULL. La justificación es obligatoria y queda en el
    // historial y la bitácora; el número de oficio es opcional.
    public async Task ReasignarAUvcAsync(long denunciaId, long jefeAreaId, string motivo, string? numeroOficio, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(motivo))
            throw new DenunciaValidationException(["Debe indicar el motivo de la reasignacion a UVC."]);

        var oficio = Normalizar(numeroOficio);
        if (oficio is { Length: > 50 })
            throw new DenunciaValidationException(["El numero de oficio no puede superar los 50 caracteres."]);

        await repository.ReasignarAUvcAsync(denunciaId, jefeAreaId, motivo.Trim(), oficio, ct);
    }

    // Alerta automática a la jefatura de área cuando la denuncia lleva
    // más de 3 días sin asignación de encargado (consideración HU-012).
    public Task<int> GenerarAlertasVencimientoAsync(long areaId, CancellationToken ct)
        => repository.GenerarAlertasVencimientoAsync(areaId, ct);

    // Confirmación de atención en la UVC por el superusuario (consideración HU-012).
    public Task ConfirmarAtencionUvcAsync(long denunciaId, ConfirmarAtencionUvcRequest request, CancellationToken ct)
    {
        if (request.SuperusuarioId <= 0)
            throw new DenunciaValidationException(["Debe indicar el superusuario que confirma la atencion."]);

        return repository.ConfirmarAtencionUvcAsync(denunciaId, request.SuperusuarioId, Normalizar(request.Motivo), ct);
    }

    // Devolución de la denuncia desde la UVC al Área Rectora (consideración HU-012).
    public Task DevolverAreaRectoraAsync(long denunciaId, DevolverAreaRectoraRequest request, CancellationToken ct)
    {
        if (request.AreaRectoraId <= 0)
            throw new DenunciaValidationException(["Debe indicar el area rectora de destino."]);

        if (request.SuperusuarioId <= 0)
            throw new DenunciaValidationException(["Debe indicar el superusuario que devuelve la denuncia."]);

        if (string.IsNullOrWhiteSpace(request.Motivo))
            throw new DenunciaValidationException(["Debe indicar la justificacion de la devolucion al area rectora."]);

        return repository.DevolverAreaRectoraAsync(denunciaId, request.AreaRectoraId, request.SuperusuarioId, Normalizar(request.Motivo), ct);
    }

    private static string? Normalizar(string? texto)
        => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
}