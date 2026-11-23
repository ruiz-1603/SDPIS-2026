using SDPIS.Api.Models.DTOs;
using SDPIS.Api.Services.Validation;
using Xunit;

namespace SDPIS.Test.Validation;

public class DenunciaValidatorTests
{
    // ---------------------------------------------------------------
    // Builders: arman un request "base" valido, y cada prueba lo muta
    // solo en el campo que quiere invalidar (los demas quedan correctos,
    // para asegurar que el error reportado es el que se esta probando).
    // ---------------------------------------------------------------
    private static ProductoDenunciaDto ProductoValido() => new(
        TipoProductoId: 1,
        NombreProductoTexto: "Perfume",
        RegistroSanitarioIngresado: null,
        DescripcionProducto: "Perfume con etiqueta enganosa",
        MarcaIngresada: null,
        NumeroLote: null,
        FabricanteIngresado: null,
        PaisOrigen: null,
        Presentacion: null,
        FechaCompra: null,
        Motivos: [9]
    );

    private static CrearDenunciaRequest RequestValidoConfidencial() => new(
        Hecho: new HechoDto(
            DenunciadoOtraInstitucion: "N",
            DetalleOtraInstitucion: null,
            DenunciadoPreviamenteMinsalud: "N",
            DetallePrevioMinsalud: null,
            NombreEstablecimiento: "Farmacia Central",
            DescripcionHecho: "Se vendio un producto sin registro sanitario visible en el empaque."
        ),
        Ubicacion: new UbicacionDto(CantonId: 1, DistritoId: null, DireccionExacta: null),
        Privacidad: new PrivacidadDto(
            EsAnonima: "N",
            Denunciante: new DenuncianteDto(
                Nombre: "Bryan Lopez",
                NumeroIdentificacion: "123456789",
                Correo: null,
                Telefono: null,
                DireccionDomicilio: null
            )
        ),
        Productos: [ProductoValido()]
    );

    private static CrearDenunciaRequest RequestValidoAnonimo() => RequestValidoConfidencial() with
    {
        Privacidad = new PrivacidadDto(EsAnonima: "S", Denunciante: null)
    };

    private static DenunciaValidationException CapturarErrores(CrearDenunciaRequest request)
        => Assert.Throws<DenunciaValidationException>(() => DenunciaValidator.Validar(request));

    // ---------------------------------------------------------------
    // Casos felices
    // ---------------------------------------------------------------
    [Fact]
    public void Validar_ConDatosValidosConfidencial_NoLanzaExcepcion()
    {
        var ex = Record.Exception(() => DenunciaValidator.Validar(RequestValidoConfidencial()));
        Assert.Null(ex);
    }

    [Fact]
    public void Validar_ConDatosValidosAnonima_NoLanzaExcepcion()
    {
        var ex = Record.Exception(() => DenunciaValidator.Validar(RequestValidoAnonimo()));
        Assert.Null(ex);
    }

    [Fact]
    public void Validar_AnonimaConDenuncianteNulo_NoExigeDatosDeDenunciante()
    {
        // Aunque Denunciante sea null, en modo anonimo no debe generar error.
        var request = RequestValidoConfidencial() with
        {
            Privacidad = new PrivacidadDto(EsAnonima: "S", Denunciante: null)
        };

        var ex = Record.Exception(() => DenunciaValidator.Validar(request));
        Assert.Null(ex);
    }

