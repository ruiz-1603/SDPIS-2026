using SDPIS.Api.Models.DTOs;
using SDPIS.Api.Repositories;
using SDPIS.Api.Services;
using SDPIS.Api.Services.Validation;
using Xunit;

namespace SDPIS.Api.Tests.Services;

public class FakeAsignacionEncargadoRepository : IAsignacionEncargadoRepository
{
    public bool AsignarEncargadoLlamado { get; private set; }
    public bool ReasignarAreaLlamado { get; private set; }
    public bool ReasignarAUvcLlamado { get; private set; }
    public bool ConfirmarAtencionUvcLlamado { get; private set; }
    public bool DevolverAreaRectoraLlamado { get; private set; }

    public Task AsignarEncargadoAsync(long denunciaId, long encargadoId, long jefeAreaId, string? motivo, CancellationToken ct)
    {
        AsignarEncargadoLlamado = true;
        return Task.CompletedTask;
    }

    public Task ReasignarAreaAsync(long denunciaId, long areaNuevaId, long jefeAreaId, string? motivo, CancellationToken ct)
    {
        ReasignarAreaLlamado = true;
        return Task.CompletedTask;
    }

    public Task ReasignarAUvcAsync(long denunciaId, long jefeAreaId, string motivo, string? numeroOficio, CancellationToken ct)
    {
        ReasignarAUvcLlamado = true;
        return Task.CompletedTask;
    }

    public Task ConfirmarAtencionUvcAsync(long denunciaId, long superusuarioId, string? motivo, CancellationToken ct)
    {
        ConfirmarAtencionUvcLlamado = true;
        return Task.CompletedTask;
    }

    public Task DevolverAreaRectoraAsync(long denunciaId, long areaRectoraId, long superusuarioId, string? motivo, CancellationToken ct)
    {
        DevolverAreaRectoraLlamado = true;
        return Task.CompletedTask;
    }

    // Metodos no testeados en este unit test
    public Task<List<DenunciaParaEncargadoDto>> ObtenerDenunciasBandejaAsync(long areaId, CancellationToken ct) => throw new NotImplementedException();
    public Task<List<FuncionarioEncargadoDto>> ObtenerEncargadosDisponiblesAsync(long areaId, CancellationToken ct) => throw new NotImplementedException();
    public Task<List<AreaReasignacionDto>> ObtenerAreasDestinoAsync(CancellationToken ct) => throw new NotImplementedException();
    public Task<List<HistorialAsignacionDto>> ObtenerHistorialAsync(long denunciaId, CancellationToken ct) => throw new NotImplementedException();
    public Task<int> GenerarAlertasVencimientoAsync(long areaId, CancellationToken ct) => Task.FromResult(0);
}

public class FakeDisponibilidadService : IDisponibilidadService
{
    public Task<List<FuncionarioDisponibilidadResumenDto>> ObtenerResumenFuncionariosAsync(
    long? areaId, CancellationToken ct)
    => throw new NotImplementedException();
    public bool Disponible { get; set; } = true;

    public Task<bool> EsDisponibleAsync(long funcionarioId, DateOnly fecha, CancellationToken ct)
    {
        return Task.FromResult(Disponible);
    }

    public Task<PeriodoIndisponibilidadDto> RegistrarPeriodoAsync(RegistrarIndisponibilidadRequest request, CancellationToken ct) => throw new NotImplementedException();
    public Task<PeriodoIndisponibilidadDto> ActualizarPeriodoAsync(ActualizarIndisponibilidadRequest request, CancellationToken ct) => throw new NotImplementedException();
    public Task EliminarPeriodoAsync(long funcionarioId, long periodoId, CancellationToken ct) => throw new NotImplementedException();
    public Task<List<FuncionarioDisponibilidadResumenDto>> ObtenerResumenFuncionariosAsync(CancellationToken ct) => throw new NotImplementedException();
    public Task<DisponibilidadFuncionarioDto> ObtenerDisponibilidadAsync(long funcionarioId, CancellationToken ct) => throw new NotImplementedException();
}

public class AsignacionEncargadoServiceTests
{
    private readonly FakeAsignacionEncargadoRepository _repository;
    private readonly FakeDisponibilidadService _disponibilidadService;
    private readonly AsignacionEncargadoService _service;

    public AsignacionEncargadoServiceTests()
    {
        _repository = new FakeAsignacionEncargadoRepository();
        _disponibilidadService = new FakeDisponibilidadService();
        _service = new AsignacionEncargadoService(_repository, _disponibilidadService);
    }

    [Fact]
    public async Task AsignarEncargadoAsync_DebeLanzarExcepcion_CuandoEncargadoNoEstaDisponible()
    {
        // Arrange
        var denunciaId = 1L;
        var request = new AsignarEncargadoRequest(2, 1, "Prueba");
        
        _disponibilidadService.Disponible = false;

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DenunciaBaseDatosException>(() => 
            _service.AsignarEncargadoAsync(denunciaId, request, CancellationToken.None));
            
        Assert.Contains("El funcionario seleccionado no esta disponible", ex.Message);
    }

    [Fact]
    public async Task AsignarEncargadoAsync_DebeLlamarARepository_CuandoEncargadoEstaDisponible()
    {
        // Arrange
        var denunciaId = 1L;
        var request = new AsignarEncargadoRequest(2, 1, "Prueba");
        
        _disponibilidadService.Disponible = true;

        // Act
        await _service.AsignarEncargadoAsync(denunciaId, request, CancellationToken.None);

        // Assert
        Assert.True(_repository.AsignarEncargadoLlamado);
    }

    [Fact]
    public async Task ReasignarAreaAsync_DebeLlamarARepository()
    {
        // Arrange
        var denunciaId = 1L;
        var request = new ReasignarAreaRequest(3, 1, "Cambio de area");

        // Act
        await _service.ReasignarAreaAsync(denunciaId, request, CancellationToken.None);

        // Assert
        Assert.True(_repository.ReasignarAreaLlamado);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ReasignarAUvcAsync_DebeLanzarExcepcion_CuandoMotivoEsInvalido(string motivo)
    {
        // Act & Assert
        await Assert.ThrowsAsync<DenunciaValidationException>(() => 
            _service.ReasignarAUvcAsync(1, 2, motivo, null, CancellationToken.None));
    }

    [Fact]
    public async Task ConfirmarAtencionUvcAsync_DebeLanzarExcepcion_CuandoSuperusuarioIdEsInvalido()
    {
        // Arrange
        var request = new ConfirmarAtencionUvcRequest(0, "Finalizado");

        // Act & Assert
        await Assert.ThrowsAsync<DenunciaValidationException>(() => 
            _service.ConfirmarAtencionUvcAsync(1, request, CancellationToken.None));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task DevolverAreaRectoraAsync_DebeLanzarExcepcion_CuandoJustificacionEsInvalida(string? motivo)
    {
        var request = new DevolverAreaRectoraRequest(5, 100, motivo);

        await Assert.ThrowsAsync<DenunciaValidationException>(() =>
            _service.DevolverAreaRectoraAsync(1, request, CancellationToken.None));
    }

    [Fact]
    public async Task DevolverAreaRectoraAsync_DebeLanzarExcepcion_CuandoAreaRectoraIdEsInvalido()
    {
        // Arrange
        var request = new DevolverAreaRectoraRequest(0, 100, "Devuelto");

        // Act & Assert
        await Assert.ThrowsAsync<DenunciaValidationException>(() => 
            _service.DevolverAreaRectoraAsync(1, request, CancellationToken.None));
    }
}
