namespace SDPIS.Api.Repositories;

// HU-001: acceso a usuario_temporal (tabla local de HU-001). La autenticacion
// real la hace el MSI; esta tabla es el respaldo local para desarrollo y
// para cuando el MSI no este disponible. Nunca expone clave_hash fuera.
public interface IUsuarioTemporalRepository
{
    // Busca por identificacion (cedula) o correo institucional: ambos
    // campos son UNIQUE y se usan como codigo de usuario.
    Task<UsuarioTemporalAuth?> ObtenerPorCodigoAsync(string codigoUsuario, CancellationToken ct);

    // Cuenta de inicios de sesion; la usa la bitacora del proveedor MSI
    // (no aplica al provider local, que guarda cada intento individual).
    Task<int> ContarExitosAsync(long idUsuarioTemporal, CancellationToken ct);
}