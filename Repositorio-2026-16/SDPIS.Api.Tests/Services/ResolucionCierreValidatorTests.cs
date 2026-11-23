using SDPIS.Api.Models.DTOs;
using SDPIS.Api.Repositories;
using SDPIS.Api.Services.GestionUsuarios;
using SDPIS.Api.Services.Validation;
using Xunit;

namespace SDPIS.Api.Tests.Services;

// HU-017: entradas de la decision de la jefatura y traduccion de los errores
// de sp_resolver_solicitud_cierre a las respuestas HTTP del middleware.
public class ResolucionCierreValidatorTests
{
    private const int Maximo = ResolucionCierreValidator.LongitudMaximaObservaciones;

    // ---------- Validador ----------

    [Fact]
    public void ValidarAprobacion_SinObservaciones_EsValida()
    {
        ResolucionCierreValidator.ValidarAprobacion(1, null);
        ResolucionCierreValidator.ValidarAprobacion(1, new ResolverCierreRequest(null));
        ResolucionCierreValidator.ValidarAprobacion(1, new ResolverCierreRequest("  "));
    }

    [Fact]
    public void ValidarRechazo_ConObservaciones_EsValida()
        => ResolucionCierreValidator.ValidarRechazo(1, new ResolverCierreRequest("Ajustar la resolucion."));

    [Fact]
    public void ValidarRechazo_SinObservaciones_LanzaValidacion()
    {
        var ex = Assert.Throws<DenunciaValidationException>(() =>
            ResolucionCierreValidator.ValidarRechazo(1, new ResolverCierreRequest(" ")));

        Assert.Contains("Debe indicar las observaciones de la jefatura para rechazar la resolución.", ex.Errores);
    }

    // Exactamente 2000 caracteres (despues de recortar espacios) se acepta.
    [Fact]
    public void ValidarRechazo_ObservacionesEnElLimite_EsValida()
        => ResolucionCierreValidator.ValidarRechazo(1, new ResolverCierreRequest("  " + new string('a', Maximo) + "  "));

    [Fact]
    public void ValidarAprobacion_ObservacionesDemasiadoLargas_LanzaValidacion()
    {
        var ex = Assert.Throws<DenunciaValidationException>(() =>
            ResolucionCierreValidator.ValidarAprobacion(1, new ResolverCierreRequest(new string('a', Maximo + 1))));

        Assert.Contains(ex.Errores, e => e.Contains("2000"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void ValidarAprobacion_SolicitudInvalida_LanzaValidacion(long solicitudId)
    {
        var ex = Assert.Throws<DenunciaValidationException>(() =>
            ResolucionCierreValidator.ValidarAprobacion(solicitudId, null));

        Assert.Contains("Debe indicar una solicitud de cierre válida.", ex.Errores);
    }

    // ---------- Errores del SP -> HTTP ----------

    [Theory]
    [InlineData(20170, typeof(DenunciaValidationException))]   // 400
    [InlineData(20171, typeof(DenunciaValidationException))]   // 400
    [InlineData(20172, typeof(RecursoNoEncontradoException))]  // 404
    [InlineData(20173, typeof(PermisoDenegadoException))]      // 403
    [InlineData(20174, typeof(ReglaNegocioException))]         // 422
    [InlineData(20175, typeof(ReglaNegocioException))]         // 422
    [InlineData(20999, typeof(DenunciaBaseDatosException))]    // 422 (generico)
    public void Mapear_ErrorDelSp_DevuelveLaExcepcionDelMiddleware(int numero, Type esperado)
    {
        var ex = ResolucionCierreErrores.Mapear(numero, $"ORA-{numero}: Mensaje", new Exception());

        Assert.IsType(esperado, ex);
    }

    // El mensaje en espanol llega limpio, sin el codigo ORA ni la pila de PL/SQL.
    [Fact]
    public void Mapear_MensajeOracle_QuitaCodigoYPila()
    {
        const string oracle = "ORA-20174: La solicitud de cierre ya fue resuelta por la jefatura.\n"
            + "ORA-06512: at \"SDPIS.SP_RESOLVER_SOLICITUD_CIERRE\", line 108\nORA-06512: at line 1";

        var ex = ResolucionCierreErrores.Mapear(20174, oracle, new Exception());

        Assert.Equal("La solicitud de cierre ya fue resuelta por la jefatura.", ex.Message);
    }

    [Fact]
    public void Mapear_ValidacionDelSp_DejaElMensajeEnLosErrores()
    {
        var ex = Assert.IsType<DenunciaValidationException>(
            ResolucionCierreErrores.Mapear(20171, "ORA-20171: Debe indicar las observaciones.", new Exception()));

        Assert.Equal(["Debe indicar las observaciones."], ex.Errores);
    }
}
