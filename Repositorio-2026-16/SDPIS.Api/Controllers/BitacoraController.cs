using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SDPIS.Api.Models.DTOs;
using SDPIS.Api.Services;
using SDPIS.Api.Services.Autorizacion;

namespace SDPIS.Api.Controllers;

// HU-016 + HU-015: todos los endpoints exigen sesion y el permiso de
// seguimiento. El funcionario que consulta o comenta sale SIEMPRE del token
// (IUsuarioActual), nunca de la URL ni del body: asi un funcionario no puede
// hacerse pasar por su jefe para ver denuncias ajenas.
[ApiController]
[Route("api/denuncias/{denunciaId:long}/bitacora")]
[Authorize]
[RequierePermiso(Permisos.RegistrarSeguimiento)]
public class BitacoraController(IBitacoraService bitacoraService, IUsuarioActual usuario) : ControllerBase
{
    // GET /api/denuncias/{denunciaId}/bitacora?tipo={tipo}&pagina=1&tamano=20 -- HU-016
    // Orden cronologico ascendente. 403 si la denuncia esta fuera del alcance
    // del usuario (funcionario: sus asignadas; jefe: su area; superusuario: todas).
    [HttpGet]
    public async Task<ActionResult<BitacoraPaginaDto>> Consultar(
        long denunciaId, [FromQuery] string? tipo,
        CancellationToken ct, [FromQuery] int pagina = 1, [FromQuery] int tamano = 20)
    {
        var resultado = await bitacoraService.ConsultarAsync(
            denunciaId, usuario.FuncionarioId, tipo, pagina, tamano, ct);
        return Ok(resultado);
    }

    // POST /api/denuncias/{denunciaId}/bitacora -- HU-016: comentario manual (tipo "comentario")
    // Body: { contenido }. Si el cliente envia funcionarioId se ignora.
    [HttpPost]
    public async Task<ActionResult<BitacoraEntradaDto>> AgregarComentario(
        long denunciaId, [FromBody] AgregarComentarioBitacoraRequest request, CancellationToken ct)
    {
        var entrada = await bitacoraService.AgregarComentarioAsync(
            denunciaId, request with { FuncionarioId = usuario.FuncionarioId }, ct);
        return StatusCode(StatusCodes.Status201Created, entrada);
    }

    // GET /api/bitacora/denuncias -- HU-016
    // Panel izquierdo de la pantalla de Bitacora: denuncias visibles para el
    // usuario autenticado segun su alcance. Reemplaza a
    // /api/funcionarios/{funcionarioId}/bitacora/denuncias, que permitia pedir
    // el listado de cualquier funcionario cambiando el id de la URL.
    [HttpGet("/api/bitacora/denuncias")]
    public async Task<ActionResult<List<DenunciaBitacoraListadoDto>>> ListarDenunciasVisibles(CancellationToken ct)
    {
        var denuncias = await bitacoraService.ListarDenunciasVisiblesAsync(usuario.FuncionarioId, ct);
        return Ok(denuncias);
    }
}