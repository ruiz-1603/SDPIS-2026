namespace SDPIS.Api.Services.Autenticacion;

// Centraliza la traduccion de roles externos (MSI/local) a los roles SDPIS.
// Es el unico lugar donde se conocen los nombres de rol validos, para que
// cambios en el esquema (p. ej. el check de usuario_temporal) se reflejen
// aqui sin tocar controladores ni servicios.
public interface IMapeadorRol
{
    // Roles SDPIS validos (coinciden con CK_USUARIO_TEMPORAL_ROL).
    string[] RolesValidos { get; }

    // Normaliza el rol origen a un rol SDPIS; null si el rol es desconocido.
    string? Mapear(string? rolOrigen);
}