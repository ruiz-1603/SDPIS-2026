namespace SDPIS.Api.Models.DTOs;

// ---------- Sesion / permisos ----------

// Permisos efectivos y alcance del usuario autenticado (menu del frontend).
public record PermisosSesionDto(IReadOnlyList<string> Permisos, string Alcance);

public record PermisoDelegableDto(string Codigo, string NivelDelegante, string Grupo, string Descripcion);

// ---------- Funcionarios ----------

// DenunciasAsignadas solo aplica a validadores; DenunciasPorAsignar solo a
// jefes de area (un jefe nunca tiene denuncias asignadas). En superusuarios
// ambos son null.
public record FuncionarioGestionDto(
    long IdFuncionario,
    string Nombre,
    string? Rol,
    string? Correo,
    bool Activo,
    string? Especialidad,
    bool PermiteAsignar,
    long AreaId,
    string AreaNombre,
    IReadOnlyList<string> PermisosDelegados,
    int? DenunciasAsignadas,
    int? DenunciasPorAsignar,
    // Superusuario que renuncio: jefe "en renuncia" (no ocupa cupo).
    // null = no aplica; con valor = fecha de la renuncia.
    DateTime? RenunciaSuperusuarioFecha = null,
    // Areas ADICIONALES (ademas de AreaId, la base). Solo las otorga un
    // superusuario y solo aplican a validadores.
    IReadOnlyList<AreaAdicionalDto>? AreasAdicionales = null);

public record AreaAdicionalDto(long IdArea, string Nombre);

// Body: { areaId, accion: OTORGAR | REVOCAR }
public record GestionarAreaAdicionalRequest(long AreaId, string Accion);

public record CrearFuncionarioRequest(
    string Nombre,
    string Correo,
    string Rol,
    long AreaId,
    string? Especialidad);

public record CrearFuncionarioResponse(long IdFuncionario);

// null = sin cambio. Especialidad "" = quitarla.
// Nombre / Correo: correccion de errores de digitacion del alta. El correo
// solo se corrige mientras el MSI no este integrado (es la llave de enlace).
public record ActualizarFuncionarioRequest(
    string? Rol,
    bool? Activo,
    string? Especialidad,
    string? Nombre = null,
    string? Correo = null);

// Accion: OTORGAR | REVOCAR
public record GestionarPermisoRequest(string Codigo, string Accion);

// TEMPORAL (eliminar al integrar MSI).
public record CrearCredencialTemporalRequest(
    string Identificacion,
    string Nombre,
    string Apellido1,
    string? Apellido2,
    string Clave);

// ---------- Areas ----------

public record AreaGestionDto(
    long IdArea,
    string Nombre,
    string? Tipo,
    string CodigoArea,
    string? RegionNombre,
    int MaxJefes,
    int JefesActivos,            // solo los que ocupan cupo
    int JefesEnRenuncia,         // ex superusuarios (no ocupan cupo)
    int FuncionariosActivos,
    int DenunciasPorAsignar,
    int DenunciasActivas);

public record ConfigurarMaxJefesRequest(int MaxJefes);

// ---------- Denuncias visibles ----------

public record DenunciaVisibleDto(
    long IdDenuncia,
    string? Consecutivo,
    string Estado,
    DateTime FechaCreacion,
    long AreaActualId,
    string AreaNombre,
    long? FuncionarioAsignadoId,
    string? FuncionarioAsignadoNombre);

// ---------- Bitacora administrativa ----------

// Origen: ADMIN (bitacora_administrativa) | SESION (bitacora_sesion, HU-001/028).
// TipoEvento (solo SESION): INICIO_SESION | CIERRE_SESION | INTENTO_FALLIDO.
// DetalleTecnico: equipo/IP o usuario digitado; solo se envia al superusuario.
public record BitacoraAdministrativaDto(
    string Origen,
    long IdRegistro,
    DateTime Fecha,
    string Categoria,
    long? RealizadoPorId,
    string? RealizadoPorNombre,
    long? FuncionarioAfectadoId,
    string? FuncionarioAfectadoNombre,
    long? AreaId,
    string? AreaNombre,
    string Detalle,
    string? ValorAnterior,
    string? ValorNuevo,
    string? TipoEvento = null,
    string? DetalleTecnico = null);

public record ResultadoPaginadoDto<T>(IReadOnlyList<T> Items, int Total, int Pagina, int Tamano);
