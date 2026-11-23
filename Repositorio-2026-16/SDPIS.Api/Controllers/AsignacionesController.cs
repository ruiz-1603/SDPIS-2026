using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SDPIS.Api.Models.DTOs;
using SDPIS.Api.Services;
using SDPIS.Api.Services.Autorizacion;

namespace SDPIS.Api.Controllers;

// HU-009 + HU-015: asignacion de validador. Exige sesion y el permiso de
// asignar denuncias. El area y quien asigna salen del token:
//   superusuario      -> todas las areas (areaId opcional para filtrar);
//   jefe o delegado   -> solo su area (si pide otra, 403).
// Reemplaza a las rutas /api/areas/{areaId}/..., que recibian el area y el
// jefe desde la pantalla.
[ApiController]
[Route("api/asignaciones")]
[Authorize]
[RequierePermiso(Permisos.AsignarDenuncias)]
public class AsignacionesController(IAsignacionService asignacionService) : ControllerBase
{
    // GET /api/asignaciones/denuncias/pendientes?areaId=
    [HttpGet("denuncias/pendientes")]
    public async Task<ActionResult<List<DenunciaPendienteDto>>> ObtenerDenunciasPendientes(
        [FromQuery] long? areaId, CancellationToken ct)
        => Ok(await asignacionService.ObtenerDenunciasPendientesAsync(areaId, ct));

    // GET /api/asignaciones/denuncias/asignadas?areaId=
    [HttpGet("denuncias/asignadas")]
    public async Task<ActionResult<List<DenunciaAsignadaDto>>> ObtenerDenunciasAsignadas(
        [FromQuery] long? areaId, CancellationToken ct)
        => Ok(await asignacionService.ObtenerDenunciasAsignadasAsync(areaId, ct));

    // GET /api/asignaciones/validadores?areaId=
    // Validadores disponibles del area de la denuncia (el superusuario debe
    // indicar el area; el jefe recibe los de la suya).
    [HttpGet("validadores")]
    public async Task<ActionResult<List<ValidadorCargaDto>>> ObtenerValidadoresDisponibles(
        [FromQuery] long? areaId, CancellationToken ct)
        => Ok(await asignacionService.ObtenerValidadoresDisponiblesAsync(areaId, ct));

    // POST /api/asignaciones/denuncias/{denunciaId}/asignar   Body: { validadorId }
    // Si el cliente envia jefeAreaId se ignora: quien asigna es el usuario del token.
    [HttpPost("denuncias/{denunciaId:long}/asignar")]
    public async Task<ActionResult> AsignarValidador(
        long denunciaId, [FromBody] AsignarValidadorRequest request, CancellationToken ct)
    {
        await asignacionService.AsignarDenunciaAsync(denunciaId, request.ValidadorId, ct);
        return Ok(new { Mensaje = "Denuncia asignada correctamente." });
    }
}