using Moq;
using SDPIS.Api.Models.DTOs;
using SDPIS.Api.Repositories;
using SDPIS.Api.Services;
using SDPIS.Api.Services.Seguimiento;
using Xunit;

namespace SDPIS.Test;

public class DenunciaServiceTests
{
    private static CrearDenunciaRequest RequestValido() => new(
     Hecho: new HechoDto("N", null, "N", null, "Establecimiento X", "Descripcion suficientemente larga."),
     Ubicacion: new UbicacionDto(CantonId: 1, DistritoId: null, DireccionExacta: null),
     Privacidad: new PrivacidadDto(EsAnonima: "S", Denunciante: null),
     Productos: [new ProductoDenunciaDto(1, "Producto de prueba", null, "Descripcion del producto de prueba", null, null, null, null, null, null, [8])]
 );

    [Fact]
    public async Task RegistrarAsync_ColisionDeCodigoUnaVez_Reintenta_Y_RegistraConExito()
    {
        var mockRepo = new Mock<IDenunciaRepository>();
        mockRepo.SetupSequence(r => r.RegistrarDenunciaAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CodigoSeguimientoDuplicadoException(new Exception("colision")))
            .ReturnsAsync(new DenunciaRegistroResultado(1, "2026-000001", "7K9M-QR2T-XW4P"));

        var servicio = new DenunciaService(mockRepo.Object);

        var resultado = await servicio.RegistrarAsync(RequestValido(), CancellationToken.None);

        Assert.Equal("7K9M-QR2T-XW4P", resultado.CodigoSeguimiento);
        mockRepo.Verify(r => r.RegistrarDenunciaAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task RegistrarAsync_ColisionEnTodosLosIntentos_LanzaInvalidOperationException()
    {
        var mockRepo = new Mock<IDenunciaRepository>();
        mockRepo.Setup(r => r.RegistrarDenunciaAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CodigoSeguimientoDuplicadoException(new Exception("colision")));

        var servicio = new DenunciaService(mockRepo.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => servicio.RegistrarAsync(RequestValido(), CancellationToken.None));

        mockRepo.Verify(r => r.RegistrarDenunciaAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(5));
    }

    [Fact]
    public async Task RegistrarAsync_FalloDeBaseDeDatos_PropagaSinReintentarNiFingirExito()
    {
        var mockRepo = new Mock<IDenunciaRepository>();
        mockRepo.Setup(r => r.RegistrarDenunciaAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DenunciaBaseDatosException("ORA-20008: no se encontro area", new Exception()));

        var servicio = new DenunciaService(mockRepo.Object);

        await Assert.ThrowsAsync<DenunciaBaseDatosException>(
            () => servicio.RegistrarAsync(RequestValido(), CancellationToken.None));

        // Un solo intento: este tipo de error no es de colision, no debe reintentar.
        mockRepo.Verify(r => r.RegistrarDenunciaAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Generar_ProduceFormatoEsperadoSinCaracteresAmbiguos()
    {
        var codigo = CodigoSeguimientoGenerator.Generar();

        Assert.Matches(@"^[23456789ABCDEFGHJKMNPQRSTUVWXYZ]{4}-[23456789ABCDEFGHJKMNPQRSTUVWXYZ]{4}-[23456789ABCDEFGHJKMNPQRSTUVWXYZ]{4}$", codigo);
    }
}