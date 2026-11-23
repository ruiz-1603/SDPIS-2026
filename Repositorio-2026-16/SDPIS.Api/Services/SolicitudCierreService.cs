using SDPIS.Api.Models.DTOs;
using SDPIS.Api.Repositories;
using SDPIS.Api.Services.Autorizacion;
using SDPIS.Api.Services.GestionUsuarios;
using SDPIS.Api.Services.Validation;

namespace SDPIS.Api.Services;

// HU-018. El funcionario sale SIEMPRE del token (IUsuarioActual), nunca del
// body. Reglas:
//   * Solicitar: el funcionario responsable asignado a la denuncia
//     (funcionario_asignado_id, el mismo criterio de la bitacora), con
//     SOLICITAR_CIERRE, y solo si la denuncia esta en curso.
//   * Consultar el estado: cualquiera que pueda ver la denuncia
//     (IVisibilidadDenunciasService).
//   * Bandeja de pendientes: la jefatura (APROBAR_CIERRE) ve su area; el
//     superusuario (VER_TODAS_AREAS), todas.
// Las mismas reglas las vuelve a validar sp_solicitar_cierre_denuncia dentro
// de la transaccion (por si la denuncia cambio entre la lectura y el SP).
public class SolicitudCierreService(
    ISolicitudCierreRepository repository,
    IUsuarioActual usuario,
    IPermisosService permisos,
    IVisibilidadDenunciasService visibilidad) : ISolicitudCierreService
{
    public const string EstadoEnRevision = "EN_REVISION";

    // Estados "en curso": los unicos desde los que se puede solicitar el cierre.
    private static readonly HashSet<string> EstadosEnCurso = new(StringComparer.Ordinal)
    {
        "EN_TIEMPO", "PRONTA_VENCER", "ATRASADA"
    };

    public async Task<EstadoSolicitudCierreDto> ObtenerEstadoAsync(long denunciaId, CancellationToken ct)
    {
        await visibilidad.VerificarAccesoDenunciaAsync(denunciaId, ct);
        var actor = await permisos.ObtenerContextoAsync(usuario.FuncionarioId, ct);

        var denuncia = await repository.ObtenerDenunciaAsync(denunciaId, ct)
            ?? throw new RecursoNoEncontradoException("La denuncia no existe.");
        var pendiente = await repository.ObtenerPendienteAsync(denunciaId, ct);

        var motivo = MotivoNoPuedeSolicitar(actor, denuncia);
        return new EstadoSolicitudCierreDto(motivo is null, motivo, pendiente,
            denuncia.CarpetaConfidencialUrl, denuncia.CarpetaNoConfidencialUrl);
    }

    public async Task<SolicitudCierreDto> SolicitarAsync(long denunciaId, SolicitarCierreRequest request, CancellationToken ct)
    {
        SolicitudCierreValidator.ValidarSolicitud(denunciaId, request);

        var actor = await permisos.ObtenerContextoAsync(usuario.FuncionarioId, ct);
        var denuncia = await repository.ObtenerDenunciaAsync(denunciaId, ct)
            ?? throw new RecursoNoEncontradoException("La denuncia no existe.");

        if (!EsResponsable(actor, denuncia))
            throw new PermisoDenegadoException(
                "Solo el funcionario responsable asignado a la denuncia puede solicitar su cierre.");

        var motivo = MotivoNoPuedeSolicitar(actor, denuncia);
        if (motivo is not null)
            throw new ReglaNegocioException(motivo);

        var normalizada = new SolicitarCierreRequest(
            request.ResolucionFinal.Trim(),
            request.ObservacionesFinales.Trim(),
            request.AccionesRealizadas.Trim());

        var idSolicitud = await repository.SolicitarAsync(denunciaId, actor!.FuncionarioId, normalizada, ct);
        return await repository.ObtenerAsync(idSolicitud, ct)
            ?? throw new InvalidOperationException("No se pudo leer la solicitud de cierre recien registrada.");
    }

    public async Task<List<SolicitudCierreDto>> ListarPendientesAsync(CancellationToken ct)
    {
        var actor = await permisos.ObtenerContextoAsync(usuario.FuncionarioId, ct);
        if (actor is null || !actor.Activo)
            throw new PermisoDenegadoException("El usuario no existe o esta inactivo.");

        if (actor.Tiene(Permisos.VerTodasAreas))
            return await repository.ListarPendientesAsync(null, ct);
        if (actor.Tiene(Permisos.AprobarCierre))
            return await repository.ListarPendientesAsync(actor.AreaId, ct);

        throw new PermisoDenegadoException("Solo la jefatura de área puede consultar las solicitudes de cierre.");
    }

    private static bool EsResponsable(ContextoPermisos? actor, DenunciaCierreInfo denuncia)
        => actor is { Activo: true }
           && actor.Tiene(Permisos.SolicitarCierre)
           && denuncia.FuncionarioAsignadoId == actor.FuncionarioId;

    // null = puede solicitar. El texto se muestra tal cual en la interfaz.
    private static string? MotivoNoPuedeSolicitar(ContextoPermisos? actor, DenunciaCierreInfo denuncia)
    {
        if (!EsResponsable(actor, denuncia))
            return "Solo el funcionario responsable asignado a la denuncia puede solicitar su cierre.";
        if (denuncia.Estado == EstadoEnRevision)
            return "La denuncia ya tiene una solicitud de cierre pendiente de revisión por la jefatura.";
        if (!EstadosEnCurso.Contains(denuncia.Estado))
            return "Solo se puede solicitar el cierre de una denuncia en curso (en tiempo, próxima a vencer o atrasada).";
        return null;
    }
}
