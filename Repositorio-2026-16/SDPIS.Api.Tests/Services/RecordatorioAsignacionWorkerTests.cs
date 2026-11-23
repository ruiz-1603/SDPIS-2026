using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using SDPIS.Api.Models.DTOs;
using SDPIS.Api.Repositories;
using SDPIS.Api.Services;
using Microsoft.Extensions.Hosting;

namespace SDPIS.Api.Tests.Services;

public class RecordatorioAsignacionWorkerTests
{
    private readonly Mock<IAsignacionRepository> _repositoryMock;
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<IServiceScope> _serviceScopeMock;
    private readonly Mock<IServiceScopeFactory> _serviceScopeFactoryMock;
    private readonly Mock<ILogger<RecordatorioAsignacionWorker>> _loggerMock;

    public RecordatorioAsignacionWorkerTests()
    {
        _repositoryMock = new Mock<IAsignacionRepository>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        _serviceScopeMock = new Mock<IServiceScope>();
        _serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
        _loggerMock = new Mock<ILogger<RecordatorioAsignacionWorker>>();

        _serviceProviderMock
            .Setup(x => x.GetService(typeof(IServiceScopeFactory)))
            .Returns(_serviceScopeFactoryMock.Object);

        _serviceScopeFactoryMock
            .Setup(x => x.CreateScope())
            .Returns(_serviceScopeMock.Object);

        _serviceScopeMock
            .Setup(x => x.ServiceProvider)
            .Returns(_serviceProviderMock.Object);

        _serviceProviderMock
            .Setup(x => x.GetService(typeof(IAsignacionRepository)))
            .Returns(_repositoryMock.Object);
    }

    [Fact]
    public async Task EjecucionWorker_ConDenunciasAtrasadas_LlamaAGenerarRecordatorio()
    {
        // arrange
        var denuncias = new List<DenunciaAtrasadaDto>
        {
            new DenunciaAtrasadaDto(IdDenuncia: 1, AreaActualId: 10, Consecutivo: "DEN-2026-001"),
            new DenunciaAtrasadaDto(IdDenuncia: 2, AreaActualId: 10, Consecutivo: "DEN-2026-002")
        };

        _repositoryMock
            .Setup(r => r.ObtenerDenunciasAtrasadasAsignacionAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(denuncias);

        var worker = new RecordatorioAsignacionWorker(_serviceProviderMock.Object, _loggerMock.Object);

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(50);

        // act
        try
        {
            await worker.StartAsync(cts.Token);
            await Task.Delay(100); 
        }
        catch (TaskCanceledException)
        {
            // se espera que Task.Delay dentro del worker lance esta excepcion a cancelarse
        }

        // assert
        _repositoryMock.Verify(r => r.ObtenerDenunciasAtrasadasAsignacionAsync(3, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        _repositoryMock.Verify(r => r.GenerarRecordatorioAtrasoAsync(1, "DEN-2026-001", It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        _repositoryMock.Verify(r => r.GenerarRecordatorioAtrasoAsync(2, "DEN-2026-002", It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task EjecucionWorker_SinDenunciasAtrasadas_NoGeneraRecordatorios()
    {
        // arrange
        _repositoryMock
            .Setup(r => r.ObtenerDenunciasAtrasadasAsignacionAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DenunciaAtrasadaDto>());

        var worker = new RecordatorioAsignacionWorker(_serviceProviderMock.Object, _loggerMock.Object);

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(50);

        // act
        try
        {
            await worker.StartAsync(cts.Token);
            await Task.Delay(100); 
        }
        catch (TaskCanceledException)
        {
        }

        // assert
        _repositoryMock.Verify(r => r.ObtenerDenunciasAtrasadasAsignacionAsync(3, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        _repositoryMock.Verify(r => r.GenerarRecordatorioAtrasoAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
