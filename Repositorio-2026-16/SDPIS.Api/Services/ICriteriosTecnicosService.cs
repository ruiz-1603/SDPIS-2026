using SDPIS.Api.Models.DTOs;

namespace SDPIS.Api.Services;

public interface ICriteriosTecnicosService
{
    Task<CriteriosTecnicosRegistradosResponse> RegistrarAsync(
        long denunciaId, RegistrarCriteriosTecnicosRequest request, CancellationToken ct);

    Task<CriterioTecnicoActualizadoResponse> ActualizarCriterioAsync(
        long denunciaId, string criterio, ActualizarCriterioTecnicoRequest request, CancellationToken ct);

    Task<MetadataCriteriosTecnicosActualizadaResponse> ActualizarMetadataAsync(
        long denunciaId, ActualizarMetadataCriteriosTecnicosRequest request, CancellationToken ct);

    Task<CriteriosTecnicosDto?> ObtenerAsync(long denunciaId, CancellationToken ct);

    Task<AlertaSanitariaVigenteResponse> ObtenerAlertaSanitariaVigenteAsync(long denunciaId, CancellationToken ct);

    Task<DetalleDenunciaDto?> ObtenerDetalleDenunciaAsync(long denunciaId, CancellationToken ct);
}
