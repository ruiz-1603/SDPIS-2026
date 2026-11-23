using Moq;
using Microsoft.AspNetCore.Http;
using SDPIS.Api.Models.DTOs;
using SDPIS.Api.Repositories;
using SDPIS.Api.Services.Autenticacion;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace SDPIS.Api.Tests.Services;

public class AutenticacionServiceTests
{
    private readonly Mock<IProveedorAutenticacion> _proveedorMock = new();
    private readonly Mock<IServicioJwt> _jwtMock = new();
    private readonly Mock<IBitacoraSesionRepository> _bitacoraMock = new();
    private readonly Mock<ITokenRevocadoRepository> _tokenRevocadoMock = new();
    private readonly Mock<IHttpContextAccessor> _httpContextMock = new();
    private readonly AutenticacionService _servicio;

    public AutenticacionServiceTests()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");
        httpContext.Request.Headers.UserAgent = "TestAgent";
        _httpContextMock.Setup(c => c.HttpContext).Returns(httpContext);

        _servicio = new AutenticacionService(
            _proveedorMock.Object,
            _jwtMock.Object,
            _bitacoraMock.Object,
            _tokenRevocadoMock.Object,
            _httpContextMock.Object
        );
    }

    [Fact]
    public async Task IniciarSesionAsync_CredencialesValidas_RetornaTokenYRegistraExito()
    {
        // Arrange
        var request = new IniciarSesionRequest("test@misalud.go.cr", "Clave123!");
        var datosUsuario = new DatosUsuario(1, "123456789", "Test", "User", null, "test@misalud.go.cr", "Funcionario", 1, 1);
        
        _proveedorMock.Setup(p => p.AutenticarAsync(It.IsAny<SolicitudAutenticacion>(), default))
            .ReturnsAsync(new ResultadoAutenticacion(EstadoAutenticacion.Exitosa, null, "OK", datosUsuario));
        
        _jwtMock.Setup(j => j.GenerarToken(It.IsAny<DatosUsuario>())).Returns("fake_jwt_token");
        _jwtMock.Setup(j => j.MinutosExpiracion).Returns(60);

        // Act
        var response = await _servicio.IniciarSesionAsync(request, default);

        // Assert
        Assert.NotNull(response);
        Assert.Equal("fake_jwt_token", response.Token);
        Assert.Equal(1, response.Usuario.IdUsuario);
        
        _bitacoraMock.Verify(b => b.RegistrarAsync(It.Is<RegistroBitacoraSesion>(r => 
            r.CodigoUsuario == "test@misalud.go.cr" && 
            r.IdUsuarioTemporal == 1 && 
            r.Resultado == "EXITO"), default), Times.Once);
    }

    [Fact]
    public async Task IniciarSesionAsync_CredencialesInvalidas_LanzaCredencialesInvalidasExceptionYRegistraFallo()
    {
        // Arrange
        var request = new IniciarSesionRequest("invalido", "Clave123!");
        
        _proveedorMock.Setup(p => p.AutenticarAsync(It.IsAny<SolicitudAutenticacion>(), default))
            .ReturnsAsync(new ResultadoAutenticacion(EstadoAutenticacion.CredencialesInvalidas, null, null, null, 999));

        // Act & Assert
        await Assert.ThrowsAsync<CredencialesInvalidasException>(() => 
            _servicio.IniciarSesionAsync(request, default));

        _bitacoraMock.Verify(b => b.RegistrarAsync(It.Is<RegistroBitacoraSesion>(r => 
            r.CodigoUsuario == "invalido" && 
            r.IdUsuarioTemporal == 999 && 
            r.Resultado == "FALLO"), default), Times.Once);
    }

    [Fact]
    public async Task IniciarSesionAsync_CuentaDeshabilitada_LanzaCuentaDeshabilitadaExceptionYRegistra()
    {
        // Arrange
        var request = new IniciarSesionRequest("bloqueado@misalud.go.cr", "Clave123!");
        var datosUsuario = new DatosUsuario(2, "987654321", "Test", "User", null, "bloqueado@misalud.go.cr", "Funcionario", 1, 1);
        
        _proveedorMock.Setup(p => p.AutenticarAsync(It.IsAny<SolicitudAutenticacion>(), default))
            .ReturnsAsync(new ResultadoAutenticacion(EstadoAutenticacion.CuentaDeshabilitada, null, "Cuenta inactiva", datosUsuario));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<CuentaDeshabilitadaException>(() => 
            _servicio.IniciarSesionAsync(request, default));
            
        Assert.Equal("Cuenta inactiva", exception.Message);

        _bitacoraMock.Verify(b => b.RegistrarAsync(It.Is<RegistroBitacoraSesion>(r => 
            r.CodigoUsuario == "bloqueado@misalud.go.cr" && 
            r.IdUsuarioTemporal == 2 && 
            r.Resultado == "DESHABILITADO"), default), Times.Once);
    }

    [Fact]
    public async Task CerrarSesionAsync_CierreManual_RevocaTokenYRegistraCierreManual()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimsSesion.Identificacion, "123456789"),
            new Claim(JwtRegisteredClaimNames.Jti, "token-jti-123"),
            new Claim(JwtRegisteredClaimNames.Sub, "1")
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _httpContextMock.Object.HttpContext!.User = claimsPrincipal;
        
        _tokenRevocadoMock.Setup(t => t.EstaRevocadoAsync("token-jti-123", default)).ReturnsAsync(false);

        // Act
        var result = await _servicio.CerrarSesionAsync(porInactividad: false, default);

        // Assert
        Assert.Equal("Sesión finalizada.", result.Mensaje);
        
        _tokenRevocadoMock.Verify(t => t.RevocarAsync(It.Is<RegistroTokenRevocado>(r => 
            r.Jti == "token-jti-123" && 
            r.Identificacion == "123456789" && 
            r.IdUsuarioTemporal == 1 &&
            r.Motivo == "CIERRE_MANUAL"), default), Times.Once);
            
        _bitacoraMock.Verify(b => b.RegistrarAsync(It.Is<RegistroBitacoraSesion>(r => 
            r.Resultado == "CIERRE_MANUAL" && 
            r.IdUsuarioTemporal == 1), default), Times.Once);
    }
}
