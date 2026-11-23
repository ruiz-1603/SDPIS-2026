using SDPIS.Api.Models.DTOs;
using SDPIS.Api.Repositories;
using SDPIS.Api.Services.Autorizacion;
using SDPIS.Api.Services.Validation;

namespace SDPIS.Api.Services.GestionUsuarios;

// Resultado de aplicar la regla de visibilidad: a lo sumo uno de los dos
// filtros viene con valor; ambos null = todas las areas (superusuario).
public record AlcanceResuelto(string Alcance, long? AreaId, long? FuncionarioAsignadoId);

// HU-015 / HU-016: UNICO punto que decide que denuncias ve cada usuario.
//   validador       -> solo las asignadas a el (PROPIAS)
//   jefe / delegado -> todas las de su area (AREA)
//   superusuario    -> todas, opcionalmente filtradas por area (TODAS)
// Las pantallas existentes (bitacora, criterios, encargado, asignaciones,
// dashboard) deben pedir sus listados a traves de este servicio.
public interface IVisibilidadDenunciasService
{
    // areaSolicitada: solo la puede usar quien tiene alcance TODAS; si un
    // jefe pide otra area distinta a la suya -> 403.
    Task<AlcanceResuelto> ResolverAlcanceAsync(long? areaSolicitada, CancellationToken ct);

    // filtro: TODAS | POR_ASIGNAR | ASIGNADAS
    // criterios (opcional): REGISTRADOS | PENDIENTES
    Task<List<DenunciaVisibleDto>> ListarAsync(long? areaSolicitada, string? filtro, CancellationToken ct,
        string? criterios = null);
    // Para pantallas OPERATIVAS de un area (bandejas de asignacion,
    // encargado, disponibilidad): exige el permiso indicado y devuelve el
    // area sobre la que puede trabajar. Quien tiene VER_TODAS_AREAS puede
    // elegir cualquier area; el resto solo la suya (si pide otra -> 403).
    // Ej.: un validador con ASIGNAR_DENUNCIAS delegado trabaja en la
    // bandeja de SU area aunque su alcance de dashboard sea PROPIAS.
    Task<long> ResolverAreaOperativaAsync(long? areaSolicitada, string permisoRequerido, CancellationToken ct);
    // Para pantallas que trabajan sobre UNA denuncia (criterios tecnicos,
    // bitacora, detalle): 404 si no existe, 403 si esta fuera del alcance del
    // usuario. Devuelve area y funcionario asignado para reglas adicionales.
    Task<DenunciaAlcanceRegistro> VerificarAccesoDenunciaAsync(long denunciaId, CancellationToken ct);
}

public class VisibilidadDenunciasService(
    IPermisosService permisos,
    IUsuarioActual usuario,
    IGestionUsuariosRepository repositorio) : IVisibilidadDenunciasService
{
    public async Task<AlcanceResuelto> ResolverAlcanceAsync(long? areaSolicitada, CancellationToken ct)
    {
        var actor = await permisos.ObtenerContextoAsync(usuario.FuncionarioId, ct);
        if (actor is null || !actor.Activo)
            throw new PermisoDenegadoException("El usuario no existe o esta inactivo.");

        switch (actor.Alcance)
        {
            case AlcanceDenuncias.Todas:
                return new AlcanceResuelto(actor.Alcance, areaSolicitada, null);

            case AlcanceDenuncias.Area:
                if (areaSolicitada.HasValue && areaSolicitada.Value != actor.AreaId)
                    throw new PermisoDenegadoException("Solo puede consultar denuncias de su propia area.");
                return new AlcanceResuelto(actor.Alcance, actor.AreaId, null);

            case AlcanceDenuncias.Propias:
                return new AlcanceResuelto(actor.Alcance, null, actor.FuncionarioId);

            default:
                throw new PermisoDenegadoException("No cuenta con permisos para consultar denuncias.");
        }
    }

    public async Task<long> ResolverAreaOperativaAsync(long? areaSolicitada, string permisoRequerido, CancellationToken ct)
    {
        var actor = await permisos.ObtenerContextoAsync(usuario.FuncionarioId, ct);
        if (actor is null || !actor.Activo || !actor.Tiene(permisoRequerido))
            throw new PermisoDenegadoException("No cuenta con permisos para realizar esta accion.");

        if (actor.Tiene(Permisos.VerTodasAreas))
            return areaSolicitada ?? actor.AreaId;

        if (areaSolicitada.HasValue && areaSolicitada.Value != actor.AreaId)
            throw new PermisoDenegadoException("Solo puede trabajar con su propia area.");
        return actor.AreaId;
    }
    public async Task<DenunciaAlcanceRegistro> VerificarAccesoDenunciaAsync(long denunciaId, CancellationToken ct)
    {
        var denuncia = await repositorio.ObtenerAlcanceDenunciaAsync(denunciaId, ct)
            ?? throw new RecursoNoEncontradoException("La denuncia no existe.");

        var alcance = await ResolverAlcanceAsync(null, ct);
        var permitido = alcance.Alcance switch
        {
            AlcanceDenuncias.Todas => true,
            AlcanceDenuncias.Area => denuncia.AreaActualId == alcance.AreaId,
            AlcanceDenuncias.Propias => denuncia.FuncionarioAsignadoId == alcance.FuncionarioAsignadoId,
            _ => false,
        };
        if (!permitido)
            throw new PermisoDenegadoException("La denuncia no está dentro de lo que usted puede consultar.");

        return denuncia;
    }
    public async Task<List<DenunciaVisibleDto>> ListarAsync(long? areaSolicitada, string? filtro, CancellationToken ct,
    string? criterios = null)
    {
        var filtroNormalizado = string.IsNullOrWhiteSpace(filtro) ? "TODAS" : filtro.Trim().ToUpperInvariant();
        GestionUsuariosValidator.ValidarFiltroDenuncias(filtroNormalizado);

        var criteriosNormalizado = string.IsNullOrWhiteSpace(criterios) ? null : criterios.Trim().ToUpperInvariant();
        if (criteriosNormalizado is not null and not ("REGISTRADOS" or "PENDIENTES"))
            throw new DenunciaValidationException(["El filtro de criterios debe ser REGISTRADOS o PENDIENTES."]);

        var alcance = await ResolverAlcanceAsync(areaSolicitada, ct);
        return await repositorio.ListarDenunciasAsync(
            new FiltroDenuncias(alcance.AreaId, alcance.FuncionarioAsignadoId, filtroNormalizado, criteriosNormalizado), ct);
    }
}
