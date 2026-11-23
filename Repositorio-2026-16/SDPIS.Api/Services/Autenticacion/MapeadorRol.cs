namespace SDPIS.Api.Services.Autenticacion;

public class MapeadorRol : IMapeadorRol
{
    public string[] RolesValidos { get; } = ["validador", "jefe_area", "superusuario"];

    private readonly HashSet<string> _roles = new(StringComparer.Ordinal) { "validador", "jefe_area", "superusuario" };

    public string? Mapear(string? rolOrigen)
    {
        if (string.IsNullOrWhiteSpace(rolOrigen))
            return null;

        var rol = rolOrigen.Trim().ToLowerInvariant();
        return _roles.Contains(rol) ? rol : null;
    }
}