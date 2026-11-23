using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SDPIS.Api.Models.DTOs;
using SDPIS.Api.Services.Autorizacion;
using SDPIS.Api.Services.GestionUsuarios;

namespace SDPIS.Api.Controllers;

// HU-014: configuracion de los plazos de atencion por nivel de criticidad.
// Solo quien tenga GESTIONAR_PLAZOS (superusuario). El actor sale del token.
[ApiController]
[Route("api/plazos-atencion")]
[Authorize]
public class PlazosAtencionController(IPlazosAtencionService servicio) : ControllerBase
{
    [HttpGet]
    [RequierePermiso(Permisos.GestionarPlazos)]
    public async Task<ActionResult<List<PlazoCriticidadDto>>> Listar(CancellationToken ct)
        => Ok(await servicio.ListarAsync(ct));

    // Body: { plazoDias }. nivel: ALTO | MEDIO | BAJO
    [HttpPut("{nivel}")]
    [RequierePermiso(Permisos.GestionarPlazos)]
    public async Task<ActionResult<PlazoCriticidadDto>> Actualizar(
        string nivel, [FromBody] ActualizarPlazoRequest request, CancellationToken ct)
        => Ok(await servicio.ActualizarPlazoAsync(nivel, request, ct));
}
