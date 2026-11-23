using SDPIS.Api.Models.DTOs;
using SDPIS.Api.Services.Validation;
using Xunit;

namespace SDPIS.Api.Tests.Services.Validation;

public class CriteriosTecnicosValidatorTests
{
    private static RegistrarCriteriosTecnicosRequest RequestRegistrarValido() => new(
        FuncionarioValidadorId: 100,
        VinculadoComercioElectronico: "S",
        DanoPotencialSalud: "Alto",
        RequiereCriterioEspecializado: "Si",
        ObservacionesTecnicas: "Observacion de prueba"
    );

    private static ActualizarCriterioTecnicoRequest RequestActualizarCriterioValido() => new(
        RolFuncionarioSimulado: "superusuario",
        FuncionarioId: 1,
        ValorSeleccionado: "Alto"
    );

    private static ActualizarMetadataCriteriosTecnicosRequest RequestActualizarMetadataValido() => new(
        RolFuncionarioSimulado: "superusuario",
        FuncionarioId: 1,
        VinculadoComercioElectronico: "N",
        ObservacionesTecnicas: null
    );

    [Fact]
    public void Validar_Registrar_ConDatosValidos_NoLanzaExcepcion()
    {
        var exception = Record.Exception(() => CriteriosTecnicosValidator.Validar(RequestRegistrarValido()));

        Assert.Null(exception);
    }

    [Fact]
    public void Validar_Registrar_SinFuncionarioValidador_LanzaExcepcion()
    {
        var request = RequestRegistrarValido() with { FuncionarioValidadorId = 0 };

        var exception = Assert.Throws<DenunciaValidationException>(() => CriteriosTecnicosValidator.Validar(request));

        Assert.Contains(exception.Errores, e => e.Contains("funcionario validador"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("X")]
    [InlineData("s")]
    public void Validar_Registrar_ComercioElectronicoInvalido_LanzaExcepcion(string valor)
    {
        var request = RequestRegistrarValido() with { VinculadoComercioElectronico = valor };

        var exception = Assert.Throws<DenunciaValidationException>(() => CriteriosTecnicosValidator.Validar(request));

        Assert.Contains(exception.Errores, e => e.Contains("comercio electronico"));
    }

    [Fact]
    public void Validar_Registrar_SinDanoPotencialSalud_LanzaExcepcion()
    {
        var request = RequestRegistrarValido() with { DanoPotencialSalud = "" };

        var exception = Assert.Throws<DenunciaValidationException>(() => CriteriosTecnicosValidator.Validar(request));

        Assert.Contains(exception.Errores, e => e.Contains("Dano potencial a la salud"));
    }

    [Fact]
    public void Validar_Registrar_SinRequiereCriterioEspecializado_LanzaExcepcion()
    {
        var request = RequestRegistrarValido() with { RequiereCriterioEspecializado = "" };

        var exception = Assert.Throws<DenunciaValidationException>(() => CriteriosTecnicosValidator.Validar(request));

        Assert.Contains(exception.Errores, e => e.Contains("Criterio tecnico especializado UVC"));
    }

    [Fact]
    public void Validar_Registrar_ObservacionesDemasiadoLargas_LanzaExcepcion()
    {
        var request = RequestRegistrarValido() with { ObservacionesTecnicas = new string('a', 1001) };

        var exception = Assert.Throws<DenunciaValidationException>(() => CriteriosTecnicosValidator.Validar(request));

        Assert.Contains(exception.Errores, e => e.Contains("1000 caracteres"));
    }

    [Fact]
    public void Validar_ActualizarCriterio_ConDatosValidos_NoLanzaExcepcion()
    {
        var exception = Record.Exception(() => CriteriosTecnicosValidator.Validar(RequestActualizarCriterioValido()));

        Assert.Null(exception);
    }

    [Fact]
    public void Validar_ActualizarCriterio_SinRolSimulado_LanzaExcepcion()
    {
        var request = RequestActualizarCriterioValido() with { RolFuncionarioSimulado = "" };

        var exception = Assert.Throws<DenunciaValidationException>(() => CriteriosTecnicosValidator.Validar(request));

        Assert.Contains(exception.Errores, e => e.Contains("rol"));
    }

    [Fact]
    public void Validar_ActualizarCriterio_SinValorSeleccionado_LanzaExcepcion()
    {
        var request = RequestActualizarCriterioValido() with { ValorSeleccionado = "" };

        var exception = Assert.Throws<DenunciaValidationException>(() => CriteriosTecnicosValidator.Validar(request));

        Assert.Contains(exception.Errores, e => e.Contains("valor"));
    }

    [Fact]
    public void Validar_ActualizarMetadata_ConDatosValidos_NoLanzaExcepcion()
    {
        var exception = Record.Exception(() => CriteriosTecnicosValidator.Validar(RequestActualizarMetadataValido()));

        Assert.Null(exception);
    }

    [Fact]
    public void Validar_ActualizarMetadata_ComercioElectronicoInvalido_LanzaExcepcion()
    {
        var request = RequestActualizarMetadataValido() with { VinculadoComercioElectronico = "X" };

        var exception = Assert.Throws<DenunciaValidationException>(() => CriteriosTecnicosValidator.Validar(request));

        Assert.Contains(exception.Errores, e => e.Contains("comercio electronico"));
    }

    [Theory]
    [InlineData("superusuario")]
    [InlineData("Superusuario")]
    [InlineData("SUPERUSUARIO")]
    public void ValidarRolSuperusuario_ConRolSuperusuario_NoLanzaExcepcion(string rol)
    {
        var exception = Record.Exception(() => CriteriosTecnicosValidator.ValidarRolSuperusuario(rol));

        Assert.Null(exception);
    }

    [Theory]
    [InlineData("validador")]
    [InlineData("jefe_area")]
    [InlineData("")]
    public void ValidarRolSuperusuario_ConRolDistinto_LanzaExcepcion(string rol)
    {
        // Criterio de aceptacion #4: solo el superusuario puede modificar.
        Assert.Throws<DenunciaValidationException>(() => CriteriosTecnicosValidator.ValidarRolSuperusuario(rol));
    }

    // Panel de justificacion obligatorio (edicion de un criterio ya
    // registrado): solo se llama desde el servicio cuando aplica, por eso se
    // prueba el metodo directamente en vez de a traves de Validar(request).
    [Fact]
    public void ValidarJustificacionEdicion_ConTextoValido_NoLanzaExcepcion()
    {
        var exception = Record.Exception(
            () => CriteriosTecnicosValidator.ValidarJustificacionEdicion("Se corrige tras revisar el expediente."));

        Assert.Null(exception);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("corto")]
    public void ValidarJustificacionEdicion_SinTextoSuficiente_LanzaExcepcion(string? justificacion)
    {
        var exception = Assert.Throws<DenunciaValidationException>(
            () => CriteriosTecnicosValidator.ValidarJustificacionEdicion(justificacion));

        Assert.Contains(exception.Errores, e => e.Contains("justificación"));
    }
}