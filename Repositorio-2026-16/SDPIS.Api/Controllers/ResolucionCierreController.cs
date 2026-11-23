using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using SDPIS.Api.Models.DTOs;
using SDPIS.Api.Services;
using SDPIS.Api.Services.Autorizacion;

namespace SDPIS.Api.Controllers;

// HU-017: todos los endpoints exigen sesion. El jefe que decide sale SIEMPRE
// del token (el servicio usa IUsuarioActual), nunca del body.
[ApiController]
[Authorize]
public class ResolucionCierreController(IResolucionCierreService service) : ControllerBase
{
    // POST /api/solicitudes-cierre/{solicitudId}/aprobar -- HU-017
    // Body opcional: { observaciones }. La denuncia pasa a FINALIZADA.
    // 403 si no es la jefatura del area de la solicitud; 404 si no existe;
    // 422 si ya fue resuelta o la denuncia no esta en revision.
    [HttpPost("/api/solicitudes-cierre/{solicitudId:long}/aprobar")]
    [RequierePermiso(Permisos.AprobarCierre)]
    public async Task<ActionResult<ResolucionCierreDto>> Aprobar(long solicitudId,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ResolverCierreRequest? request, CancellationToken ct)
        => Ok(await service.AprobarAsync(solicitudId, request, ct));

    // POST /api/solicitudes-cierre/{solicitudId}/rechazar -- HU-017
    // Body: { observaciones } (obligatorias; 400 si faltan). La denuncia
    // vuelve al estado que tenia antes de la solicitud de cierre.
    [HttpPost("/api/solicitudes-cierre/{solicitudId:long}/rechazar")]
    [RequierePermiso(Permisos.AprobarCierre)]
    public async Task<ActionResult<ResolucionCierreDto>> Rechazar(long solicitudId,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ResolverCierreRequest? request, CancellationToken ct)
        => Ok(await service.RechazarAsync(solicitudId, request, ct));

    // GET /api/denuncias/{denunciaId}/solicitudes-cierre/historial -- HU-017
    // Solicitudes de cierre de la denuncia con la decision de la jefatura.
    [HttpGet("/api/denuncias/{denunciaId:long}/solicitudes-cierre/historial")]
    public async Task<ActionResult<List<HistorialSolicitudCierreDto>>> Historial(long denunciaId, CancellationToken ct)
        => Ok(await service.ObtenerHistorialAsync(denunciaId, ct));
}
