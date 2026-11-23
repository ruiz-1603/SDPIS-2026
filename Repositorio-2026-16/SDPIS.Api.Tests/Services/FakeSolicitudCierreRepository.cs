using SDPIS.Api.Models.DTOs;
using SDPIS.Api.Repositories;

namespace SDPIS.Api.Tests.Services;

// Fake en memoria de ISolicitudCierreRepository (HU-018). Permite verificar
// que SolicitudCierreService llama (o no) al SP sin tocar Oracle.
public class FakeSolicitudCierreRepository : ISolicitudCierreRepository
{
    // Por defecto: denuncia 9001 en el area 5, asignada al funcionario 100 y en curso.
    public DenunciaCierreInfo? DenunciaARetornar { get; set; } = new(9001, "EN_TIEMPO", 5, 100);
    public SolicitudCierreDto? PendienteARetornar { get; set; }
    public List<SolicitudCierreDto> PendientesARetornar { get; set; } = [];

    public List<(long DenunciaId, long FuncionarioId, SolicitarCierreRequest Request)> Solicitudes { get; } = [];
    public bool SeListaronPendientes { get; private set; }
    public long? AreaListadaRecibida { get; private set; }

    public Task<DenunciaCierreInfo?> ObtenerDenunciaAsync(long denunciaId, CancellationToken ct)
        => Task.FromResult(DenunciaARetornar);

    public Task<long> SolicitarAsync(long denunciaId, long funcionarioId, SolicitarCierreRequest request, CancellationToken ct)
    {
        Solicitudes.Add((denunciaId, funcionarioId, request));
        return Task.FromResult(1L);
    }

    public Task<SolicitudCierreDto?> ObtenerAsync(long solicitudId, CancellationToken ct)
    {
        var (denunciaId, funcionarioId, request) = Solicitudes[^1];
        return Task.FromResult<SolicitudCierreDto?>(new SolicitudCierreDto(
            solicitudId, denunciaId, "ABAN-2026-000001", funcionarioId, "Funcionario Prueba",
            DenunciaARetornar?.AreaActualId ?? 5, "Area Prueba",
            request.ResolucionFinal, request.ObservacionesFinales, request.AccionesRealizadas,
            "2026-10-08T10:00:00.000", "PENDIENTE"));
    }

    public Task<SolicitudCierreDto?> ObtenerPendienteAsync(long denunciaId, CancellationToken ct)
        => Task.FromResult(PendienteARetornar);

    public Task<List<SolicitudCierreDto>> ListarPendientesAsync(long? areaId, CancellationToken ct)
    {
        SeListaronPendientes = true;
        AreaListadaRecibida = areaId;
        return Task.FromResult(PendientesARetornar);
    }
}
