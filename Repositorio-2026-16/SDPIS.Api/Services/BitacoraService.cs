using SDPIS.Api.Models.DTOs;
using SDPIS.Api.Repositories;
using SDPIS.Api.Services.Autorizacion;
using SDPIS.Api.Services.GestionUsuarios;
using SDPIS.Api.Services.Validation;

namespace SDPIS.Api.Services;

// HU-016. HU-015: el alcance ya no sale del rol guardado sino de los permisos
// efectivos del usuario (IPermisosService), igual que en el resto del sistema.
// Asi tambien aplica a los permisos delegados (p. ej. un funcionario con
// "Ver denuncias del area") y a los superusuarios en renuncia.
//  - alcance TODAS   (superusuario): cualquier denuncia.
//  - alcance AREA    (jefe o delegado): las de su area (area_actual_id).
//  - alcance PROPIAS (funcionario): las que tiene asignadas
//    (funcionario_asignado_id), como validador o como encargado.
// El funcionarioId que reciben estos metodos lo pone el controlador desde el
// token; nunca viene del cliente.
public class BitacoraService(IBitacoraRepository repository, IPermisosService permisos) : IBitacoraService
{
    public async Task RegistrarAsync(long denunciaId, long? funcionarioId, long? areaOrigenId,
        string tipoRegistro, string contenido, CancellationToken ct)
    {
        BitacoraValidator.ValidarRegistro(denunciaId, tipoRegistro, contenido);
        await repository.RegistrarAsync(
            new RegistroBitacora(denunciaId, funcionarioId, areaOrigenId, tipoRegistro, contenido.Trim()), ct);
    }

    public async Task<BitacoraPaginaDto> ConsultarAsync(long denunciaId, long funcionarioId,
        string? tipoRegistro, int pagina, int tamano, CancellationToken ct)
    {
        var tipo = string.IsNullOrWhiteSpace(tipoRegistro) ? null : tipoRegistro.Trim();
        BitacoraValidator.ValidarConsulta(denunciaId, funcionarioId, tipo, pagina, tamano);
        await ValidarAccesoAsync(denunciaId, funcionarioId, ct);

        var (items, total) = await repository.ListarPorDenunciaAsync(denunciaId, tipo, pagina, tamano, ct);
        return new BitacoraPaginaDto(items, pagina, tamano, total);
    }

    public async Task<BitacoraEntradaDto> AgregarComentarioAsync(long denunciaId,
        AgregarComentarioBitacoraRequest request, CancellationToken ct)
    {
        BitacoraValidator.ValidarComentario(denunciaId, request);
        var (actor, denuncia) = await ValidarAccesoAsync(denunciaId, request.FuncionarioId, ct);

        // HU-018: con la solicitud de cierre enviada, el funcionario ya no
        // puede agregar comentarios; la jefatura (alcance AREA/TODAS) si.
        if (denuncia.CierrePendiente && actor.Alcance == AlcanceDenuncias.Propias)
            throw new ReglaNegocioException(
                "La denuncia tiene una solicitud de cierre en revisión: ya no se pueden agregar comentarios.");

        return await repository.RegistrarAsync(
            new RegistroBitacora(denunciaId, actor.FuncionarioId,
                // Un funcionario con varias areas actua en el area de la denuncia, no en su base.
                actor.Alcance == AlcanceDenuncias.Propias ? denuncia.AreaActualId : actor.AreaId,
                TiposBitacora.Comentario, request.Contenido.Trim()), ct);
    }

    // Panel de busqueda de Bitacora. Usa el mismo alcance que
    // ValidarAccesoAsync, para que nunca se muestre en la lista una denuncia
    // que despues, al hacer clic, resulte con acceso denegado.
    public async Task<List<DenunciaBitacoraListadoDto>> ListarDenunciasVisiblesAsync(
        long funcionarioId, CancellationToken ct)
    {
        var actor = await ActorAsync(funcionarioId, ct);

        (long? AreaId, long? FuncionarioAsignadoId) filtro = actor.Alcance switch
        {
            AlcanceDenuncias.Todas => (null, null),
            AlcanceDenuncias.Area => (actor.AreaId, null),
            AlcanceDenuncias.Propias => (null, actor.FuncionarioId),
            _ => throw new BitacoraAccesoDenegadoException("No cuenta con permisos para consultar denuncias en la bitacora.")
        };

        return await repository.ListarDenunciasVisiblesAsync(filtro.AreaId, filtro.FuncionarioAsignadoId, ct);
    }

    private async Task<(ContextoPermisos Actor, DenunciaAccesoInfo Denuncia)> ValidarAccesoAsync(
        long denunciaId, long funcionarioId, CancellationToken ct)
    {
        var actor = await ActorAsync(funcionarioId, ct);

        var denuncia = await repository.ObtenerDenunciaAsync(denunciaId, ct)
            ?? throw new RecursoNoEncontradoException("La denuncia indicada no existe.");

        var permitido = actor.Alcance switch
        {
            AlcanceDenuncias.Todas => true,
            AlcanceDenuncias.Area => denuncia.AreaActualId == actor.AreaId,
            AlcanceDenuncias.Propias => denuncia.FuncionarioAsignadoId == actor.FuncionarioId,
            _ => false
        };

        if (!permitido)
            throw new BitacoraAccesoDenegadoException("No tiene permisos para acceder a la bitacora de esta denuncia.");

        return (actor, denuncia);
    }

    // Usuario activo con permiso de seguimiento (lo exige tambien el
    // controlador; aqui se repite porque el servicio puede usarse desde otros).
    private async Task<ContextoPermisos> ActorAsync(long funcionarioId, CancellationToken ct)
    {
        var actor = await permisos.ObtenerContextoAsync(funcionarioId, ct);
        if (actor is null || !actor.Activo || !actor.Tiene(Permisos.RegistrarSeguimiento))
            throw new BitacoraAccesoDenegadoException("El usuario no tiene acceso a la bitacora.");
        return actor;
    }
}