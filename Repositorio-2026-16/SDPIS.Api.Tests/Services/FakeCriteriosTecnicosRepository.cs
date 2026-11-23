using SDPIS.Api.Models.DTOs;
using SDPIS.Api.Repositories;

namespace SDPIS.Api.Tests.Services;

// Fake en memoria de ICriteriosTecnicosRepository: el proyecto no tiene una
// libreria de mocking instalada (ver SDPIS.Test.csproj), asi que se usa un
// fake simple en vez de un mock generado. Permite verificar que el servicio
// llama (o no llama) al repositorio segun corresponda, sin tocar Oracle.
public class FakeCriteriosTecnicosRepository : ICriteriosTecnicosRepository
{
    public CriteriosTecnicosResultado ResultadoARetornar { get; set; } = new(1, 6);
    public long IdEvaluacionMetadataARetornar { get; set; } = 1;

    // Lo que devuelve ObtenerAsync (usado por CriteriosTecnicosService para
    // decidir si la evaluacion sigue completa tras editar un
    // criterio, y para leer el comercio electronico anterior en
    // ActualizarMetadataAsync -- ver HU-011). Por defecto representa una
    // evaluacion completa (ambos criterios respondidos), para no romper las
    // pruebas existentes que no configuran este valor explicitamente.
    public CriteriosTecnicosDto? DtoARetornar { get; set; } = new(
        IdEvaluacion: 1,
        DenunciaId: 9001,
        FuncionarioValidadorId: 100,
        FechaEvaluacion: DateTime.Now,
        PuntajeTotal: 6,
        VinculadoComercioElectronico: "N",
        DanoPotencialSalud: "Alto",
        RequiereCriterioEspecializado: "Si",
        ObservacionesTecnicas: null,
        NivelCriticidad: null,
        FechaLimite: null
    );
    public bool SeLlamoRegistrar { get; private set; }
    public bool SeLlamoActualizarCriterio { get; private set; }
    public bool SeLlamoActualizarMetadata { get; private set; }
    public long? UltimoDenunciaId { get; private set; }
    public string? UltimaClaveCriterio { get; private set; }

    public Task<CriteriosTecnicosResultado> RegistrarAsync(
        long denunciaId, RegistrarCriteriosTecnicosRequest request, CancellationToken ct)
    {
        SeLlamoRegistrar = true;
        UltimoDenunciaId = denunciaId;
        return Task.FromResult(ResultadoARetornar);
    }

    public Task<CriteriosTecnicosResultado> ActualizarCriterioAsync(
        long denunciaId, string criterioClave, ActualizarCriterioTecnicoRequest request, CancellationToken ct)
    {
        SeLlamoActualizarCriterio = true;
        UltimoDenunciaId = denunciaId;
        UltimaClaveCriterio = criterioClave;
        return Task.FromResult(ResultadoARetornar);
    }

    public Task<long> ActualizarMetadataAsync(
        long denunciaId, ActualizarMetadataCriteriosTecnicosRequest request, CancellationToken ct)
    {
        SeLlamoActualizarMetadata = true;
        UltimoDenunciaId = denunciaId;
        return Task.FromResult(IdEvaluacionMetadataARetornar);
    }

    public Task<CriteriosTecnicosDto?> ObtenerAsync(long denunciaId, CancellationToken ct)
    {
        return Task.FromResult(DtoARetornar);
    }

    public Task<AlertaSanitariaVigenteResponse> ObtenerAlertaSanitariaVigenteAsync(long denunciaId, CancellationToken ct)
    {
        return Task.FromResult(new AlertaSanitariaVigenteResponse(false, []));
    }

    public Task<DetalleDenunciaDto?> ObtenerDetalleDenunciaAsync(long denunciaId, CancellationToken ct)
    {
        return Task.FromResult<DetalleDenunciaDto?>(null);
    }
}
