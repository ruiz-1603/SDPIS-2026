using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using SDPIS.Api.Models.DTOs;
using SDPIS.Api.Repositories;

namespace SDPIS.Api.Services.Autenticacion;

public class AutenticacionService(IProveedorAutenticacion proveedor,
    IServicioJwt servicioJwt, IBitacoraSesionRepository bitacoraSesion,
    ITokenRevocadoRepository tokenRevocados,
    IHttpContextAccessor contextAccesor) : IAutenticacionService
{
    public async Task<IniciarSesionResponse> IniciarSesionAsync(IniciarSesionRequest request, CancellationToken ct)
    {
        AutenticacionValidator.Validar(request);

        // El correo se compara y se audita en minusculas; la cedula tal cual.
        var codigo = request.CodigoUsuario.Trim();
        if (codigo.Contains('@'))
            codigo = codigo.ToLowerInvariant();

        var resultado = await proveedor.AutenticarAsync(new SolicitudAutenticacion(codigo, request.Clave), ct);
        if (resultado is null)
            throw new InvalidOperationException("El proveedor de autenticacion no devolvio un resultado.");

        if (resultado.Estado == EstadoAutenticacion.Exitosa)
            return await SesionExitosaAsync(resultado, codigo, ct);

        return await ManejarFalloAsync(resultado, codigo, ct);
    }

    private async Task<IniciarSesionResponse> SesionExitosaAsync(
        ResultadoAutenticacion resultado, string codigo, CancellationToken ct)
    {
        var usuario = resultado.DatosUsuario
            ?? throw new InvalidOperationException("Autenticacion exitosa sin datos de usuario.");

        var token = servicioJwt.GenerarToken(usuario);
        var expiraEn = DateTimeOffset.UtcNow.AddMinutes(servicioJwt.MinutosExpiracion);

        await RegistrarAsync(codigo, usuario.IdUsuario, "EXITO", null, ct);

        return new IniciarSesionResponse(
            token,
            expiraEn.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'"),
            new UsuarioAutenticadoDto(
                usuario.IdUsuario, usuario.Identificacion, usuario.Nombre,
                usuario.Apellido1, usuario.Apellido2, usuario.CorreoInstitucional, usuario.Rol,
                usuario.FuncionarioId, usuario.AreaId));
    }

    private async Task<IniciarSesionResponse> ManejarFalloAsync(
        ResultadoAutenticacion resultado, string codigo, CancellationToken ct)
        => resultado.Estado switch
        {
            // Clave correcta pero la cuenta no puede operar: mensaje propio.
            EstadoAutenticacion.CuentaDeshabilitada
                or EstadoAutenticacion.CuentaBloqueada => throw await FalloCuentaAsync(resultado, codigo, "DESHABILITADO", ct),
            EstadoAutenticacion.RolInvalido => throw await FalloCuentaAsync(resultado, codigo, "ROL_INVALIDO", ct),

            // Usuario inexistente o clave incorrecta: mensaje generico identico.
            _ => throw await FalloGenericoAsync(resultado, codigo, ct)
        };

    private async Task<CuentaDeshabilitadaException> FalloCuentaAsync(
        ResultadoAutenticacion resultado, string codigo, string resultadoBitacora, CancellationToken ct)
    {
        var mensaje = resultado.Mensaje
            ?? "La cuenta no puede iniciar sesion en este momento. Contacte a la administracion.";
        await RegistrarAsync(codigo, IdParaBitacora(resultado), resultadoBitacora, mensaje, ct);
        return new CuentaDeshabilitadaException(mensaje);
    }

    private async Task<CredencialesInvalidasException> FalloGenericoAsync(
        ResultadoAutenticacion resultado, string codigo, CancellationToken ct)
    {
        await RegistrarAsync(codigo, IdParaBitacora(resultado), "FALLO",
            "Credenciales invalidas o usuario inexistente.", ct);
        return new CredencialesInvalidasException("Credenciales invalidas. Verifique su usuario o clave.");
    }

    private static long? IdParaBitacora(ResultadoAutenticacion resultado)
        => resultado.DatosUsuario?.IdUsuario ?? resultado.IdUsuarioAuditoria;

    // Un solo punto de escritura en bitacora_sesion; la clave nunca se persiste.
    private async Task RegistrarAsync(string codigoUsuario, long? idUsuario,
        string resultado, string? motivo, CancellationToken ct)
        => await bitacoraSesion.RegistrarAsync(new RegistroBitacoraSesion(
            CodigoUsuario: codigoUsuario,
            IdUsuarioTemporal: idUsuario,
            Resultado: resultado,
            Motivo: motivo,
            DireccionIp: ObtenerIpCliente(),
            AgenteUsuario: ObtenerAgenteUsuario()), ct);

    private string? ObtenerIpCliente()
    {
        var ip = contextAccesor.HttpContext?.Connection.RemoteIpAddress?.ToString();
        return string.IsNullOrWhiteSpace(ip) ? null : ip;
    }

    private string? ObtenerAgenteUsuario()
    {
        var agente = contextAccesor.HttpContext?.Request.Headers.UserAgent.ToString();
        return string.IsNullOrWhiteSpace(agente) ? null : agente;
    }

    // HU-028: cierre de sesion. Es [Authorize], asi que el token ya fue
    // validado; se revoca en el servidor (token_revocado por JTI), se registra
    // el evento en bitacora_sesion y se responde con una confirmacion generica.
    // porInactividad distingue el cierre automatico del manual en ambas tablas.
    public async Task<ResultadoCierreSesion> CerrarSesionAsync(bool porInactividad, CancellationToken ct)
    {
        var motivoCierre = porInactividad ? "CIERRE_INACTIVIDAD" : "CIERRE_MANUAL";

        var principal = contextAccesor.HttpContext?.User;
        var identificacion = principal?.FindFirstValue(ClaimsSesion.Identificacion);
        var jti = principal?.FindFirstValue(JwtRegisteredClaimNames.Jti);
        var idUsuario = long.TryParse(principal?.FindFirstValue(JwtRegisteredClaimNames.Sub),
            System.Globalization.NumberStyles.Integer,
            System.Globalization.CultureInfo.InvariantCulture, out var id) ? id : (long?)null;

        // Revoca el token 1 sola vez (el JTI es UNIQUE).
        if (!string.IsNullOrWhiteSpace(jti)
            && !await tokenRevocados.EstaRevocadoAsync(jti, ct))
        {
            await tokenRevocados.RevocarAsync(new RegistroTokenRevocado(
                Jti: jti,
                Identificacion: identificacion ?? "",
                IdUsuarioTemporal: idUsuario,
                Motivo: motivoCierre), ct);
        }

        await RegistrarAsync(identificacion ?? "DESCONOCIDO", idUsuario, motivoCierre,
            porInactividad
                ? "Cierre automatico de sesion por inactividad."
                : "Cierre de sesion solicitado por el usuario.", ct);

        return new ResultadoCierreSesion("Sesión finalizada.");
    }
}