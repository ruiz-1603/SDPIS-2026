using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SDPIS.Api.Models.DTOs;
using SDPIS.Api.Services.Autorizacion;
using SDPIS.Api.Services.GestionUsuarios;

namespace SDPIS.Api.Controllers;

// HU-015: gestion de roles y permisos. [RequierePermiso] es la primera
// barrera; las reglas finas (area, cupos, auto-modificacion) las aplica
// GestionUsuariosService. El actor siempre sale del token.
[ApiController]
[Route("api/gestion-usuarios")]
[Authorize]
public class GestionUsuariosController(
    IGestionUsuariosService servicio,
    IPermisosService permisos,
    IUsuarioActual usuario) : ControllerBase
{
    // Permisos efectivos del usuario autenticado (refrescar menu).
    [HttpGet("mis-permisos")]
    public async Task<ActionResult<PermisosSesionDto>> MisPermisos(CancellationToken ct)
        => Ok(await permisos.ObtenerSesionAsync(usuario.FuncionarioId, ct));

    // Catalogo de permisos delegables (para la pantalla de permisos).
    [HttpGet("permisos-delegables")]
    [RequierePermiso(Permisos.DelegarPermisosArea)]
    public ActionResult<IReadOnlyList<PermisoDelegableDto>> PermisosDelegables()
        => Ok(servicio.ListarPermisosDelegables());

    // ?areaId= (solo superusuario) &rol=jefe_area (lista de jefes)
    [HttpGet("funcionarios")]
    [RequierePermiso(Permisos.VerFuncionariosArea)]
    public async Task<ActionResult<List<FuncionarioGestionDto>>> ListarFuncionarios(
        [FromQuery] long? areaId, [FromQuery] string? rol, CancellationToken ct)
        => Ok(await servicio.ListarFuncionariosAsync(areaId, rol, ct));

    [HttpPost("funcionarios")]
    [RequierePermiso(Permisos.CrearFuncionariosArea)]
    public async Task<ActionResult<CrearFuncionarioResponse>> CrearFuncionario(
        [FromBody] CrearFuncionarioRequest request, CancellationToken ct)
        => Ok(await servicio.CrearFuncionarioAsync(request, ct));

    [HttpPut("funcionarios/{id:long}")]
    [RequierePermiso(Permisos.CrearFuncionariosArea)]
    public async Task<IActionResult> ActualizarFuncionario(
        long id, [FromBody] ActualizarFuncionarioRequest request, CancellationToken ct)
    {
        await servicio.ActualizarFuncionarioAsync(id, request, ct);
        return NoContent();
    }

    // Validador: sus denuncias asignadas. Jefe: denuncias por asignar de su area.
    [HttpGet("funcionarios/{id:long}/denuncias")]
    public async Task<ActionResult<List<DenunciaVisibleDto>>> DenunciasFuncionario(long id, CancellationToken ct)
        => Ok(await servicio.ListarDenunciasFuncionarioAsync(id, ct));

    // Body: { codigo, accion: OTORGAR | REVOCAR }
    [HttpPost("funcionarios/{id:long}/permisos")]
    [RequierePermiso(Permisos.DelegarPermisosArea)]
    public async Task<IActionResult> GestionarPermiso(
        long id, [FromBody] GestionarPermisoRequest request, CancellationToken ct)
    {
        await servicio.GestionarPermisoAsync(id, request, ct);
        return NoContent();
    }

    // Areas adicionales de un funcionario: solo el superusuario.
    // Body: { areaId, accion: OTORGAR | REVOCAR }
    [HttpPost("funcionarios/{id:long}/areas-adicionales")]
    [RequierePermiso(Permisos.AdministrarSistema)]
    public async Task<IActionResult> GestionarAreaAdicional(
        long id, [FromBody] GestionarAreaAdicionalRequest request, CancellationToken ct)
    {
        await servicio.GestionarAreaAdicionalAsync(id, request, ct);
        return NoContent();
    }

    // TEMPORAL - ELIMINAR AL INTEGRAR EL MSI.
    [HttpPost("funcionarios/{id:long}/credencial-temporal")]
    [RequierePermiso(Permisos.CrearFuncionariosArea)]
    public async Task<IActionResult> CrearCredencialTemporal(
        long id, [FromBody] CrearCredencialTemporalRequest request, CancellationToken ct)
    {
        await servicio.CrearCredencialTemporalAsync(id, request, ct);
        return NoContent();
    }

    // El superusuario autenticado renuncia y queda como jefe en renuncia de
    // su area actual (no puede elegir otra).
    [HttpPost("superusuario/renuncia")]
    [RequierePermiso(Permisos.AdministrarSistema)]
    public async Task<IActionResult> RenunciarSuperusuario(CancellationToken ct)
    {
        await servicio.RenunciarSuperusuarioAsync(ct);
        return NoContent();
    }

    [HttpGet("areas")]
    [RequierePermiso(Permisos.VerListaAreas)]
    public async Task<ActionResult<List<AreaGestionDto>>> ListarAreas(CancellationToken ct)
        => Ok(await servicio.ListarAreasAsync(ct));

    [HttpPut("areas/{id:long}/max-jefes")]
    [RequierePermiso(Permisos.AdministrarSistema)]
    public async Task<IActionResult> ConfigurarMaxJefes(
        long id, [FromBody] ConfigurarMaxJefesRequest request, CancellationToken ct)
    {
        await servicio.ConfigurarMaxJefesAsync(id, request, ct);
        return NoContent();
    }

    // Bitacora administrativa (incluye inicios/cierres de sesion).
    // ?areaId= (solo superusuario) &categoria= &desde=AAAA-MM-DD &hasta=AAAA-MM-DD
    // &texto= (funcionario o detalle) &incluirConsultas=false &pagina=1 &tamano=20
    [HttpGet("bitacora")]
    [RequierePermiso(Permisos.VerBitacoraAdministrativa)]
    public async Task<ActionResult<ResultadoPaginadoDto<BitacoraAdministrativaDto>>> ListarBitacora(
        [FromQuery] long? areaId, [FromQuery] string? categoria,
        [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta, [FromQuery] string? texto,
        [FromQuery] bool incluirConsultas = false,
        [FromQuery] int pagina = 1, [FromQuery] int tamano = 20, CancellationToken ct = default)
        => Ok(await servicio.ListarBitacoraAsync(
            new ConsultaBitacora(areaId, categoria, desde, hasta, texto, incluirConsultas, pagina, tamano), ct));
}

// Listado unico de denuncias segun el rol (bitacora, criterios, encargado,
// asignaciones y dashboard deben consumirlo o usar el servicio).
[ApiController]
[Route("api/denuncias-visibles")]
[Authorize]
public class DenunciasVisiblesController(IVisibilidadDenunciasService visibilidad) : ControllerBase
{
    // ?areaId= (solo alcance TODAS) &filtro=TODAS|POR_ASIGNAR|ASIGNADAS
    // &criterios=REGISTRADOS|PENDIENTES (opcional)
    [HttpGet]
    public async Task<ActionResult<List<DenunciaVisibleDto>>> Listar(
        [FromQuery] long? areaId, [FromQuery] string? filtro, [FromQuery] string? criterios, CancellationToken ct)
        => Ok(await visibilidad.ListarAsync(areaId, filtro, ct, criterios));
}
