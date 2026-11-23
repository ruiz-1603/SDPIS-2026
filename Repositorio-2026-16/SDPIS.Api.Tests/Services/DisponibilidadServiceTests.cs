using Moq;
using SDPIS.Api.Models.DTOs;
using SDPIS.Api.Repositories;
using SDPIS.Api.Services;
using SDPIS.Api.Services.Validation;

namespace SDPIS.Api.Tests.Services;

public class DisponibilidadServiceTests
{
    private readonly Mock<IDisponibilidadRepository> _repositoryMock;
    private readonly DisponibilidadService _service;

    public DisponibilidadServiceTests()
    {
        _repositoryMock = new Mock<IDisponibilidadRepository>();
        _service = new DisponibilidadService(_repositoryMock.Object);
    }

    [Fact]
    public async Task RegistrarPeriodoAsync_Valido_SinTraslape_LlamaRepositoryYRetornaDTO()
    {
        // arrange
        var request = new RegistrarIndisponibilidadRequest(
            FuncionarioId: 1,
            Motivo: "vacaciones",
            FechaInicio: "2026-10-01",
            FechaFinEstimada: "2026-10-15",
            RegistradoPor: "JefeTest"
        );

        _repositoryMock.Setup(r => r.ObtenerPeriodosAsync(request.FuncionarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PeriodoIndisponibilidadRegistro>());

        _repositoryMock.Setup(r => r.InsertarPeriodoAsync(
            request.FuncionarioId, request.Motivo, It.IsAny<DateOnly>(), It.IsAny<DateOnly?>(), request.RegistradoPor, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PeriodoIndisponibilidadRegistro(100, 1, "vacaciones", new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 15), "JefeTest", DateOnly.FromDateTime(DateTime.Now)));

        // act
        var result = await _service.RegistrarPeriodoAsync(request, CancellationToken.None);

        // assert
        Assert.NotNull(result);
        Assert.Equal(100, result.IdPeriodo);
        Assert.Equal("vacaciones", result.Motivo);
        _repositoryMock.Verify(r => r.InsertarPeriodoAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegistrarPeriodoAsync_ConTraslape_LanzaDenunciaValidationException()
    {
        // arrange
        var request = new RegistrarIndisponibilidadRequest(
            FuncionarioId: 1,
            Motivo: "incapacidad",
            FechaInicio: "2026-10-10",
            FechaFinEstimada: "2026-10-20",
            RegistradoPor: "JefeTest"
        );

        var periodosExistentes = new List<PeriodoIndisponibilidadRegistro>
        {
            new PeriodoIndisponibilidadRegistro(101, 1, "vacaciones", new DateOnly(2026, 10, 5), new DateOnly(2026, 10, 15), "Otro", DateOnly.FromDateTime(DateTime.Now))
        };

        _repositoryMock.Setup(r => r.ObtenerPeriodosAsync(request.FuncionarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(periodosExistentes);

        // act & assert
        var exception = await Assert.ThrowsAsync<DenunciaValidationException>(() => 
            _service.RegistrarPeriodoAsync(request, CancellationToken.None));
            
        Assert.Contains("Ya existe un periodo de indisponibilidad registrado que se traslapa con estas fechas.", exception.Errores);
    }

    [Fact]
    public async Task EliminarPeriodoAsync_PeriodoFinalizado_LanzaDenunciaValidationException()
    {
        // arrange
        long periodoId = 101;
        long funcionarioId = 1;

        var periodoFinalizado = new PeriodoIndisponibilidadRegistro(periodoId, funcionarioId, "vacaciones", new DateOnly(2020, 1, 1), new DateOnly(2020, 1, 10), "Admin", DateOnly.FromDateTime(DateTime.Now));

        _repositoryMock.Setup(r => r.ObtenerPeriodosAsync(funcionarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PeriodoIndisponibilidadRegistro> { periodoFinalizado });

        // act & assert
        var exception = await Assert.ThrowsAsync<DenunciaBaseDatosException>(() => 
            _service.EliminarPeriodoAsync(periodoId, funcionarioId, CancellationToken.None));

        Assert.Contains("No se puede eliminar un periodo de indisponibilidad finalizado.", exception.Message);
    }
}
