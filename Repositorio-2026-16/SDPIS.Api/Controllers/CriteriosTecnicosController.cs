using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SDPIS.Api.Models.DTOs;
using SDPIS.Api.Services;

namespace SDPIS.Api.Controllers;

// HU-010/011. HU-015: todos los endpoints exigen sesion ([Authorize]); el
// servicio toma el usuario del token y verifica que la denuncia este dentro
// de su alcance (403 si no, 404 si no existe).
[ApiController]
[Route("api/denuncias/{denunciaId:long}")]
[Authorize]
public class CriteriosTecnicosController(ICriteriosTecnicosService criteriosTecnicosService) : ControllerBase
{
    // POST /api/denuncias/{denunciaId}/criterios-tecnicos -- HU-010
    // Funcionario asignado a la denuncia (REGISTRAR_CRITERIOS) o superusuario.
    [HttpPost("criterios-tecnicos")]
    public async Task<ActionResult<CriteriosTecnicosRegistradosResponse>> Registrar(
        long denunciaId, [FromBody] RegistrarCriteriosTecnicosRequest request, CancellationToken ct)
    {
        var resultado = await criteriosTecnicosService.RegistrarAsync(denunciaId, request, ct);
        return StatusCode(StatusCodes.Status201Created, resultado);
    }

    // PUT /api/denuncias/{denunciaId}/criterios-tecnicos/{criterio} -- HU-010 (solo superusuario)
    // Agrega o actualiza (upsert) UN criterio individual. {criterio} es un
    // slug estable, no el nombre de catalogo (ver CriteriosTecnicosService.MapearClave).
    [HttpPut("criterios-tecnicos/{criterio:regex(^(dano-potencial-salud|criterio-especializado-uvc)$)}")]
    public async Task<ActionResult<CriterioTecnicoActualizadoResponse>> ActualizarCriterio(
        long denunciaId, string criterio, [FromBody] ActualizarCriterioTecnicoRequest request, CancellationToken ct)
    {
        var resultado = await criteriosTecnicosService.ActualizarCriterioAsync(denunciaId, criterio, request, ct);
        return Ok(resultado);
    }

    // PUT /api/denuncias/{denunciaId}/criterios-tecnicos/metadata -- HU-010 (solo superusuario)
    // Comercio electronico/observaciones no son "criterios" en el
    // modelo de datos (viven en evaluacion_criticidad, no en respuesta_criterio).
    [HttpPut("criterios-tecnicos/metadata")]
    public async Task<ActionResult<MetadataCriteriosTecnicosActualizadaResponse>> ActualizarMetadata(
        long denunciaId, [FromBody] ActualizarMetadataCriteriosTecnicosRequest request, CancellationToken ct)
    {
        var resultado = await criteriosTecnicosService.ActualizarMetadataAsync(denunciaId, request, ct);
        return Ok(resultado);
    }

    // GET /api/denuncias/{denunciaId}/criterios-tecnicos -- HU-010
    // Devuelve null si la denuncia aun no tiene criterios tecnicos registrados.
    [HttpGet("criterios-tecnicos")]
    public async Task<ActionResult<CriteriosTecnicosDto?>> Obtener(long denunciaId, CancellationToken ct)
    {
        var resultado = await criteriosTecnicosService.ObtenerAsync(denunciaId, ct);
        return Ok(resultado);
    }

    // GET /api/denuncias/{denunciaId}/alerta-sanitaria-vigente -- HU-010
    [HttpGet("alerta-sanitaria-vigente")]
    public async Task<ActionResult<AlertaSanitariaVigenteResponse>> ObtenerAlertaSanitariaVigente(
        long denunciaId, CancellationToken ct)
    {
        var resultado = await criteriosTecnicosService.ObtenerAlertaSanitariaVigenteAsync(denunciaId, ct);
        return Ok(resultado);
    }

    // GET /api/denuncias/{denunciaId}/detalle -- HU-010
    // Informacion registrada de la denuncia (hecho, ubicacion, productos y
    // motivos) para que el funcionario la consulte antes de seleccionar los
    // criterios tecnicos. Sin datos personales del denunciante.
    [HttpGet("detalle")]
    public async Task<ActionResult<DetalleDenunciaDto>> ObtenerDetalle(long denunciaId, CancellationToken ct)
    {
        var resultado = await criteriosTecnicosService.ObtenerDetalleDenunciaAsync(denunciaId, ct);
        return resultado is null
            ? NotFound(new { error = "La denuncia no existe." })
            : Ok(resultado);
    }
}