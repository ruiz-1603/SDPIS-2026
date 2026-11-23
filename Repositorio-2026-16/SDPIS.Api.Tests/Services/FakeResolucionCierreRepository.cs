using SDPIS.Api.Models.DTOs;
using SDPIS.Api.Repositories;

namespace SDPIS.Api.Tests.Services;

// Fake en memoria de IResolucionCierreRepository (HU-017). Simula lo que hace
// sp_resolver_solicitud_cierre con el estado: APROBADA -> FINALIZADA;
// RECHAZADA -> EstadoPrevio (o EN_TIEMPO si es null). Permite verificar si
// el servicio llama (o no) al SP sin tocar Oracle.
public class FakeResolucionCierreRepository : IResolucionCierreRepository
{
    // Por defecto: solicitud 7001 pendiente, de la denuncia 9001 en revision, area 5.
    public SolicitudResolucionInfo? SolicitudARetornar { get; set; } =
        new(7001, 9001, 5, "PENDIENTE", "EN_REVISION");

    // Estado de la denuncia antes de la solicitud (solicitud_cierre.estado_previo).
    public string? EstadoPrevio { get; set; } = "ATRASADA";

    public List<HistorialSolicitudCierreDto> HistorialARetornar { get; set; } = [];

    public List<(long SolicitudId, long JefeId, string Decision, string? Observaciones)> Resoluciones { get; } = [];
    public long? DenunciaHistorialConsultada { get; private set; }

    public Task<SolicitudResolucionInfo?> ObtenerSolicitudAsync(long solicitudId, CancellationToken ct)
        => Task.FromResult(SolicitudARetornar);

    public Task<string> ResolverAsync(long solicitudId, long jefeId, string decision, string? observaciones,
        CancellationToken ct)
    {
        Resoluciones.Add((solicitudId, jefeId, decision, observaciones));
        return Task.FromResult(decision == "APROBADA" ? "FINALIZADA" : EstadoPrevio ?? "EN_TIEMPO");
    }

    public Task<DecisionCierreDto?> ObtenerDecisionAsync(long solicitudId, CancellationToken ct)
    {
        var (_, jefeId, decision, observaciones) = Resoluciones[^1];
        return Task.FromResult<DecisionCierreDto?>(new DecisionCierreDto(
            1, decision, jefeId, "Jefe Prueba", observaciones, "2026-10-09T10:00:00.000"));
    }

    public Task<List<HistorialSolicitudCierreDto>> ListarHistorialAsync(long denunciaId, CancellationToken ct)
    {
        DenunciaHistorialConsultada = denunciaId;
        return Task.FromResult(HistorialARetornar);
    }
}
