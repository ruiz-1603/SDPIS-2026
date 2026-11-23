using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace SDPIS.Api.Services.Autenticacion;

public class ServicioJwt(IConfiguration configuration) : IServicioJwt
{
    public int MinutosExpiracion { get; } =
        configuration.GetValue<int>("Autenticacion:MinutosExpiracion", 60);

    public string GenerarToken(DatosUsuario usuario)
    {
        var claveBase64 = configuration["Autenticacion:ClaveFirmaJwt"]
            ?? throw new InvalidOperationException("No se encontro 'Autenticacion:ClaveFirmaJwt'. Configurela con dotnet user-secrets o ejecute tools/configurar-entorno.ps1.");
        byte[] claveFirma;
        try
        {
            claveFirma = Convert.FromBase64String(claveBase64);
        }
        catch (FormatException)
        {
            claveFirma = Encoding.UTF8.GetBytes(claveBase64);
        }

        // Seguridad: la clave de firma debe tener >= 32 bytes (256 bits).
        if (claveFirma.Length < 32)
            throw new InvalidOperationException(
                "'Autenticacion:ClaveFirmaJwt' debe tener al menos 256 bits (32 bytes).");

        var emisor = configuration["Autenticacion:Emisor"] ?? "SDPIS";
        var audiencia = configuration["Autenticacion:Audiencia"] ?? "SDPIS-Web";
        var ahora = DateTimeOffset.UtcNow;
        var expiracion = ahora.AddMinutes(MinutosExpiracion);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuario.IdUsuario.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new Claim(JwtRegisteredClaimNames.UniqueName, usuario.Identificacion),
            new Claim(ClaimsSesion.Identificacion, usuario.Identificacion),
            new Claim(ClaimsSesion.Nombre, usuario.Nombre),
            new Claim(ClaimsSesion.Apellido1, usuario.Apellido1),
            new Claim(ClaimsSesion.Apellido2, usuario.Apellido2 ?? ""),
            new Claim(ClaimsSesion.Correo, usuario.CorreoInstitucional ?? ""),
            new Claim(ClaimTypes.Role, usuario.Rol),
            new Claim(ClaimsSesion.Rol, usuario.Rol),
            // Funcionario y area del usuario: las reglas de negocio trabajan
            // con funcionario.id_funcionario y su area, no con el usuario temporal.
            new Claim(ClaimsSesion.FuncionarioId, usuario.FuncionarioId.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new Claim(ClaimsSesion.AreaId, usuario.AreaId.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };

        var credenciales = new SigningCredentials(
            new SymmetricSecurityKey(claveFirma), SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: emisor,
            audience: audiencia,
            claims: claims,
            notBefore: ahora.UtcDateTime,
            expires: expiracion.UtcDateTime,
            signingCredentials: credenciales);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}