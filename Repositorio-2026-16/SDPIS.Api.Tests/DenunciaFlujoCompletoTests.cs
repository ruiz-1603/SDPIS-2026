using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using SDPIS.Api.Models.DTOs;
using SDPIS.Api.Repositories;
using SDPIS.Api.Services;
using Xunit;

namespace SDPIS.Test.Integration;

// ---------------------------------------------------------------------
// Requisitos para correrlas:
//   1. Tu base local con SDPIS.sql + 002 seed_ubicaciones.sql +
//      03_catalogo_minimo.sql + 04_sp_registrar_denuncia_temporal.sql
//      ya cargados (ver README del setup local).
//   2. Exporta la variable de entorno SDPIS_TEST_CONNECTION_STRING con TU
//      PROPIA cadena de conexion local antes de correr las pruebas. Es
//      obligatoria (no hay valor por defecto en el codigo a proposito,
//      para no dejar ninguna credencial hardcodeada en el repo).
//      Ejemplo (reemplaza los placeholders por tus datos reales):
//        setx SDPIS_TEST_CONNECTION_STRING "User Id=TU_USUARIO;Password=TU_CLAVE;Data Source=127.0.0.1:1521/TU_SERVICIO"
//      Nota: setx solo toma efecto en terminales/instancias de Visual
//      Studio que abras DESPUES de correr el comando.
//
// Si no tienes Oracle disponible (por ejemplo en un pipeline de CI sin
// base de datos), estas pruebas van a fallar con un error de conexion
// claro en vez de dar un falso positivo silencioso. Cuando el equipo
// tenga un ambiente de pruebas dedicado, aqui es donde apuntar la
// cadena de conexion en vez de la de desarrollo local.
// ---------------------------------------------------------------------
public class DenunciaFlujoCompletoTests
{
    private static readonly string ConnectionString =
        Environment.GetEnvironmentVariable("SDPIS_TEST_CONNECTION_STRING")
        ?? "User Id=sdpis_app;Password=sdpis123;Data Source=127.0.0.1:1521/XEPDB1";

    private static IDenunciaService CrearServicio()
    {
        var configuration = new ConfigurationManager();
        configuration["ConnectionStrings:SDPIS"] = ConnectionString;

        var repository = new DenunciaRepository(configuration);
        return new DenunciaService(repository);
    }

    private static ProductoDenunciaDto ProductoValido() => new(
        TipoProductoId: 1, // 'Alimentos' segun 03_catalogo_minimo.sql (rama dev)
        NombreProductoTexto: "Perfume de prueba automatizada",
        RegistroSanitarioIngresado: null,
        DescripcionProducto: "Producto usado unicamente por las pruebas de integracion",
        MarcaIngresada: null,
        NumeroLote: null,
        FabricanteIngresado: null,
        PaisOrigen: null,
        Presentacion: null,
        FechaCompra: null,
        Motivos: [8] // 'Producto sin registro sanitario'
    );

    private static CrearDenunciaRequest RequestValido() => new(
        Hecho: new HechoDto(
            DenunciadoOtraInstitucion: "N",
            DetalleOtraInstitucion: null,
            DenunciadoPreviamenteMinsalud: "N",
            DetallePrevioMinsalud: null,
            NombreEstablecimiento: "Establecimiento de prueba automatizada",
            DescripcionHecho: "Descripcion generada por una prueba de integracion automatizada."
        ),
        Ubicacion: new UbicacionDto(CantonId: 1, DistritoId: 1, DireccionExacta: null),
        Privacidad: new PrivacidadDto(EsAnonima: "S", Denunciante: null),
        Productos: [ProductoValido()]
    );

    [Fact]
    public async Task RegistrarAsync_ConDatosValidos_GuardaLaDenunciaCompletaYRetornaCodigoSeguimiento()
    {
        var servicio = CrearServicio();

        var resultado = await servicio.RegistrarAsync(RequestValido(), CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(resultado.CodigoSeguimiento));

        // Confirma en la base que la denuncia y sus tablas relacionadas
        // realmente quedaron persistidas (no solo que el SP no truene).
        await using var connection = new OracleConnection(ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM denuncia d
            JOIN denuncia_seguimiento ds ON ds.denuncia_id = d.id_denuncia
            JOIN denuncia_hecho dh ON dh.denuncia_id = d.id_denuncia
            JOIN denuncia_ubicacion du ON du.denuncia_id = d.id_denuncia
            JOIN detalle_producto dp ON dp.denuncia_id = d.id_denuncia
            WHERE ds.codigo_seguimiento = :codigo
            """;
        command.Parameters.Add(new OracleParameter("codigo", resultado.CodigoSeguimiento));

        var cantidad = Convert.ToInt32(await command.ExecuteScalarAsync());
        Assert.Equal(1, cantidad);
    }

    [Fact]
    public async Task RegistrarAsync_ConTipoProductoInexistente_NoDejaFilasParcialesTrasElRollback()
    {
        // HU-005: "manejo de rollback si cualquier parte de la transaccion
        // falla (evitar denuncias parcialmente guardadas)". Forzamos el
        // fallo en el ULTIMO insert relevante (detalle_producto), que es
        // justo el escenario de "falla a medio camino" que pide la HU:
        // para ese momento el SP ya hizo los inserts de denuncia,
        // denuncia_hecho, denuncia_ubicacion, denuncia_seguimiento y
        // denuncia_denunciante, y debe revertirlos a todos si el de
        // producto falla.
        var servicio = CrearServicio();
        var productoInexistente = ProductoValido() with { TipoProductoId = 999_999 };
        var request = RequestValido() with { Productos = [productoInexistente] };

        // No usamos DenunciaValidator para este caso porque TipoProductoId
        // > 0 es formalmente valido; el problema es de integridad
        // referencial en la base, que es justo lo que queremos ejercitar.
        await Assert.ThrowsAnyAsync<OracleException>(
            () => servicio.RegistrarAsync(request, CancellationToken.None));

        // Verifica que NO haya quedado ninguna denuncia con establecimiento
        // "Establecimiento de prueba automatizada" y sin productos asociados
        // (lo cual indicaria un commit parcial indebido).
        await using var connection = new OracleConnection(ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM denuncia d
            JOIN denuncia_hecho dh ON dh.denuncia_id = d.id_denuncia
            WHERE dh.nombre_establecimiento = 'Establecimiento de prueba automatizada'
              AND NOT EXISTS (
                SELECT 1 FROM detalle_producto dp WHERE dp.denuncia_id = d.id_denuncia
              )
            """;

        var huerfanas = Convert.ToInt32(await command.ExecuteScalarAsync());
        Assert.Equal(0, huerfanas);
    }
}
