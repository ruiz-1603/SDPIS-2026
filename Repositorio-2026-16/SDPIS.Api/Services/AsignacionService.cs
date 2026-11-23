using SDPIS.Api.Models.DTOs;
using SDPIS.Api.Repositories;
using SDPIS.Api.Services.Autorizacion;
using SDPIS.Api.Services.GestionUsuarios;
using SDPIS.Api.Services.Validation;

namespace SDPIS.Api.Services;

// HU-009 + HU-015. El usuario sale del token y su alcance de los permisos:
//   VER_TODAS_AREAS (superusuario) -> cualquier area; areaId null = todas.
//   resto (jefe o delegado con ASIGNAR_DENUNCIAS) -> solo su area.
// La asignacion valida aqui lo que el procedimiento no valida: que la
// denuncia este en el alcance del usuario y que el validador pertenezca al
// area de la denuncia, este activo y sea funcionario (no jefe).
public class AsignacionService(
    IAsignacionRepository repository,
    IGestionUsuariosRepository gestionRepository,
    IPermisosService permisos,
    IUsuarioActual usuario) : IAsignacionService
{
    public async Task<List<DenunciaPendienteDto>> ObtenerDenunciasPendientesAsync(long? areaId, CancellationToken ct)
        => await repository.ObtenerDenunciasPendientesAsignacionAsync(await ResolverAreaAsync(areaId, ct), ct);

    public async Task<List<DenunciaAsignadaDto>> ObtenerDenunciasAsignadasAsync(long? areaId, CancellationToken ct)
        => await repository.ObtenerDenunciasAsignadasAsync(await ResolverAreaAsync(areaId, ct), ct);

    public async Task<List<ValidadorCargaDto>> ObtenerValidadoresDisponiblesAsync(long? areaId, CancellationToken ct)
    {
        var area = await ResolverAreaAsync(areaId, ct)
            ?? throw new DenunciaValidationException(["Indique el area de la denuncia para listar sus validadores."]);
        return await repository.ObtenerValidadoresDisponiblesConCargaAsync(area, ct);
    }

    public async Task AsignarDenunciaAsync(long denunciaId, long validadorId, CancellationToken ct)
    {
        if (validadorId <= 0)
            throw new DenunciaValidationException(["Debe seleccionar un validador."]);

        var actor = await ActorAsync(ct);

        var denuncia = await gestionRepository.ObtenerAlcanceDenunciaAsync(denunciaId, ct)
            ?? throw new RecursoNoEncontradoException("La denuncia no existe.");
        if (!actor.Tiene(Permisos.VerTodasAreas) && denuncia.AreaActualId != actor.AreaId)
            throw new PermisoDenegadoException("Solo puede asignar denuncias de su propia area.");

        var validador = await gestionRepository.ObtenerFuncionarioAsync(validadorId, ct)
            ?? throw new RecursoNoEncontradoException("El validador indicado no existe.");
        if (validador.Rol != Roles.Validador)
            throw new ReglaNegocioException("Solo se puede asignar la denuncia a un funcionario (un jefe de area no recibe denuncias).");
        if (!validador.Activo)
            throw new ReglaNegocioException("No se puede asignar la denuncia a un funcionario inactivo.");
        // Pertenece si es su area base o una area adicional que le otorgo un superusuario.
        if (validador.AreaId != denuncia.AreaActualId
            && !await gestionRepository.FuncionarioPerteneceAAreaAsync(validadorId, denuncia.AreaActualId, ct))
            throw new ReglaNegocioException("El validador debe pertenecer al area de la denuncia.");

        // Quien asigna (bitacora y notificacion) es el usuario autenticado.
        await repository.AsignarValidadorManualAsync(denunciaId, validadorId, actor.FuncionarioId, ct);
    }

    // null = todas las areas (solo con VER_TODAS_AREAS).
    private async Task<long?> ResolverAreaAsync(long? areaSolicitada, CancellationToken ct)
    {
        var actor = await ActorAsync(ct);
        if (actor.Tiene(Permisos.VerTodasAreas))
            return areaSolicitada;

        if (areaSolicitada.HasValue && areaSolicitada.Value != actor.AreaId)
            throw new PermisoDenegadoException("Solo puede consultar las denuncias de su propia area.");
        return actor.AreaId;
    }

    private async Task<ContextoPermisos> ActorAsync(CancellationToken ct)
    {
        var actor = await permisos.ObtenerContextoAsync(usuario.FuncionarioId, ct);
        if (actor is null || !actor.Activo || !actor.Tiene(Permisos.AsignarDenuncias))
            throw new PermisoDenegadoException("No cuenta con permisos para asignar denuncias.");
        return actor;
    }
}