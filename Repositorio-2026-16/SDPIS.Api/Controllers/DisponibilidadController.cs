using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SDPIS.Api.Models.DTOs;
using SDPIS.Api.Services;
using SDPIS.Api.Services.Autorizacion;
using SDPIS.Api.Services.GestionUsuarios;

namespace SDPIS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DisponibilidadController(
    IDisponibilidadService disponibilidadService,
    IPermisosService permisos,
    IUsuarioActual usuario) : ControllerBase
{
    // POST /api/disponibilidad -- HU-007: registrar periodo de indisponibilidad
    [HttpPost]
    public async Task<ActionResult<PeriodoIndisponibilidadDto>> Registrar(
        [FromBody] RegistrarIndisponibilidadRequest request, CancellationToken ct)
    {
        var periodo = await disponibilidadService.RegistrarPeriodoAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, periodo);
    }

    // PUT /api/disponibilidad/periodos/{idPeriodo} -- HU-007 actualizar periodo de indisponibilidad
    [HttpPut("periodos/{idPeriodo:long}")]
    public async Task<ActionResult<PeriodoIndisponibilidadDto>> Actualizar(
        long idPeriodo, [FromBody] ActualizarIndisponibilidadRequest request, CancellationToken ct)
    {
        if (idPeriodo != request.IdPeriodo)
        {
            return BadRequest(new { error = "El identificador del periodo no coincide con la ruta." });
        }

        var periodo = await disponibilidadService.ActualizarPeriodoAsync(request, ct);
        return Ok(periodo);
    }

    // DELETE /api/disponibilidad/periodos/{idPeriodo}?funcionarioId={id} -- HU-007 eliminar periodo de indisponibilidad
    [HttpDelete("periodos/{idPeriodo:long}")]
    public async Task<ActionResult> Eliminar(
        long idPeriodo, [FromQuery] long funcionarioId, CancellationToken ct)
    {
        await disponibilidadService.EliminarPeriodoAsync(idPeriodo, funcionarioId, ct);
        return NoContent();
    }

    // GET /api/disponibilidad/funcionarios -- HU-007 resumen de disponibilidad.
    // HU-015: superusuario (VER_TODAS_AREAS) -> funcionarios de todas las
    // areas; jefe o delegado con GESTIONAR_DISPONIBILIDAD -> solo su area.
    [HttpGet("funcionarios")]
    [Authorize]
    [RequierePermiso(Permisos.GestionarDisponibilidad)]
    public async Task<ActionResult<List<FuncionarioDisponibilidadResumenDto>>> ObtenerFuncionarios(
        CancellationToken ct)
    {
        var actor = await permisos.ObtenerContextoAsync(usuario.FuncionarioId, ct)
            ?? throw new PermisoDenegadoException("El usuario no existe o esta inactivo.");
        long? areaId = actor.Tiene(Permisos.VerTodasAreas) ? null : actor.AreaId;

        var funcionarios = await disponibilidadService.ObtenerResumenFuncionariosAsync(areaId, ct);
        return Ok(funcionarios);
    }

    // GET /api/disponibilidad/funcionarios/{id} -- HU-007 resumen de disponibilidad de un funcionario específico
    [HttpGet("funcionarios/{id:long}")]
    public async Task<ActionResult<DisponibilidadFuncionarioDto>> ObtenerDisponibilidad(
        long id, CancellationToken ct)
    {
        var disponibilidad = await disponibilidadService.ObtenerDisponibilidadAsync(id, ct);
        return Ok(disponibilidad);
    }
}