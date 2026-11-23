using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SDPIS.Api.Middleware;
using SDPIS.Api.Models.DTOs;
using SDPIS.Api.Repositories;
using SDPIS.Api.Services;
using SDPIS.Api.Services.Mapping;
using SDPIS.Api.Services.Validation;

namespace SDPIS.Api.Tests.Services;

// HU-008: dirigir la denuncia al area segun su ubicacion.
// La resolucion del area (Provincia+Canton+Distrito -> ARS) vive en
// sp_registrar_denuncia (PL/SQL). Estas pruebas cubren la parte C# que la
// alimenta y que traduce su resultado: el JSON con la ubicacion, que no se
// intente asignar con datos invalidos y la respuesta cuando no hay ARS.
public class DirigirDenunciaAreaTests
{
    private static CrearDenunciaRequest RequestConUbicacion(int cantonId, int? distritoId, string? direccion = "Frente a la plaza") => new(
        Hecho: new HechoDto("N", null, "N", null, "Establecimiento X", "Descripcion suficientemente larga."),
        Ubicacion: new UbicacionDto(cantonId, distritoId, direccion),
        Privacidad: new PrivacidadDto(EsAnonima: "S", Denunciante: null),
        Productos: [new ProductoDenunciaDto(1, "Producto de prueba", null, "Descripcion del producto de prueba", null, null, null, null, null, null, [8])]
    );

    // ---------- JSON que lee el SP (rutas $.ubicacion.*) ----------

    [Theory]
    [InlineData(61, 367)]
    [InlineData(12, 76)]
    [InlineData(6, 42)]
    public void ToJson_ConCantonYDistrito_UsaLasClavesQueLeeElSp(int cantonId, int distritoId)
    {
        var json = DenunciaJsonMapper.ToJson(RequestConUbicacion(cantonId, distritoId));

        using var doc = JsonDocument.Parse(json);
        var ubicacion = doc.RootElement.GetProperty("ubicacion");
        Assert.Equal(cantonId, ubicacion.GetProperty("canton_id").GetInt32());
        Assert.Equal(distritoId, ubicacion.GetProperty("distrito_id").GetInt32());
    }

    [Fact]
    public void ToJson_SinDistrito_EnviaDistritoNulo_ParaElFallbackPorCanton()
    {
        var json = DenunciaJsonMapper.ToJson(RequestConUbicacion(78, null));

        using var doc = JsonDocument.Parse(json);
        var ubicacion = doc.RootElement.GetProperty("ubicacion");
        Assert.Equal(78, ubicacion.GetProperty("canton_id").GetInt32());
        Assert.Equal(JsonValueKind.Null, ubicacion.GetProperty("distrito_id").ValueKind);
    }

    [Fact]
    public void ToJson_DireccionEnBlanco_SeNormalizaANulo()
    {
        var json = DenunciaJsonMapper.ToJson(RequestConUbicacion(61, 367, direccion: "   "));

        using var doc = JsonDocument.Parse(json);
        Assert.Equal(JsonValueKind.Null,
            doc.RootElement.GetProperty("ubicacion").GetProperty("direccion_exacta").ValueKind);
    }

    // ---------- Servicio: la ubicacion llega intacta al repositorio ----------

    [Fact]
    public async Task RegistrarAsync_UbicacionValida_EnviaAlRepositorioElCantonYDistritoIndicados()
    {
        string? payloadRecibido = null;
        var mockRepo = new Mock<IDenunciaRepository>();
        mockRepo.Setup(r => r.RegistrarDenunciaAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((json, _, _) => payloadRecibido = json)
            .ReturnsAsync(new DenunciaRegistroResultado(1, "ABAN-2026-000001", "7K9M-QR2T-XW4P"));
        var servicio = new DenunciaService(mockRepo.Object);

        await servicio.RegistrarAsync(RequestConUbicacion(61, 367), CancellationToken.None);

        Assert.NotNull(payloadRecibido);
        using var doc = JsonDocument.Parse(payloadRecibido!);
        var ubicacion = doc.RootElement.GetProperty("ubicacion");
        Assert.Equal(61, ubicacion.GetProperty("canton_id").GetInt32());
        Assert.Equal(367, ubicacion.GetProperty("distrito_id").GetInt32());
    }

    [Fact]
    public async Task RegistrarAsync_RespuestaPublica_NoExponeAreaNiConsecutivoInterno()
    {
        var mockRepo = new Mock<IDenunciaRepository>();
        mockRepo.Setup(r => r.RegistrarDenunciaAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DenunciaRegistroResultado(1, "ABAN-2026-000001", "7K9M-QR2T-XW4P"));
        var servicio = new DenunciaService(mockRepo.Object);

        var resultado = await servicio.RegistrarAsync(RequestConUbicacion(61, 367), CancellationToken.None);

        var json = JsonSerializer.Serialize(resultado);
        Assert.Contains("7K9M-QR2T-XW4P", json);
        Assert.DoesNotContain("ABAN", json);
    }

    // ---------- Ubicacion invalida: no se intenta asignar area ----------

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task RegistrarAsync_CantonInvalido_LanzaValidacionYNoLlamaAlRepositorio(int cantonId)
    {
        var mockRepo = new Mock<IDenunciaRepository>();
        var servicio = new DenunciaService(mockRepo.Object);

        var ex = await Assert.ThrowsAsync<DenunciaValidationException>(
            () => servicio.RegistrarAsync(RequestConUbicacion(cantonId, null), CancellationToken.None));

        Assert.Contains(ex.Errores, e => e.Contains("canton valido", StringComparison.OrdinalIgnoreCase));
        mockRepo.Verify(r => r.RegistrarDenunciaAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---------- Sin ARS para la ubicacion (-20008) ----------

    [Fact]
    public async Task RegistrarAsync_SinAreaParaLaUbicacion_PropagaElMensajeDelSpSinReintentar()
    {
        const string mensaje = "No se encontró un Área Rectora de Salud (ARS) para la ubicación especificada.";
        var mockRepo = new Mock<IDenunciaRepository>();
        mockRepo.Setup(r => r.RegistrarDenunciaAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DenunciaBaseDatosException(mensaje, new Exception()));
        var servicio = new DenunciaService(mockRepo.Object);

        var ex = await Assert.ThrowsAsync<DenunciaBaseDatosException>(
            () => servicio.RegistrarAsync(RequestConUbicacion(61, 367), CancellationToken.None));

        Assert.Equal(mensaje, ex.Message);
        mockRepo.Verify(r => r.RegistrarDenunciaAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Middleware_ErrorDeSinAreaDelSp_Responde422ConElMensaje()
    {
        const string mensaje = "No se encontró un Área Rectora de Salud (ARS) para la ubicación especificada.";
        var middleware = new ManejadorDeErroresMiddleware(
            _ => throw new DenunciaBaseDatosException(mensaje, new Exception()),
            NullLogger<ManejadorDeErroresMiddleware>.Instance);
        var contexto = new DefaultHttpContext();
        contexto.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(contexto);

        Assert.Equal(StatusCodes.Status422UnprocessableEntity, contexto.Response.StatusCode);
        contexto.Response.Body.Position = 0;
        using var doc = await JsonDocument.ParseAsync(contexto.Response.Body);
        Assert.Equal(mensaje, doc.RootElement.GetProperty("error").GetString());
    }
}
