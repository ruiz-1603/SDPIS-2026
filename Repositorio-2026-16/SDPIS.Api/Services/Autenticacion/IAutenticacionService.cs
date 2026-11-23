using SDPIS.Api.Models.DTOs;

namespace SDPIS.Api.Services.Autenticacion;

// HU-001: orquesta el inicio de sesion: valida la forma, delega en el
// proveedor (Local/MSI), registra el intento en bitacora_sesion y emite
// el token JWT cuando las credenciales son exitosas.
// HU-028: orquesta el cierre de sesion: revoca el token JWT en el servidor
// y registra el evento en bitacora_sesion con usuario, fecha y hora.
public interface IAutenticacionService
{
    Task<IniciarSesionResponse> IniciarSesionAsync(IniciarSesionRequest request, CancellationToken ct);

    // Cierre de sesion del usuario autenticado (requiere [Authorize]).
    // Lee identificacion/jti de los claims del token vigente; porInactividad
    // marca el cierre automatico (CIERRE_INACTIVIDAD) frente al manual.
    Task<ResultadoCierreSesion> CerrarSesionAsync(bool porInactividad, CancellationToken ct);
}