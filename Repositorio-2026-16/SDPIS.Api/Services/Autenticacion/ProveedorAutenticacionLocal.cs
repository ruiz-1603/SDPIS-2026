using SDPIS.Api.Repositories;

namespace SDPIS.Api.Services.Autenticacion;

// Autenticacion contra la tabla local usuario_temporal (HU-001).
// Contrato de seguridad:
//  * bcrypt con costo 12.
//  * Tiempo constante: cuando la identificacion no existe se valida la clave
//    contra un hash señuelo fijo para no filtrar existencia del usuario.
//  * Credenciales inexistentes e incorrectas devuelven el MISMO estado
//    (CredencialesInvalidas): el API siempre responde el mensaje generico.
//  * CuentaDeshabilitada solo se devuelve tras verificar la clave (no se da
//    informacion de cuentas inexistentes ni de claves con cuenta inactiva).
//  * Rol y estado activo se toman SOLO de funcionario:
//      funcionario inactivo -> CuentaDeshabilitada (403);
//      rol del funcionario sin mapeo SDPIS -> RolInvalido (403).
public class ProveedorAutenticacionLocal(ILogger<ProveedorAutenticacionLocal> logger,
    IUsuarioTemporalRepository repositorio, IMapeadorRol mapeadorRol) : IProveedorAutenticacion
{
    // Hash señuelo (costo 12) para tiempo constante cuando el usuario no existe.
    private static readonly string HashSenuelo = "$2a$12$82Eci5Pst.S1QA9v86ssquHqob.IOmXJAKJYTf3CRe4iJHLvwvJ.e";

    public async Task<ResultadoAutenticacion> AutenticarAsync(SolicitudAutenticacion solicitud, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(solicitud.CodigoUsuario) || string.IsNullOrWhiteSpace(solicitud.Clave))
            return new ResultadoAutenticacion(EstadoAutenticacion.CredencialesInvalidas,
                CodigoOrigen: null, Mensaje: null, DatosUsuario: null);

        var usuario = await repositorio.ObtenerPorCodigoAsync(solicitud.CodigoUsuario.Trim(), ct);

        // Tiempo constante: validar clave contra el hash senuelo si no existe.
        if (usuario is null)
        {
            BCrypt.Net.BCrypt.Verify(solicitud.Clave, HashSenuelo);
            return FalloGenerico(null);
        }

        bool claveValida;
        try
        {
            claveValida = BCrypt.Net.BCrypt.Verify(solicitud.Clave, usuario.ClaveHash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // Hash corrupto en la base de datos: tratar como clave invalida
            // y nunca exponer el detalle al cliente.
            logger.LogWarning("Hash de clave corrupto para la identificacion {Identificacion}.", usuario.Identificacion);
            return FalloGenerico(usuario.IdUsuarioTemporal);
        }

        if (!claveValida)
            return FalloGenerico(usuario.IdUsuarioTemporal);

        // Solo tras verificar la clave se revela el estado de la cuenta. Rol y
        // estado activo son permisos internos del SDPIS: se toman SOLO del
        // funcionario (igual que con el MSI, que no los conoce), asi un cambio
        // de rol o una desactivacion en funcionario rige de inmediato.
        if (!usuario.Activo)
            return new ResultadoAutenticacion(EstadoAutenticacion.CuentaDeshabilitada,
                CodigoOrigen: null,
                Mensaje: "La cuenta esta desactivada. Contacte a la administracion.",
                DatosUsuario: null,
                IdUsuarioAuditoria: usuario.IdUsuarioTemporal);

        // El rol del funcionario debe existir en el SDPIS.
        var rol = mapeadorRol.Mapear(usuario.Rol);
        if (rol is null)
            return new ResultadoAutenticacion(EstadoAutenticacion.RolInvalido,
                CodigoOrigen: null,
                Mensaje: "El rol del usuario no esta configurado en el SDPIS. Contacte a la administracion.",
                DatosUsuario: null,
                IdUsuarioAuditoria: usuario.IdUsuarioTemporal);

        return new ResultadoAutenticacion(
            EstadoAutenticacion.Exitosa,
            CodigoOrigen: "LOCAL",
            Mensaje: null,
            DatosUsuario: new DatosUsuario(usuario.IdUsuarioTemporal, usuario.Identificacion,
                usuario.Nombre, usuario.Apellido1, usuario.Apellido2, usuario.CorreoInstitucional, rol,
                usuario.FuncionarioId, usuario.AreaId));
    }

    // idUsuario solo viaja para la bitacora (auditoria interna); el cliente
    // recibe el mismo mensaje generico exista o no el usuario.
    private static ResultadoAutenticacion FalloGenerico(long? idUsuario)
        => new(EstadoAutenticacion.CredencialesInvalidas, CodigoOrigen: null,
            Mensaje: "Credenciales invalidas. Verifique su usuario o clave.", DatosUsuario: null,
            IdUsuarioAuditoria: idUsuario);
}