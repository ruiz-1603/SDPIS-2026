using Microsoft.AspNetCore.Mvc;
using SDPIS.Api.Models.DTOs;
using SDPIS.Api.Services;

namespace SDPIS.Api.Controllers;

[ApiController]
public class AsignacionEncargadoController(IAsignacionEncargadoService service) : ControllerBase
{
    // GET /api/areas/{areaId}/encargados/bandeja
    [HttpGet("/api/areas/{areaId:long}/encargados/bandeja")]
    public async Task<ActionResult<List<DenunciaParaEncargadoDto>>> ObtenerBandeja(
        long areaId, CancellationToken ct)
    {
        var denuncias = await service.ObtenerDenunciasBandejaAsync(areaId, ct);
        return Ok(denuncias);
    }

    // GET /api/areas/{areaId}/encargados/disponibles
    [HttpGet("/api/areas/{areaId:long}/encargados/disponibles")]
    public async Task<ActionResult<List<FuncionarioEncargadoDto>>> ObtenerEncargadosDisponibles(
        long areaId, CancellationToken ct)
    {
        var encargados = await service.ObtenerEncargadosDisponiblesAsync(areaId, ct);
        return Ok(encargados);
    }

    // GET /api/areas/destino
    [HttpGet("/api/areas/destino")]
    public async Task<ActionResult<List<AreaReasignacionDto>>> ObtenerAreasDestino(CancellationToken ct)
    {
        var areas = await service.ObtenerAreasDestinoAsync(ct);
        return Ok(areas);
    }

    // GET /api/denuncias/{denunciaId}/historial-asignacion
    [HttpGet("/api/denuncias/{denunciaId:long}/historial-asignacion")]
    public async Task<ActionResult<List<HistorialAsignacionDto>>> ObtenerHistorial(
        long denunciaId, CancellationToken ct)
    {
        var historial = await service.ObtenerHistorialAsync(denunciaId, ct);
        return Ok(historial);
    }

    // POST /api/areas/{areaId}/denuncias/{denunciaId}/asignar-encargado
    [HttpPost("/api/areas/{areaId:long}/denuncias/{denunciaId:long}/asignar-encargado")]
    public async Task<ActionResult> AsignarEncargado(
        long areaId, long denunciaId, [FromBody] AsignarEncargadoRequest request, CancellationToken ct)
    {
        await service.AsignarEncargadoAsync(denunciaId, request, ct);
        return Ok(new { Mensaje = "Denuncia asignada al funcionario encargado correctamente." });
    }

    // POST /api/areas/{areaId}/denuncias/{denunciaId}/reasignar-area
    [HttpPost("/api/areas/{areaId:long}/denuncias/{denunciaId:long}/reasignar-area")]
    public async Task<ActionResult> ReasignarArea(
        long areaId, long denunciaId, [FromBody] ReasignarAreaRequest request, CancellationToken ct)
    {
        await service.ReasignarAreaAsync(denunciaId, request, ct);
        return Ok(new { Mensaje = "Denuncia reasignada de area correctamente." });
    }

    // POST /api/areas/{areaId}/denuncias/{denunciaId}/reasignar-uvc
    // AC #5: reasignación real a UVC por comercio electrónico.
    [HttpPost("/api/areas/{areaId:long}/denuncias/{denunciaId:long}/reasignar-uvc")]
    public async Task<ActionResult> ReasignarAUvc(
        long areaId, long denunciaId, [FromBody] ReasignarAUvcRequest request, CancellationToken ct)
    {
        await service.ReasignarAUvcAsync(denunciaId, request.JefeAreaId, request.Motivo, request.NumeroOficio, ct);
        return Ok(new { Mensaje = "Denuncia reasignada a UVC correctamente." });
    }

    // POST /api/areas/{areaId}/denuncias/alertas/vencimiento
    // Consideración HU-012: alerta a la jefatura de área cuando una
    // denuncia lleva más de 3 días sin asignación de encargado.
    [HttpPost("/api/areas/{areaId:long}/denuncias/alertas/vencimiento")]
    public async Task<ActionResult> GenerarAlertasVencimiento(long areaId, CancellationToken ct)
    {
        var generadas = await service.GenerarAlertasVencimientoAsync(areaId, ct);
        return Ok(new { Mensaje = $"Se generaron {generadas} alertas de vencimiento de asignacion.", Generadas = generadas });
    }

    // POST /api/areas/{areaId}/denuncias/{denunciaId}/confirmar-atencion-uvc
    // Consideración HU-012: el superusuario de UVC confirma la atención del caso.
    [HttpPost("/api/areas/{areaId:long}/denuncias/{denunciaId:long}/confirmar-atencion-uvc")]
    public async Task<ActionResult> ConfirmarAtencionUvc(
        long areaId, long denunciaId, [FromBody] ConfirmarAtencionUvcRequest request, CancellationToken ct)
    {
        await service.ConfirmarAtencionUvcAsync(denunciaId, request, ct);
        return Ok(new { Mensaje = "Denuncia confirmada como atendida en la UVC." });
    }

    // POST /api/areas/{areaId}/denuncias/{denunciaId}/devolver-area-rectora
    // Consideración HU-012: el superusuario de UVC devuelve la denuncia al Área Rectora.
    [HttpPost("/api/areas/{areaId:long}/denuncias/{denunciaId:long}/devolver-area-rectora")]
    public async Task<ActionResult> DevolverAreaRectora(
        long areaId, long denunciaId, [FromBody] DevolverAreaRectoraRequest request, CancellationToken ct)
    {
        await service.DevolverAreaRectoraAsync(denunciaId, request, ct);
        return Ok(new { Mensaje = "Denuncia devuelta al area rectora correctamente." });
    }
}