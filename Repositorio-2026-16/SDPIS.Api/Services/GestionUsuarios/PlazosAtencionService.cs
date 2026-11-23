using SDPIS.Api.Models.DTOs;
using SDPIS.Api.Repositories;
using SDPIS.Api.Services.Autorizacion;
using SDPIS.Api.Services.Validation;

namespace SDPIS.Api.Services.GestionUsuarios;

public interface IPlazosAtencionService
{
    Task<List<PlazoCriticidadDto>> ListarAsync(CancellationToken ct);
    Task<PlazoCriticidadDto> ActualizarPlazoAsync(string nivel, ActualizarPlazoRequest request, CancellationToken ct);
}

// HU-014: el superusuario (GESTIONAR_PLAZOS) define los dias de atencion de
// cada nivel de criticidad. Reglas:
//   * Alto debe tener un plazo menor que medio y que bajo.
//   * El cambio aplica a las evaluaciones de criticidad que se calculen de
//     ahi en adelante (EvaluacionCriticidadService lee los rangos en cada
//     calculo); las fechas limite ya calculadas no se modifican.
//   * Cada cambio queda en bitacora_administrativa (categoria PLAZOS), en la
//     misma transaccion que el UPDATE.
public class PlazosAtencionService(
    IPlazosAtencionRepository repositorio,
    IPermisosService permisos,
    IUsuarioActual usuario) : IPlazosAtencionService
{
    private const string CategoriaPlazos = "PLAZOS";

    public async Task<List<PlazoCriticidadDto>> ListarAsync(CancellationToken ct)
    {
        await ActorAsync(ct);
        return await repositorio.ListarAsync(ct);
    }

    public async Task<PlazoCriticidadDto> ActualizarPlazoAsync(
        string nivel, ActualizarPlazoRequest request, CancellationToken ct)
    {
        PlazosAtencionValidator.ValidarActualizar(nivel, request);
        var nivelNormalizado = nivel.Trim().ToUpperInvariant();
        var actor = await ActorAsync(ct);

        await using var conexion = await repositorio.AbrirConexionAsync(ct);
        await using var transaccion = conexion.BeginTransaction();
        try
        {
            var actuales = await repositorio.BloquearTodosAsync(conexion, transaccion, ct);
            var actual = actuales.FirstOrDefault(p => p.Nivel == nivelNormalizado)
                ?? throw new RecursoNoEncontradoException("El nivel de criticidad indicado no existe.");

            ValidarOrden(actuales, nivelNormalizado, request.PlazoDias);

            if (request.PlazoDias != actual.PlazoDias)
            {
                await repositorio.ActualizarPlazoAsync(conexion, transaccion, nivelNormalizado, request.PlazoDias, ct);
                await repositorio.RegistrarBitacoraAsync(conexion, transaccion, new RegistroBitacoraAdministrativa(
                    actor.FuncionarioId, null, null, CategoriaPlazos,
                    $"Plazo de atención de criticidad {EtiquetaNivel(nivelNormalizado)} actualizado.",
                    $"plazo_dias={actual.PlazoDias}", $"plazo_dias={request.PlazoDias}"), ct);
            }

            await transaccion.CommitAsync(ct);
            return actual with { PlazoDias = request.PlazoDias };
        }
        catch
        {
            await transaccion.RollbackAsync(ct);
            throw;
        }
    }

    // Alto debe quedar estrictamente por debajo de medio y de bajo.
    private static void ValidarOrden(IReadOnlyList<PlazoCriticidadDto> actuales, string nivel, int nuevo)
    {
        int Plazo(string n) => n == nivel ? nuevo : actuales.First(p => p.Nivel == n).PlazoDias;

        var alto = Plazo("ALTO");
        if (alto >= Plazo("MEDIO") || alto >= Plazo("BAJO"))
            throw new ReglaNegocioException(
                "El plazo de criticidad alta debe ser menor que el de criticidad media y baja.");
    }

    private static string EtiquetaNivel(string nivel) => nivel switch
    {
        "ALTO" => "alta",
        "MEDIO" => "media",
        _ => "baja",
    };

    private async Task<ContextoPermisos> ActorAsync(CancellationToken ct)
    {
        var actor = await permisos.ObtenerContextoAsync(usuario.FuncionarioId, ct);
        if (actor is null || !actor.Activo)
            throw new PermisoDenegadoException("El usuario que realiza la accion no existe o esta inactivo.");
        if (!actor.Tiene(Permisos.GestionarPlazos))
            throw new PermisoDenegadoException("No cuenta con permisos para gestionar los plazos de atencion.");
        return actor;
    }
}
