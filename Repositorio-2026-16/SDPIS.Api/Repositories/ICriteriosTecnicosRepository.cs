using SDPIS.Api.Models.DTOs;

namespace SDPIS.Api.Repositories;

public interface ICriteriosTecnicosRepository
{
    Task<CriteriosTecnicosResultado> RegistrarAsync(
        long denunciaId, RegistrarCriteriosTecnicosRequest request, CancellationToken ct);

    Task<CriteriosTecnicosResultado> ActualizarCriterioAsync(
        long denunciaId, string criterioClave, ActualizarCriterioTecnicoRequest request, CancellationToken ct);

    Task<long> ActualizarMetadataAsync(
        long denunciaId, ActualizarMetadataCriteriosTecnicosRequest request, CancellationToken ct);

    Task<CriteriosTecnicosDto?> ObtenerAsync(long denunciaId, CancellationToken ct);

    Task<AlertaSanitariaVigenteResponse> ObtenerAlertaSanitariaVigenteAsync(long denunciaId, CancellationToken ct);

    // Devuelve null si la denuncia no existe.
    Task<DetalleDenunciaDto?> ObtenerDetalleDenunciaAsync(long denunciaId, CancellationToken ct);
}
