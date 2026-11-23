using SDPIS.Api.Repositories;
using SDPIS.Api.Services.Autenticacion;
using SDPIS.Api.Services.GestionUsuarios;
using SDPIS.Api.Services.Validation;

namespace SDPIS.Api.Middleware;

// HU-015: se agregan PermisoDenegado (403), RecursoNoEncontrado (404),
// ReglaNegocio (422) y UnauthorizedAccess (401, token sin funcionarioId).
// El resto queda igual que antes.
public class ManejadorDeErroresMiddleware(RequestDelegate next, ILogger<ManejadorDeErroresMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (DenunciaValidationException ex)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { errores = ex.Errores });
        }
        catch (AutenticacionValidationException ex)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { errores = ex.Errores });
        }
        catch (CredencialesInvalidasException ex)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "token_invalido", mensaje = ex.Message });
        }
        catch (CuentaDeshabilitadaException ex)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { error = ex.Message });
        }
        catch (BitacoraAccesoDenegadoException ex)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { error = ex.Message });
        }
        catch (PermisoDenegadoException ex)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { error = ex.Message });
        }
        catch (RecursoNoEncontradoException ex)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsJsonAsync(new { error = ex.Message });
        }
        catch (ReglaNegocioException ex)
        {
            context.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
            await context.Response.WriteAsJsonAsync(new { error = ex.Message });
        }
        catch (DenunciaBaseDatosException ex)
        {
            logger.LogWarning(ex, "Error de negocio reportado por la base de datos");
            context.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
            await context.Response.WriteAsJsonAsync(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error no controlado");
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new { error = "Ocurrio un error inesperado. Intente nuevamente." });
        }
    }
}
