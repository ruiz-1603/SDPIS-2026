using SDPIS.Api.Models.DTOs;
using SDPIS.Api.Repositories;
using SDPIS.Api.Services.Autorizacion;
using SDPIS.Api.Services.GestionUsuarios;
using SDPIS.Api.Services.Validation;

namespace SDPIS.Api.Services;

// HU-010/011 + HU-015: el funcionario y el rol salen SIEMPRE del token
// (IUsuarioActual + IPermisosService). Los campos FuncionarioValidadorId,
// FuncionarioId y RolFuncionarioSimulado de los requests se ignoran: el
// servicio los rellena con el usuario autenticado antes de llamar al
// repositorio (que no cambia). Ademas, toda operacion verifica que la
// denuncia este dentro del alcance del usuario (IVisibilidadDenunciasService).
//
// Reglas:
//   * Consultar: cualquiera que pueda ver la denuncia (funcionario: las suyas;
//     jefe/delegado: las de su area; superusuario: todas).
//   * Registrar: el funcionario ASIGNADO a la denuncia con REGISTRAR_CRITERIOS,
//     o un superusuario.
//   * Modificar criterios o datos generales ya registrados: solo superusuario.
public class CriteriosTecnicosService(
    ICriteriosTecnicosRepository repository,
    IEvaluacionCriticidadService evaluacionCriticidadService,
    IUsuarioActual usuario,
    IPermisosService permisos,
    IVisibilidadDenunciasService visibilidad) : ICriteriosTecnicosService
{
    // Claves estables usadas hacia la BD (ver fn_nombre_criterio_tecnico en
    // 10_sp_criterios_tecnicos.sql), separadas del slug de ruta publico para
    // no acoplar la URL al texto interno del catalogo.
    private const string ClaveDanoPotencialSalud = "DANO_POTENCIAL_SALUD";
    private const string ClaveCriterioEspecializadoUvc = "CRITERIO_ESPECIALIZADO_UVC";

    public async Task<CriteriosTecnicosRegistradosResponse> RegistrarAsync(
        long denunciaId, RegistrarCriteriosTecnicosRequest request, CancellationToken ct)
    {
        var actor = await ActorAsync(ct);
        var denuncia = await visibilidad.VerificarAccesoDenunciaAsync(denunciaId, ct);

        var esFuncionarioAsignado = actor.Tiene(Permisos.RegistrarCriterios)
        && denuncia.FuncionarioAsignadoId == actor.FuncionarioId;
        if (!esFuncionarioAsignado)
            throw new PermisoDenegadoException(
                "Solo el funcionario asignado a la denuncia puede registrar sus criterios técnicos. El superusuario solo puede modificar criterios ya registrados.");

        // Quien evalua es el usuario autenticado, no lo que diga el navegador.
        request = request with { FuncionarioValidadorId = actor.FuncionarioId };

        CriteriosTecnicosValidator.Validar(request);
        var resultado = await repository.RegistrarAsync(denunciaId, request, ct);

        // HU-011 (#62), AC #1/#2/#6/#7: el registro siempre trae los 2
        // criterios puntuables (sp_registrar_criterios_tecnicos), por lo que
        // la evaluacion recien creada esta completa -- no hay nivel anterior.
        var criticidad = await evaluacionCriticidadService.EvaluarAsync(denunciaId, resultado.PuntajeTotal, null, ct);

        // AC #5: no habia comercio electronico anterior (evaluacion nueva).
        await evaluacionCriticidadService.EvaluarComercioElectronicoAsync(
            denunciaId, request.VinculadoComercioElectronico, null, ct);

        return new CriteriosTecnicosRegistradosResponse(
            resultado.IdEvaluacion, resultado.PuntajeTotal, criticidad.Nivel);
    }

    public async Task<CriterioTecnicoActualizadoResponse> ActualizarCriterioAsync(
        long denunciaId, string criterio, ActualizarCriterioTecnicoRequest request, CancellationToken ct)
    {
        var actor = await ActorSuperusuarioAsync(denunciaId, ct);
        request = request with { FuncionarioId = actor.FuncionarioId, RolFuncionarioSimulado = Roles.Superusuario };

        CriteriosTecnicosValidator.Validar(request);
        CriteriosTecnicosValidator.ValidarRolSuperusuario(request.RolFuncionarioSimulado);

        var clave = MapearClave(criterio);

        // Si el valor no cambia no hay modificacion real: no se toca la BD,
        // para no reiniciar el plazo de la denuncia ni dejar en bitacora un
        // "cambio" que no ocurrio (ver ReevaluarCriticidadAsync).
        var anterior = await repository.ObtenerAsync(denunciaId, ct);
        var valorAnteriorCriterio = anterior is not null ? ValorActual(anterior, clave) : null;
        if (valorAnteriorCriterio == request.ValorSeleccionado)
        {
            return new CriterioTecnicoActualizadoResponse(
                anterior!.IdEvaluacion, criterio, request.ValorSeleccionado, anterior.PuntajeTotal ?? 0);
        }

        if (valorAnteriorCriterio is not null)
            CriteriosTecnicosValidator.ValidarJustificacionEdicion(request.Justificacion);

        var resultado = await repository.ActualizarCriterioAsync(denunciaId, clave, request, ct);

        // HU-011 (#62): cada edicion de un criterio individual puede cambiar
        // el puntaje total -- recalcular automaticamente (AC #1/#2/#6/#7).
        // Al recalcular, sp_registrar_evaluacion_criticidad reinicia el
        // plazo desde hoy segun el nuevo nivel (acordado con el PO) y deja
        // constancia en bitacora de la fecha limite anterior y la nueva.
        await ReevaluarCriticidadAsync(denunciaId, resultado.PuntajeTotal, ct);

        return new CriterioTecnicoActualizadoResponse(
            resultado.IdEvaluacion, criterio, request.ValorSeleccionado, resultado.PuntajeTotal);
    }

    public async Task<MetadataCriteriosTecnicosActualizadaResponse> ActualizarMetadataAsync(
        long denunciaId, ActualizarMetadataCriteriosTecnicosRequest request, CancellationToken ct)
    {
        var actor = await ActorSuperusuarioAsync(denunciaId, ct);
        request = request with { FuncionarioId = actor.FuncionarioId, RolFuncionarioSimulado = Roles.Superusuario };

        CriteriosTecnicosValidator.Validar(request);
        CriteriosTecnicosValidator.ValidarRolSuperusuario(request.RolFuncionarioSimulado);

        var anterior = await repository.ObtenerAsync(denunciaId, ct);

        if (anterior?.VinculadoComercioElectronico is { } valorPrevio
            && valorPrevio != request.VinculadoComercioElectronico)
        {
            CriteriosTecnicosValidator.ValidarJustificacionEdicion(request.Justificacion);
        }

        var idEvaluacion = await repository.ActualizarMetadataAsync(denunciaId, request, ct);

        await evaluacionCriticidadService.EvaluarComercioElectronicoAsync(
            denunciaId, request.VinculadoComercioElectronico, anterior?.VinculadoComercioElectronico, ct);

        return new MetadataCriteriosTecnicosActualizadaResponse(idEvaluacion);
    }

    public async Task<CriteriosTecnicosDto?> ObtenerAsync(long denunciaId, CancellationToken ct)
    {
        await visibilidad.VerificarAccesoDenunciaAsync(denunciaId, ct);
        return await repository.ObtenerAsync(denunciaId, ct);
    }

    public async Task<AlertaSanitariaVigenteResponse> ObtenerAlertaSanitariaVigenteAsync(long denunciaId, CancellationToken ct)
    {
        await visibilidad.VerificarAccesoDenunciaAsync(denunciaId, ct);
        return await repository.ObtenerAlertaSanitariaVigenteAsync(denunciaId, ct);
    }

    public async Task<DetalleDenunciaDto?> ObtenerDetalleDenunciaAsync(long denunciaId, CancellationToken ct)
    {
        await visibilidad.VerificarAccesoDenunciaAsync(denunciaId, ct);
        return await repository.ObtenerDetalleDenunciaAsync(denunciaId, ct);
    }

    // ---------- Usuario autenticado ----------

    private async Task<ContextoPermisos> ActorAsync(CancellationToken ct)
    {
        var actor = await permisos.ObtenerContextoAsync(usuario.FuncionarioId, ct);
        if (actor is null || !actor.Activo)
            throw new PermisoDenegadoException("El usuario que realiza la acción no existe o está inactivo.");
        return actor;
    }

    // Modificar criterios ya registrados es exclusivo del superusuario
    // (HU-010, correccion de alcance).
    private async Task<ContextoPermisos> ActorSuperusuarioAsync(long denunciaId, CancellationToken ct)
    {
        var actor = await ActorAsync(ct);
        await visibilidad.VerificarAccesoDenunciaAsync(denunciaId, ct);
        if (!actor.Tiene(Permisos.AdministrarSistema))
            throw new PermisoDenegadoException("Solo un superusuario puede modificar los criterios técnicos ya registrados.");
        return actor;
    }

    // HU-011 (#62): el catalogo de criterios puntuables es fijo (Dano
    // potencial a la salud + Criterio especializado UVC, 09_catalogo_
    // criterios_tecnicos.sql). Tras editar un criterio individual se relee
    // el estado actual para decidir si la evaluacion sigue completa (ambos
    // criterios con valor): si lo esta, se recalcula el nivel; si no, se
    // limpia en vez de calcular sobre un puntaje parcial. Ya no se pueden
    // eliminar criterios, pero pueden existir evaluaciones incompletas
    // de cuando si se permitia (el superusuario las completa con "Agregar").
    // NivelCriticidad no lo toca ningun SP de HU-010, asi que leerlo aqui
    // (antes de nuestro propio recalculo) equivale a leer el "nivel anterior".
    private async Task ReevaluarCriticidadAsync(long denunciaId, decimal puntajeTotal, CancellationToken ct)
    {
        var actual = await repository.ObtenerAsync(denunciaId, ct);
        var completa = actual is { DanoPotencialSalud: not null, RequiereCriterioEspecializado: not null };

        if (completa)
            await evaluacionCriticidadService.EvaluarAsync(denunciaId, puntajeTotal, actual!.NivelCriticidad, ct);
        else
            await evaluacionCriticidadService.LimpiarAsync(denunciaId, ct);
    }

    private static string? ValorActual(CriteriosTecnicosDto criterios, string clave) => clave switch
    {
        ClaveDanoPotencialSalud => criterios.DanoPotencialSalud,
        ClaveCriterioEspecializadoUvc => criterios.RequiereCriterioEspecializado,
        _ => null
    };

    private static string MapearClave(string criterio) => criterio switch
    {
        "dano-potencial-salud" => ClaveDanoPotencialSalud,
        "criterio-especializado-uvc" => ClaveCriterioEspecializadoUvc,
        _ => throw new DenunciaValidationException([$"Criterio tecnico no reconocido: \"{criterio}\"."])
    };
}