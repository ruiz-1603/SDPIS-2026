using Oracle.ManagedDataAccess.Client;
using SDPIS.Api.Models.DTOs;
using SDPIS.Api.Repositories;

namespace SDPIS.Api.Tests.Services;

// Fake en memoria de IBitacoraRepository (ver FakeCriteriosTecnicosRepository
// para el mismo criterio: se usa un fake simple en vez de un mock generado).
// Permite verificar que BitacoraService llama (o no llama) al repositorio
// segun corresponda, sin tocar Oracle ni el trigger de inmutabilidad real.
public class FakeBitacoraRepository : IBitacoraRepository
{
    public List<RegistroBitacora> RegistrosInsertados { get; } = [];

    public BitacoraEntradaDto EntradaARetornar { get; set; } = new(
        IdBitacora: 1, DenunciaId: 9001, FuncionarioId: null, FuncionarioNombre: null,
        AreaOrigenId: null, AreaOrigenNombre: null, Fecha: "2026-01-01T00:00:00.000",
        TipoRegistro: "asignacion", Contenido: "contenido");

    public FuncionarioAccesoInfo? FuncionarioARetornar { get; set; }
    public DenunciaAccesoInfo? DenunciaARetornar { get; set; }
    public (List<BitacoraEntradaDto> Items, int Total) ResultadoListarARetornar { get; set; } = ([], 0);
    public List<DenunciaBitacoraListadoDto> DenunciasVisiblesARetornar { get; set; } = [];
    public (long? AreaId, long? FuncionarioAsignadoId)? FiltroVisiblesRecibido { get; private set; }

    public Task<BitacoraEntradaDto> RegistrarAsync(RegistroBitacora registro, CancellationToken ct)
    {
        RegistrosInsertados.Add(registro);
        return Task.FromResult(EntradaARetornar);
    }

    // No la usa BitacoraService (es para repositorios de otras HUs, p. ej.
    // AsignacionRepository, que ya tienen su propia transaccion abierta).
    public Task RegistrarAsync(OracleConnection connection, OracleTransaction transaction,
        RegistroBitacora registro, CancellationToken ct)
        => throw new NotSupportedException("No se usa desde BitacoraService.");

    public Task<(List<BitacoraEntradaDto> Items, int Total)> ListarPorDenunciaAsync(
        long denunciaId, string? tipoRegistro, int pagina, int tamano, CancellationToken ct)
        => Task.FromResult(ResultadoListarARetornar);

    public Task<FuncionarioAccesoInfo?> ObtenerFuncionarioAsync(long funcionarioId, CancellationToken ct)
        => Task.FromResult(FuncionarioARetornar);

    public Task<DenunciaAccesoInfo?> ObtenerDenunciaAsync(long denunciaId, CancellationToken ct)
        => Task.FromResult(DenunciaARetornar);

    public Task<List<DenunciaBitacoraListadoDto>> ListarDenunciasVisiblesAsync(
        long? areaId, long? funcionarioAsignadoId, CancellationToken ct)
    {
        FiltroVisiblesRecibido = (areaId, funcionarioAsignadoId);
        return Task.FromResult(DenunciasVisiblesARetornar);
    }
}
