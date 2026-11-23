using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SDPIS.Api.Models.DTOs;
using SDPIS.Api.Services;
using SDPIS.Api.Services.Autorizacion;

namespace SDPIS.Api.Controllers;

// HU-018: todos los endpoints exigen sesion. El funcionario que solicita sale
// SIEMPRE del token (el servicio usa IUsuarioActual), nunca del body.
[ApiController]
[Authorize]
public class SolicitudCierreController(ISolicitudCierreService service) : ControllerBase
{
    // GET /api/denuncias/{denunciaId}/solicitud-cierre -- HU-018
    // Si el usuario puede solicitar el cierre y la solicitud pendiente (si hay).
    [HttpGet("/api/denuncias/{denunciaId:long}/solicitud-cierre")]
    public async Task<ActionResult<EstadoSolicitudCierreDto>> ObtenerEstado(long denunciaId, CancellationToken ct)
        => Ok(await service.ObtenerEstadoAsync(denunciaId, ct));

    // POST /api/denuncias/{denunciaId}/solicitud-cierre -- HU-018
    // Body: { resolucionFinal, observacionesFinales, accionesRealizadas }.
    // 403 si no es el responsable asignado; 422 si la denuncia no esta en curso
    // o ya tiene una solicitud pendiente.
    [HttpPost("/api/denuncias/{denunciaId:long}/solicitud-cierre")]
    [RequierePermiso(Permisos.SolicitarCierre)]
    public async Task<ActionResult<SolicitudCierreDto>> Solicitar(
        long denunciaId, [FromBody] SolicitarCierreRequest request, CancellationToken ct)
    {
        var solicitud = await service.SolicitarAsync(denunciaId, request, ct);
        return StatusCode(StatusCodes.Status201Created, solicitud);
    }

    // GET /api/solicitudes-cierre/pendientes -- HU-018
    // Bandeja de la jefatura (solo lectura: aprobar/rechazar es HU-017).
    [HttpGet("/api/solicitudes-cierre/pendientes")]
    public async Task<ActionResult<List<SolicitudCierreDto>>> ListarPendientes(CancellationToken ct)
        => Ok(await service.ListarPendientesAsync(ct));
}