    // ---------------------------------------------------------------
    // HU-002: Hecho
    // ---------------------------------------------------------------
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Validar_SinNombreEstablecimiento_LanzaError(string? nombre)
    {
        var request = RequestValidoConfidencial() with
        {
            Hecho = RequestValidoConfidencial().Hecho with { NombreEstablecimiento = nombre! }
        };

        var ex = CapturarErrores(request);
        Assert.Contains(ex.Errores, e => e.Contains("nombre del establecimiento", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("corta")]        // 5 caracteres, minimo 10
    [InlineData("123456789")]    // 9 caracteres, todavia insuficiente
    public void Validar_ConDescripcionHechoMuyCorta_LanzaError(string descripcion)
    {
        var request = RequestValidoConfidencial() with
        {
            Hecho = RequestValidoConfidencial().Hecho with { DescripcionHecho = descripcion }
        };

        var ex = CapturarErrores(request);
        Assert.Contains(ex.Errores, e => e.Contains("descripcion cronologica", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validar_ConDescripcionHechoDeExactamente10Caracteres_NoLanzaError()
    {
        var request = RequestValidoConfidencial() with
        {
            Hecho = RequestValidoConfidencial().Hecho with { DescripcionHecho = "1234567890" } // exactamente 10
        };

        var ex = Record.Exception(() => DenunciaValidator.Validar(request));
        Assert.Null(ex);
    }

    // ---------------------------------------------------------------
    // HU-002: Privacidad
    // ---------------------------------------------------------------
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("X")]
    public void Validar_ConEsAnonimaInvalido_LanzaError(string? valor)
    {
        var request = RequestValidoConfidencial() with
        {
            Privacidad = new PrivacidadDto(EsAnonima: valor!, Denunciante: null)
        };

        var ex = CapturarErrores(request);
        Assert.Contains(ex.Errores, e => e.Contains("anonima o confidencial", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validar_ConfidencialSinDenunciante_LanzaAmbosErrores()
    {
        var request = RequestValidoConfidencial() with
        {
            Privacidad = new PrivacidadDto(EsAnonima: "N", Denunciante: null)
        };

        var ex = CapturarErrores(request);
        Assert.Contains(ex.Errores, e => e.Contains("nombre del denunciante", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(ex.Errores, e => e.Contains("numero de identificacion", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validar_ConfidencialSinNombreDenunciante_LanzaError()
    {
        var baseReq = RequestValidoConfidencial();
        var request = baseReq with
        {
            Privacidad = baseReq.Privacidad with
            {
                Denunciante = baseReq.Privacidad.Denunciante! with { Nombre = "" }
            }
        };

        var ex = CapturarErrores(request);
        Assert.Contains(ex.Errores, e => e.Contains("nombre del denunciante", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("")]
    [InlineData("123")] // 3 caracteres, minimo 5
    public void Validar_ConfidencialConNumeroIdentificacionMuyCorto_LanzaError(string numeroIdentificacion)
    {
        var baseReq = RequestValidoConfidencial();
        var request = baseReq with
        {
            Privacidad = baseReq.Privacidad with
            {
                Denunciante = baseReq.Privacidad.Denunciante! with { NumeroIdentificacion = numeroIdentificacion }
            }
        };

        var ex = CapturarErrores(request);
        Assert.Contains(ex.Errores, e => e.Contains("numero de identificacion", StringComparison.OrdinalIgnoreCase));
    }

    // ---------------------------------------------------------------
    // HU-002: Ubicacion
    // ---------------------------------------------------------------
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validar_ConCantonIdInvalido_LanzaError(int cantonId)
    {
        var request = RequestValidoConfidencial() with
        {
            Ubicacion = RequestValidoConfidencial().Ubicacion with { CantonId = cantonId }
        };

        var ex = CapturarErrores(request);
        Assert.Contains(ex.Errores, e => e.Contains("canton valido", StringComparison.OrdinalIgnoreCase));
    }

    // ---------------------------------------------------------------
    // HU-004: Productos
    // ---------------------------------------------------------------
    [Fact]
    public void Validar_SinProductos_LanzaError()
    {
        var request = RequestValidoConfidencial() with { Productos = [] };

        var ex = CapturarErrores(request);
        Assert.Contains(ex.Errores, e => e.Contains("al menos un producto", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validar_ProductoSinNombre_LanzaError()
    {
        var producto = ProductoValido() with { NombreProductoTexto = "" };
        var request = RequestValidoConfidencial() with { Productos = [producto] };

        var ex = CapturarErrores(request);
        Assert.Contains(ex.Errores, e => e.Contains("nombre del producto", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("")]
    [InlineData("abcd")] // 4 caracteres, minimo 5
    public void Validar_ProductoConDescripcionMuyCorta_LanzaError(string descripcion)
    {
        var producto = ProductoValido() with { DescripcionProducto = descripcion };
        var request = RequestValidoConfidencial() with { Productos = [producto] };

        var ex = CapturarErrores(request);
        Assert.Contains(ex.Errores, e => e.Contains("descripcion del producto", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Validar_ProductoSinTipoProductoValido_LanzaError(int tipoProductoId)
    {
        var producto = ProductoValido() with { TipoProductoId = tipoProductoId };
        var request = RequestValidoConfidencial() with { Productos = [producto] };

        var ex = CapturarErrores(request);
        Assert.Contains(ex.Errores, e => e.Contains("tipo de producto", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validar_ProductoSinMotivos_LanzaError()
    {
        var producto = ProductoValido() with { Motivos = [] };
        var request = RequestValidoConfidencial() with { Productos = [producto] };

        var ex = CapturarErrores(request);
        Assert.Contains(ex.Errores, e => e.Contains("motivo de denuncia", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validar_ConVariosProductos_IdentificaCadaUnoPorSuNumeroEnElMensaje()
    {
        var productoValido = ProductoValido();
        var productoInvalido = ProductoValido() with { NombreProductoTexto = "" };
        var request = RequestValidoConfidencial() with { Productos = [productoValido, productoInvalido] };

        var ex = CapturarErrores(request);
        Assert.Contains(ex.Errores, e => e.StartsWith("Producto #2", StringComparison.Ordinal));
    }

    // ---------------------------------------------------------------
    // Multiples errores a la vez
    // ---------------------------------------------------------------
    [Fact]
    public void Validar_ConMultiplesCamposInvalidos_AcumulaTodosLosErrores()
    {
        var request = new CrearDenunciaRequest(
            Hecho: new HechoDto(null, null, null, null, NombreEstablecimiento: "", DescripcionHecho: "corta"),
            Ubicacion: new UbicacionDto(CantonId: 0, DistritoId: null, DireccionExacta: null),
            Privacidad: new PrivacidadDto(EsAnonima: "N", Denunciante: null),
            Productos: []
        );

        var ex = CapturarErrores(request);

        // Hecho (2) + Privacidad (2) + Ubicacion (1) + Productos (1) = 6 errores
        Assert.Equal(6, ex.Errores.Count);
    }
}
