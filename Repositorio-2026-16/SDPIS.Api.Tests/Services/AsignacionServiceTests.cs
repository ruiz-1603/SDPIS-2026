using Moq;
using SDPIS.Api.Models.DTOs;
using SDPIS.Api.Repositories;
using SDPIS.Api.Services;
using SDPIS.Api.Services.Autorizacion;
using SDPIS.Api.Services.GestionUsuarios;

namespace SDPIS.Api.Tests.Services;

public class AsignacionServiceTests
{
    private readonly Mock<IAsignacionRepository> _repositoryMock = new();
    private readonly Mock<IGestionUsuariosRepository> _gestionRepositoryMock = new();

    // HU-015: el servicio toma al usuario del token y sus permisos de
    // IPermisosService. Aqui el autenticado es siempre el jefe de area 1, en
    // el area indicada.
    private AsignacionService CrearServicio(long areaDelJefe = 5)
    {
        var jefe = new FuncionarioPermisosInfo(1, Roles.JefeArea, true, areaDelJefe, false, []);
        return new AsignacionService(
            _repositoryMock.Object,
            _gestionRepositoryMock.Object,
            new FakePermisosService(jefe),
            new FakeUsuarioActual(1));
    }

    [Fact]
    public async Task AsignarDenunciaAsync_JefeAsignaAOtroFuncionario_AsignacionExitosa()
    {
        // Arrange
        long denunciaId = 100;
        long validadorId = 2;

        _gestionRepositoryMock
            .Setup(r => r.ObtenerAlcanceDenunciaAsync(denunciaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DenunciaAlcanceRegistro(denunciaId, AreaActualId: 5, FuncionarioAsignadoId: null));
        _gestionRepositoryMock
            .Setup(r => r.ObtenerFuncionarioAsync(validadorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FuncionarioRegistro(
                validadorId, "Valeria Mora Chaves", Roles.Validador, "valeria.mora@sdpis.go.cr", true, 5, null, false));
        _repositoryMock
            .Setup(r => r.AsignarValidadorManualAsync(denunciaId, validadorId, 1, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var exception = await Record.ExceptionAsync(() =>
            CrearServicio().AsignarDenunciaAsync(denunciaId, validadorId, CancellationToken.None));

        // Assert
        Assert.Null(exception);
        _repositoryMock.Verify(r => r.AsignarValidadorManualAsync(denunciaId, validadorId, 1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AsignarDenunciaAsync_ValidadorConAreaAdicionalDeLaDenuncia_AsignacionExitosa()
    {
        // Area base 9; el superusuario le otorgo el area 5 (la de la denuncia).
        long denunciaId = 100;
        long validadorId = 2;

        _gestionRepositoryMock
            .Setup(r => r.ObtenerAlcanceDenunciaAsync(denunciaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DenunciaAlcanceRegistro(denunciaId, AreaActualId: 5, FuncionarioAsignadoId: null));
        _gestionRepositoryMock
            .Setup(r => r.ObtenerFuncionarioAsync(validadorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FuncionarioRegistro(
                validadorId, "Valeria Mora Chaves", Roles.Validador, "valeria.mora@sdpis.go.cr", true, 9, null, false));
        _gestionRepositoryMock
            .Setup(r => r.FuncionarioPerteneceAAreaAsync(validadorId, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _repositoryMock
            .Setup(r => r.AsignarValidadorManualAsync(denunciaId, validadorId, 1, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var exception = await Record.ExceptionAsync(() =>
            CrearServicio().AsignarDenunciaAsync(denunciaId, validadorId, CancellationToken.None));

        Assert.Null(exception);
        _repositoryMock.Verify(r => r.AsignarValidadorManualAsync(denunciaId, validadorId, 1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AsignarDenunciaAsync_ValidadorDeOtraAreaSinAreaAdicional_LanzaReglaNegocio()
    {
        long denunciaId = 100;
        long validadorId = 2;

        _gestionRepositoryMock
            .Setup(r => r.ObtenerAlcanceDenunciaAsync(denunciaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DenunciaAlcanceRegistro(denunciaId, AreaActualId: 5, FuncionarioAsignadoId: null));
        _gestionRepositoryMock
            .Setup(r => r.ObtenerFuncionarioAsync(validadorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FuncionarioRegistro(
                validadorId, "Valeria Mora Chaves", Roles.Validador, "valeria.mora@sdpis.go.cr", true, 9, null, false));

        var exception = await Assert.ThrowsAsync<ReglaNegocioException>(() =>
            CrearServicio().AsignarDenunciaAsync(denunciaId, validadorId, CancellationToken.None));

        Assert.Contains("pertenecer al area", exception.Message);
        _repositoryMock.Verify(r => r.AsignarValidadorManualAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AsignarDenunciaAsync_JefeSeAsignaASiMismo_LanzaExcepcion()
    {
        // Arrange: el validador elegido (id 1) es el propio jefe autenticado.
        long denunciaId = 100;

        _gestionRepositoryMock
            .Setup(r => r.ObtenerAlcanceDenunciaAsync(denunciaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DenunciaAlcanceRegistro(denunciaId, AreaActualId: 5, FuncionarioAsignadoId: null));
        _gestionRepositoryMock
            .Setup(r => r.ObtenerFuncionarioAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FuncionarioRegistro(
                1, "Rosa Uribe Vargas", Roles.JefeArea, "jefe.area@sdpis.go.cr", true, 5, null, true));

        // Act & Assert: ahora la regla la aplica el servicio con ReglaNegocioException.
        var exception = await Assert.ThrowsAsync<ReglaNegocioException>(() =>
            CrearServicio().AsignarDenunciaAsync(denunciaId, 1, CancellationToken.None));

        Assert.Contains("jefe de area", exception.Message);

        _repositoryMock.Verify(r => r.AsignarValidadorManualAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ObtenerDenunciasPendientesAsync_DevuelveDenunciasConAlertaSiPasan3Dias()
    {
        // Arrange: el jefe autenticado pertenece al area consultada (un jefe solo ve la suya).
        long areaId = 10;
        var denunciaVieja = new DenunciaPendienteDto(
            IdDenuncia: 1,
            Consecutivo: "DEN-001",
            FechaRecepcion: DateTime.Now.AddDays(-4).ToString("yyyy-MM-dd")
        );

        var denunciaReciente = new DenunciaPendienteDto(
            IdDenuncia: 2,
            Consecutivo: "DEN-002",
            FechaRecepcion: DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd")
        );

        _repositoryMock
            .Setup(r => r.ObtenerDenunciasPendientesAsignacionAsync(areaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DenunciaPendienteDto> { denunciaVieja, denunciaReciente });

        // Act
        var resultado = await CrearServicio(areaDelJefe: areaId)
            .ObtenerDenunciasPendientesAsync(areaId, CancellationToken.None);

        // Assert
        Assert.Equal(2, resultado.Count);
    }
}