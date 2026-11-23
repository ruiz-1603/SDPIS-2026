using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SDPIS.Api.Models.DTOs;
using SDPIS.Api.Services.Autenticacion;

namespace SDPIS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AutenticacionController(IAutenticacionService autenticacionService) : ControllerBase
{
    // HU-001: inicio de sesion. 200 con token y datos del usuario = exito;
    // 400 solicitud mal formada; 401 credenciales invalidas; 403 cuenta
    // desactivada o rol no configurado (los traduce el middleware).
    [HttpPost("iniciar-sesion")]
    public async Task<ActionResult<IniciarSesionResponse>> IniciarSesion(
        [FromBody] IniciarSesionRequest request, CancellationToken ct)
        => Ok(await autenticacionService.IniciarSesionAsync(request, ct));

    // HU-001: valida un token guardado al recargar la aplicacion y devuelve
    // los datos de la sesion. Requiere token valido ([Authorize]); un token
    // expirado responde 401 "sesion_expirada" (OnChallenge del JwtBearer).
    [HttpGet("sesion")]
    [Authorize]
    public ActionResult<UsuarioAutenticadoDto> ObtenerSesion()
    {
        var id = ObtenerClaim(JwtRegisteredClaimNames.Sub);
        var identificacion = ObtenerClaim(ClaimsSesion.Identificacion);
        var rol = ObtenerClaim(ClaimsSesion.Rol);
        var funcionarioId = ObtenerClaim(ClaimsSesion.FuncionarioId);
        var areaId = ObtenerClaim(ClaimsSesion.AreaId);

        if (id is null || identificacion is null || rol is null || funcionarioId is null || areaId is null)
            return Unauthorized(new { error = "token_invalido" });

        return Ok(new UsuarioAutenticadoDto(
            long.Parse(id, System.Globalization.CultureInfo.InvariantCulture),
            identificacion,
            ObtenerClaim(ClaimsSesion.Nombre) ?? "",
            ObtenerClaim(ClaimsSesion.Apellido1) ?? "",
            ObtenerClaim(ClaimsSesion.Apellido2),
            ObtenerClaim(ClaimsSesion.Correo),
            rol,
            long.Parse(funcionarioId, System.Globalization.CultureInfo.InvariantCulture),
            long.Parse(areaId, System.Globalization.CultureInfo.InvariantCulture)));
    }

    // HU-028: cierre de sesion manual o por inactividad (?motivo=inactividad).
    // Requiere token vigente ([Authorize]): el backend revoca ese JWT en
    // token_revocado y registra el evento en bitacora_sesion. El frontend
    // limpia la sesion local despues de la respuesta 200. Un token ya revocado
    // responde 401 en el OnChallenge del JwtBearer, antes de llegar aqui.
    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult<ResultadoCierreSesion>> CerrarSesion(
        [FromQuery] string? motivo, CancellationToken ct)
        => Ok(await autenticacionService.CerrarSesionAsync(
            string.Equals(motivo, "inactividad", StringComparison.OrdinalIgnoreCase), ct));

    private string? ObtenerClaim(string tipo) => User.FindFirstValue(tipo);
}