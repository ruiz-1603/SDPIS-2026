using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SDPIS.Api.Models.DTOs;
using SDPIS.Api.Repositories;
using SDPIS.Api.Services.Autenticacion;

namespace SDPIS.Api.Services.Autorizacion;

// ======================================================================
// Usuario autenticado: el actor SIEMPRE sale del token, nunca del body ni
// de la query (evita suplantar a otro funcionario).
// ======================================================================
public interface IUsuarioActual
{
    long FuncionarioId { get; }
}

public class UsuarioActual(IHttpContextAccessor accesor) : IUsuarioActual
{
    public long FuncionarioId
    {
        get
        {
            var valor = accesor.HttpContext?.User.FindFirstValue(ClaimsSesion.FuncionarioId);
            return long.TryParse(valor, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id)
                ? id
                : throw new UnauthorizedAccessException("No autorizado. Debe iniciar sesion.");
        }
    }
}

// ======================================================================
// Permisos efectivos. Se consultan en la BD en cada peticion (no se
// guardan en el JWT): una revocacion o desactivacion rige de inmediato.
// Cache solo dentro de la misma peticion (servicio Scoped).
// ======================================================================
public interface IPermisosService
{
    Task<ContextoPermisos?> ObtenerContextoAsync(long funcionarioId, CancellationToken ct);
    Task<bool> TieneAsync(long funcionarioId, string codigo, CancellationToken ct);
    Task<PermisosSesionDto> ObtenerSesionAsync(long funcionarioId, CancellationToken ct);
    void Invalidar(long funcionarioId);
}

public class PermisosService(IPermisosRepository repositorio) : IPermisosService
{
    private readonly Dictionary<long, ContextoPermisos?> _cache = new();

    public async Task<ContextoPermisos?> ObtenerContextoAsync(long funcionarioId, CancellationToken ct)
    {
        if (_cache.TryGetValue(funcionarioId, out var enCache))
            return enCache;

        var info = await repositorio.ObtenerAsync(funcionarioId, ct);
        var contexto = info is null ? null : CatalogoPermisos.Construir(info);
        _cache[funcionarioId] = contexto;
        return contexto;
    }

    public async Task<bool> TieneAsync(long funcionarioId, string codigo, CancellationToken ct)
        => (await ObtenerContextoAsync(funcionarioId, ct))?.Tiene(codigo) == true;

    public async Task<PermisosSesionDto> ObtenerSesionAsync(long funcionarioId, CancellationToken ct)
    {
        var contexto = await ObtenerContextoAsync(funcionarioId, ct);
        return contexto is null
            ? new PermisosSesionDto(Array.Empty<string>(), AlcanceDenuncias.Ninguna)
            : new PermisosSesionDto(contexto.Efectivos.OrderBy(p => p, StringComparer.Ordinal).ToList(), contexto.Alcance);
    }

    public void Invalidar(long funcionarioId) => _cache.Remove(funcionarioId);
}

// ======================================================================
// [RequierePermiso(Permisos.X)]: primera barrera en el controlador.
// Usar junto con [Authorize]. Las reglas finas (area, cupos, etc.) las
// valida el servicio.
// ======================================================================
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequierePermisoAttribute : TypeFilterAttribute
{
    public RequierePermisoAttribute(string codigo) : base(typeof(RequierePermisoFilter))
        => Arguments = new object[] { codigo };
}

public sealed class RequierePermisoFilter(string codigo, IPermisosService permisos) : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext contexto)
    {
        var usuario = contexto.HttpContext.User;
        if (usuario.Identity?.IsAuthenticated != true)
        {
            contexto.Result = new UnauthorizedObjectResult(new
            {
                error = "token_invalido",
                mensaje = "No autorizado. Debe iniciar sesion."
            });
            return;
        }

        var claim = usuario.FindFirstValue(ClaimsSesion.FuncionarioId);
        if (!long.TryParse(claim, NumberStyles.Integer, CultureInfo.InvariantCulture, out var funcionarioId)
            || !await permisos.TieneAsync(funcionarioId, codigo, contexto.HttpContext.RequestAborted))
        {
            contexto.Result = new ObjectResult(new { error = "No cuenta con permisos para realizar esta accion." })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}
