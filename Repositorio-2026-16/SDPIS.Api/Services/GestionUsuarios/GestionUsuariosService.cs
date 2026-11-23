using Oracle.ManagedDataAccess.Client;
using SDPIS.Api.Models.DTOs;
using SDPIS.Api.Repositories;
using SDPIS.Api.Services.Autorizacion;
using SDPIS.Api.Services.Validation;

namespace SDPIS.Api.Services.GestionUsuarios;

// Parametros del historial administrativo tal como llegan del controlador.
public record ConsultaBitacora(
    long? AreaId, string? Categoria, DateTime? Desde, DateTime? Hasta,
    string? Texto, bool IncluirConsultas, int Pagina, int Tamano);

public interface IGestionUsuariosService
{
    IReadOnlyList<PermisoDelegableDto> ListarPermisosDelegables();
    Task<List<FuncionarioGestionDto>> ListarFuncionariosAsync(long? areaId, string? rol, CancellationToken ct);
    Task<List<DenunciaVisibleDto>> ListarDenunciasFuncionarioAsync(long funcionarioId, CancellationToken ct);
    Task<List<AreaGestionDto>> ListarAreasAsync(CancellationToken ct);
    Task<CrearFuncionarioResponse> CrearFuncionarioAsync(CrearFuncionarioRequest request, CancellationToken ct);
    Task ActualizarFuncionarioAsync(long funcionarioId, ActualizarFuncionarioRequest request, CancellationToken ct);
    Task GestionarPermisoAsync(long funcionarioId, GestionarPermisoRequest request, CancellationToken ct);
    Task GestionarAreaAdicionalAsync(long funcionarioId, GestionarAreaAdicionalRequest request, CancellationToken ct);
    Task RenunciarSuperusuarioAsync(CancellationToken ct);
    Task ConfigurarMaxJefesAsync(long areaId, ConfigurarMaxJefesRequest request, CancellationToken ct);
    Task<ResultadoPaginadoDto<BitacoraAdministrativaDto>> ListarBitacoraAsync(ConsultaBitacora consulta, CancellationToken ct);

    // TEMPORAL (eliminar al integrar MSI).
    Task CrearCredencialTemporalAsync(long funcionarioId, CrearCredencialTemporalRequest request, CancellationToken ct);
}

// HU-015: todas las reglas de gestion de roles y permisos.
//   * El actor siempre es el usuario del token (IUsuarioActual).
//   * Cada escritura + su registro en bitacora_administrativa van en la
//     misma transaccion (si una falla, se revierten ambas).
//   * Los cupos bloquean filas (FOR UPDATE) para no superarse con
//     solicitudes concurrentes.
public class GestionUsuariosService(
    IGestionUsuariosRepository repositorio,
    IPermisosService permisos,
    IUsuarioActual usuario,
    IConfiguration configuration) : IGestionUsuariosService
{
    private const string CategoriaFuncionarios = "FUNCIONARIOS";
    private const string CategoriaPermisos = "PERMISOS";
    private const string CategoriaCupos = "CUPOS";
    private const string CategoriaConsulta = "CONSULTA";

    // =====================================================
    // Consultas
    // =====================================================

    public IReadOnlyList<PermisoDelegableDto> ListarPermisosDelegables()
        => CatalogoPermisos.Delegables.Values
            .OrderBy(p => p.Grupo, StringComparer.Ordinal)
            .ThenBy(p => p.Codigo, StringComparer.Ordinal)
            .Select(p => new PermisoDelegableDto(p.Codigo, p.NivelDelegante, p.Grupo, p.Descripcion))
            .ToList();

    // Jefe (o delegado con VER_FUNCIONARIOS_AREA): solo su area.
    // Superusuario: cualquier area; rol=jefe_area da la "lista de jefes".
    public async Task<List<FuncionarioGestionDto>> ListarFuncionariosAsync(long? areaId, string? rol, CancellationToken ct)
    {
        var rolFiltro = string.IsNullOrWhiteSpace(rol) ? null : rol.Trim();
        GestionUsuariosValidator.ValidarRolFiltro(rolFiltro);

        var actor = await ActorAsync(ct);
        long? areaFiltro;
        if (actor.Tiene(Permisos.VerTodasAreas))
            areaFiltro = areaId;
        else if (actor.Tiene(Permisos.VerFuncionariosArea))
        {
            if (areaId.HasValue && areaId.Value != actor.AreaId)
                throw new PermisoDenegadoException("Solo puede consultar los funcionarios de su propia area.");
            areaFiltro = actor.AreaId;
        }
        else
            throw new PermisoDenegadoException("No cuenta con permisos para consultar funcionarios.");

        await repositorio.RegistrarConsultaDiariaAsync(new RegistroBitacoraAdministrativa(
            actor.FuncionarioId, null, areaFiltro, CategoriaConsulta,
            "Consulta de la lista de funcionarios" + (rolFiltro is null ? "." : $" (rol {rolFiltro}).")), ct);

        return await repositorio.ListarFuncionariosAsync(areaFiltro, rolFiltro, ct);
    }

    // validador -> sus denuncias ASIGNADAS; jefe -> denuncias POR ASIGNAR
    // de su area (un jefe nunca tiene denuncias asignadas).
    public async Task<List<DenunciaVisibleDto>> ListarDenunciasFuncionarioAsync(long funcionarioId, CancellationToken ct)
    {
        var actor = await ActorAsync(ct);
        var objetivo = await repositorio.ObtenerFuncionarioAsync(funcionarioId, ct)
            ?? throw new RecursoNoEncontradoException("El funcionario indicado no existe.");

        // El area del objetivo puede ser la base o una adicional (varias areas).
        var permitido =
            (objetivo.IdFuncionario == actor.FuncionarioId && actor.Tiene(Permisos.VerDashboard))
            || actor.Tiene(Permisos.VerTodasAreas)
            || (actor.Tiene(Permisos.VerFuncionariosArea)
                && (actor.AreaId == objetivo.AreaId
                    || await repositorio.FuncionarioPerteneceAAreaAsync(objetivo.IdFuncionario, actor.AreaId, ct)));
        if (!permitido)
            throw new PermisoDenegadoException("No cuenta con permisos para consultar las denuncias de este funcionario.");

        // Un funcionario con varias areas tiene denuncias de otras areas: quien
        // no es superusuario ni el propio funcionario solo ve las de su area.
        long? areaAcotada = actor.Tiene(Permisos.VerTodasAreas) || objetivo.IdFuncionario == actor.FuncionarioId
            ? null
            : actor.AreaId;
        var filtro = objetivo.Rol == Roles.JefeArea
            ? new FiltroDenuncias(objetivo.AreaId, null, "POR_ASIGNAR")
            : new FiltroDenuncias(areaAcotada, objetivo.IdFuncionario, "TODAS");
        return await repositorio.ListarDenunciasAsync(filtro, ct);
    }

    public async Task<List<AreaGestionDto>> ListarAreasAsync(CancellationToken ct)
    {
        var actor = await ActorAsync(ct);
        if (!actor.Tiene(Permisos.VerListaAreas))
            throw new PermisoDenegadoException("No cuenta con permisos para consultar la lista de areas.");

        await repositorio.RegistrarConsultaDiariaAsync(new RegistroBitacoraAdministrativa(
            actor.FuncionarioId, null, null, CategoriaConsulta, "Consulta de la lista de áreas."), ct);

        return await repositorio.ListarAreasAsync(ct);
    }

    // Superusuario: todas las areas (areaId opcional). Resto: su area.
    // Los eventos de sesion se traducen a texto entendible; el detalle
    // tecnico (IP / usuario digitado) solo lo recibe el superusuario.
    public async Task<ResultadoPaginadoDto<BitacoraAdministrativaDto>> ListarBitacoraAsync(
        ConsultaBitacora consulta, CancellationToken ct)
    {
        var categoriaFiltro = string.IsNullOrWhiteSpace(consulta.Categoria) ? null : consulta.Categoria.Trim().ToUpperInvariant();
        var texto = string.IsNullOrWhiteSpace(consulta.Texto) ? null : consulta.Texto.Trim();
        GestionUsuariosValidator.ValidarConsultaBitacora(categoriaFiltro, consulta.Pagina, consulta.Tamano,
            consulta.Desde, consulta.Hasta, texto);

        var actor = await ActorAsync(ct);
        if (!actor.Tiene(Permisos.VerBitacoraAdministrativa))
            throw new PermisoDenegadoException("No cuenta con permisos para consultar la bitacora administrativa.");

        var veTodo = actor.Tiene(Permisos.VerTodasAreas);
        var filtro = new FiltroBitacora(
            veTodo ? consulta.AreaId : actor.AreaId,
            categoriaFiltro,
            consulta.Desde?.Date,
            consulta.Hasta?.Date.AddDays(1), // "hasta" incluye todo ese dia
            texto,
            consulta.IncluirConsultas,
            consulta.Pagina,
            consulta.Tamano);

        var (eventos, total) = await repositorio.ListarBitacoraAsync(filtro, ct);
        var items = eventos.Select(e => AHistorial(e, veTodo)).ToList();
        return new ResultadoPaginadoDto<BitacoraAdministrativaDto>(items, total, consulta.Pagina, consulta.Tamano);
    }

    private static BitacoraAdministrativaDto AHistorial(EventoBitacoraRegistro e, bool verDetalleTecnico)
    {
        if (e.Origen != "SESION")
        {
            return new BitacoraAdministrativaDto(e.Origen, e.IdRegistro, e.Fecha, e.Categoria,
                e.RealizadoPorId, e.RealizadoPorNombre, e.FuncionarioAfectadoId, e.FuncionarioAfectadoNombre,
                e.AreaId, e.AreaNombre, e.Detalle, e.ValorAnterior, e.ValorNuevo);
        }

        var registrado = e.RealizadoPorNombre is not null;
        var (detalle, tipo) = e.ResultadoSesion switch
        {
            "EXITO" => ("Inició sesión en el sistema.", "INICIO_SESION"),
            "CIERRE_MANUAL" => ("Cerró sesión.", "CIERRE_SESION"),
            "CIERRE_INACTIVIDAD" => ("La sesión se cerró automáticamente por inactividad.", "CIERRE_SESION"),
            "FALLO" => (registrado
                    ? "Intento de inicio de sesión fallido: usuario o clave incorrectos."
                    : "Intento de inicio de sesión con un usuario no registrado.", "INTENTO_FALLIDO"),
            "DESHABILITADO" => ("Intento de inicio de sesión con una cuenta deshabilitada.", "INTENTO_FALLIDO"),
            "ROL_INVALIDO" => ("Intento de inicio de sesión rechazado: el rol del funcionario no es válido en el SDPIS.", "INTENTO_FALLIDO"),
            _ => ("Evento de sesión.", "SESION"),
        };

        string? tecnico = null;
        if (verDetalleTecnico)
        {
            var partes = new List<string>();
            if (!registrado && !string.IsNullOrWhiteSpace(e.CodigoUsuario))
                partes.Add($"Usuario digitado: {e.CodigoUsuario}");
            if (!string.IsNullOrWhiteSpace(e.DireccionIp))
                partes.Add(e.DireccionIp is "::1" or "127.0.0.1" or "::ffff:127.0.0.1"
                    ? "Equipo: el mismo servidor (acceso local)"
                    : $"Dirección IP: {e.DireccionIp}");
            tecnico = partes.Count == 0 ? null : string.Join(" · ", partes);
        }

        // En sesiones, "realizado por" y "afectado" son la misma persona: se
        // deja solo "realizado por" para no repetirla.
        return new BitacoraAdministrativaDto(e.Origen, e.IdRegistro, e.Fecha, e.Categoria,
            e.RealizadoPorId, e.RealizadoPorNombre, null, null,
            e.AreaId, e.AreaNombre, detalle, null, null, tipo, tecnico);
    }

    // =====================================================
    // Escrituras
    // =====================================================

    // Alta INTERNA (solo funcionario). Luego la valida el MSI por correo.
    //   jefe / delegado: validadores de su area, sin maximo.
    //   superusuario   : validadores, jefes (cupo del area) y superusuarios (max 2).
    public async Task<CrearFuncionarioResponse> CrearFuncionarioAsync(CrearFuncionarioRequest request, CancellationToken ct)
    {
        GestionUsuariosValidator.ValidarCrear(request);
        var actor = await ActorAsync(ct);

        var rol = request.Rol.Trim();
        var nombre = request.Nombre.Trim();
        var correo = request.Correo.Trim().ToLowerInvariant();
        var especialidad = Limpiar(request.Especialidad);

        if (!PuedeGestionar(actor, request.AreaId, rol))
            throw new PermisoDenegadoException("No cuenta con permisos para crear funcionarios con ese rol en esa area.");

        return await EnTransaccionAsync(async (c, t) =>
        {
            var area = await ValidarCupoAsync(c, t, rol, request.AreaId, excluirId: null, ct);

            if (await repositorio.ExisteCorreoAsync(c, t, correo, null, ct))
                throw new ReglaNegocioException("Ya existe un funcionario con ese correo institucional.");

            var id = await repositorio.InsertarFuncionarioAsync(c, t, new NuevoFuncionario(
                nombre, rol, correo, request.AreaId, especialidad,
                PermiteAsignar: rol != Roles.Validador), ct);

            await repositorio.RegistrarBitacoraAsync(c, t, new RegistroBitacoraAdministrativa(
                actor.FuncionarioId, id, request.AreaId, CategoriaFuncionarios,
                $"Alta del funcionario {nombre} en el área {area.Nombre}.",
                null, $"rol={rol}; correo={correo}; especialidad={especialidad ?? "-"}"), ct);

            return new CrearFuncionarioResponse(id);
        }, ct);
    }

    // Rol (validador <-> jefe_area), estado, especialidad y correccion de
    // nombre/correo (errores de digitacion del alta).
    // Un superusuario solo se modifica a si mismo (renuncia).
    //
    // BAJA (activo -> inactivo). No se borra al funcionario: conserva su
    // historial (bitacoras inmutables). La cuenta del MSI la deshabilita el
    // administrador del MSI (asociarCuenta, tipEstado = 2).
    //   * No puede tener denuncias asignadas activas (reasignar primero).
    //   * Se revocan todos sus permisos delegados y el de asignar; al
    //     reactivarlo no se devuelven.
    //   * No puede ser el ultimo jefe activo de su area.
    public async Task ActualizarFuncionarioAsync(long funcionarioId, ActualizarFuncionarioRequest request, CancellationToken ct)
    {
        GestionUsuariosValidator.ValidarActualizar(funcionarioId, request);
        var actor = await ActorAsync(ct);
        if (funcionarioId == actor.FuncionarioId)
            throw new ReglaNegocioException("No puede modificar su propio rol, estado ni permisos.");

        await EnTransaccionAsync(async (c, t) =>
        {
            var f = await repositorio.BloquearFuncionarioAsync(c, t, funcionarioId, ct)
                ?? throw new RecursoNoEncontradoException("El funcionario indicado no existe.");

            if (f.Rol == Roles.Superusuario)
                throw new ReglaNegocioException("Un superusuario solo puede modificarse a si mismo, renunciando a su rol.");

            var rolAnterior = f.Rol ?? Roles.Validador;
            var rolNuevo = request.Rol?.Trim() ?? rolAnterior;
            var activoNuevo = request.Activo ?? f.Activo;
            var especialidadNueva = request.Especialidad is null ? f.Especialidad : Limpiar(request.Especialidad);
            var nombreNuevo = request.Nombre?.Trim() ?? f.Nombre;
            var correoNuevo = request.Correo?.Trim().ToLowerInvariant() ?? f.Correo;

            if (!PuedeGestionar(actor, f.AreaId, rolAnterior) || !PuedeGestionar(actor, f.AreaId, rolNuevo))
                throw new PermisoDenegadoException("No cuenta con permisos para modificar este funcionario.");

            // Correccion del correo: solo antes del MSI (es la llave de enlace).
            var cambiaCorreo = !string.Equals(correoNuevo, f.Correo, StringComparison.Ordinal);
            if (cambiaCorreo)
            {
                if (MsiIntegrado())
                    throw new ReglaNegocioException("El correo institucional lo gestiona el MSI; no se puede corregir desde el SDPIS.");
                if (await repositorio.ExisteCorreoAsync(c, t, correoNuevo!, f.IdFuncionario, ct))
                    throw new ReglaNegocioException("Ya existe otro funcionario con ese correo institucional.");
            }

            if (rolNuevo == Roles.JefeArea && rolAnterior != Roles.JefeArea)
            {
                var asignadas = await repositorio.ContarDenunciasAsignadasActivasAsync(c, t, f.IdFuncionario, ct);
                if (asignadas > 0)
                    throw new ReglaNegocioException(
                        $"El funcionario tiene {asignadas} denuncias asignadas activas; un jefe de area no puede tener denuncias asignadas. Reasignelas primero.");
            }

            var esBaja = f.Activo && !activoNuevo;
            var esReactivacion = !f.Activo && activoNuevo;

            if (esBaja)
            {
                var asignadas = await repositorio.ContarDenunciasAsignadasActivasAsync(c, t, f.IdFuncionario, ct);
                if (asignadas > 0)
                    throw new ReglaNegocioException(
                        $"El funcionario tiene {asignadas} denuncias asignadas activas. Reasignelas antes de darlo de baja.");
            }

            // Ultimo jefe activo: el area quedaria sin quien asigne denuncias.
            // Se bloquea la fila del area para serializar bajas simultaneas.
            var dejaJefatura = f.Activo && rolAnterior == Roles.JefeArea
                && (!activoNuevo || rolNuevo != Roles.JefeArea);
            if (dejaJefatura)
            {
                var area = await repositorio.BloquearAreaAsync(c, t, f.AreaId, ct)
                    ?? throw new RecursoNoEncontradoException("El area del funcionario no existe.");
                // Aqui SI cuentan los jefes en renuncia: tambien pueden asignar.
                var otrosJefes = await repositorio.ContarJefesActivosAsync(c, t, f.AreaId, f.IdFuncionario,
                    incluirEnRenuncia: true, ct);
                if (otrosJefes == 0)
                    throw new ReglaNegocioException(
                        $"{f.Nombre} es el unico jefe activo de {area.Nombre}. Registre o promueva otro jefe antes de darlo de baja o cambiarle el rol.");
            }

            if (activoNuevo && (rolNuevo != rolAnterior || !f.Activo))
                await ValidarCupoAsync(c, t, rolNuevo, f.AreaId, f.IdFuncionario, ct);

            // El jefe conserva siempre el permiso de asignar; al dejar de
            // ser jefe lo pierde (se puede volver a delegar). Un validador
            // dado de baja lo pierde.
            var permiteAsignar = rolNuevo == Roles.JefeArea
                || (rolAnterior != Roles.JefeArea && f.PermiteAsignar && !esBaja);

            // Jefe "en renuncia" (ex superusuario): sale de esa condicion al
            // quitarle el rol de jefe o al darlo de baja.
            var delegadosActuales = await repositorio.ObtenerPermisosDelegadosAsync(c, t, f.IdFuncionario, ct);
            var estabaEnRenuncia = delegadosActuales.Contains(ReglasGestion.MarcaRenunciaSuperusuario);
            var saleDeRenuncia = estabaEnRenuncia && (esBaja || rolNuevo != Roles.JefeArea);

            var revocados = new List<string>();
            if (esBaja)
            {
                // Todo se borra (incluida la marca), pero en la bitacora solo
                // se listan permisos reales.
                revocados = delegadosActuales.Where(p => p != ReglasGestion.MarcaRenunciaSuperusuario).ToList();
                if (rolNuevo != Roles.JefeArea && f.PermiteAsignar)
                    revocados.Insert(0, Permisos.AsignarDenuncias);
                await repositorio.EliminarPermisosDelegadosAsync(c, t, f.IdFuncionario, delegadosActuales, ct);
            }
            else
            {
                // Delegaciones que el nuevo rol ya trae quedan sobrando.
                var sobrantes = CatalogoPermisos.PermisosDeRol(rolNuevo).ToList();
                if (saleDeRenuncia) sobrantes.Add(ReglasGestion.MarcaRenunciaSuperusuario);
                await repositorio.EliminarPermisosDelegadosAsync(c, t, f.IdFuncionario, sobrantes, ct);
            }

            // Las areas adicionales solo existen para validadores activos: al dejar
            // de serlo (baja o cambio de rol) se retiran. No hay denuncias asignadas
            // (ya se valido arriba), asi que no queda nada huerfano.
            if (esBaja || rolNuevo != Roles.Validador)
            {
                var adicionales = await repositorio.ListarAreasAdicionalesAsync(c, t, f.IdFuncionario, ct);
                if (adicionales.Count > 0)
                {
                    await repositorio.EliminarAreasAdicionalesAsync(c, t, f.IdFuncionario, ct);
                    await repositorio.RegistrarBitacoraAsync(c, t, new RegistroBitacoraAdministrativa(
                        actor.FuncionarioId, f.IdFuncionario, f.AreaId, CategoriaPermisos,
                        $"Se retiraron las áreas adicionales de {f.Nombre} porque {(esBaja ? "fue dado de baja" : "dejó de ser funcionario")}.",
                        DescribirAreasAdicionales(adicionales), DescribirAreasAdicionales([])), ct);
                }
            }

            await repositorio.ActualizarFuncionarioAsync(c, t, f.IdFuncionario, nombreNuevo, correoNuevo,
                rolNuevo, activoNuevo, especialidadNueva, permiteAsignar, ct);

            var cambiaNombre = !string.Equals(nombreNuevo, f.Nombre, StringComparison.Ordinal);
            var hayCambios = cambiaNombre || cambiaCorreo || rolNuevo != rolAnterior
                || activoNuevo != f.Activo || especialidadNueva != f.Especialidad;
            if (hayCambios)
            {
                var detalle = esBaja
                    ? $"Baja del funcionario {f.Nombre}{(estabaEnRenuncia ? " (jefe en renuncia, ex superusuario)" : "")}. Permisos revocados: {(revocados.Count == 0 ? "ninguno" : string.Join(", ", revocados))}."
                    : saleDeRenuncia
                        ? $"Retiro del rol de jefe al ex superusuario {nombreNuevo}: queda como funcionario."
                    : esReactivacion
                        ? $"Reactivación del funcionario {nombreNuevo} (sin permisos delegados)."
                        : (cambiaNombre || cambiaCorreo) && rolNuevo == rolAnterior && especialidadNueva == f.Especialidad
                            ? $"Corrección de datos del funcionario {nombreNuevo}."
                            : $"Modificación del funcionario {nombreNuevo}.";

                await repositorio.RegistrarBitacoraAsync(c, t, new RegistroBitacoraAdministrativa(
                    actor.FuncionarioId, f.IdFuncionario, f.AreaId, CategoriaFuncionarios, detalle,
                    DescribirFuncionario(f.Nombre, f.Correo, rolAnterior, f.Activo, f.Especialidad, cambiaNombre, cambiaCorreo),
                    DescribirFuncionario(nombreNuevo, correoNuevo, rolNuevo, activoNuevo, especialidadNueva, cambiaNombre, cambiaCorreo)), ct);
            }
            return true;
        }, ct);

        permisos.Invalidar(funcionarioId);
    }

    // Valores antes/despues para la bitacora; nombre y correo solo si cambiaron.
    private static string DescribirFuncionario(string nombre, string? correo, string rol, bool activo,
        string? especialidad, bool incluirNombre, bool incluirCorreo)
    {
        var partes = new List<string>();
        if (incluirNombre) partes.Add($"nombre={nombre}");
        if (incluirCorreo) partes.Add($"correo={correo ?? "-"}");
        partes.Add($"rol={rol}");
        partes.Add($"activo={SiNo(activo)}");
        partes.Add($"especialidad={especialidad ?? "-"}");
        return string.Join("; ", partes);
    }

    private bool MsiIntegrado()
        => string.Equals(configuration["Autenticacion:Proveedor"], "MSI", StringComparison.OrdinalIgnoreCase);

    // Otorgar / revocar un permiso ESPECIFICO (no el rol completo).
    public async Task GestionarPermisoAsync(long funcionarioId, GestionarPermisoRequest request, CancellationToken ct)
    {
        GestionUsuariosValidator.ValidarPermiso(funcionarioId, request);
        var codigo = request.Codigo.Trim();
        var otorgar = request.Accion.Trim().ToUpperInvariant() == "OTORGAR";
        var definicion = CatalogoPermisos.Delegables[codigo];

        var actor = await ActorAsync(ct);
        if (funcionarioId == actor.FuncionarioId)
            throw new ReglaNegocioException("No puede modificar su propio rol, estado ni permisos.");

        await EnTransaccionAsync(async (c, t) =>
        {
            var f = await repositorio.BloquearFuncionarioAsync(c, t, funcionarioId, ct)
                ?? throw new RecursoNoEncontradoException("El funcionario indicado no existe.");

            if (f.Rol == Roles.JefeArea && codigo == Permisos.AsignarDenuncias && !otorgar)
                throw new ReglaNegocioException("El jefe de area conserva el permiso de asignar de forma permanente e irrevocable.");
            if (f.Rol == Roles.Superusuario)
                throw new ReglaNegocioException("El superusuario ya cuenta con todos los permisos.");
            if (CatalogoPermisos.RolIncluye(f.Rol, codigo))
                throw new ReglaNegocioException("El funcionario ya tiene ese permiso por su rol; no se delega ni se revoca.");
            if (otorgar && !f.Activo)
                throw new ReglaNegocioException("No se pueden otorgar permisos a un funcionario inactivo.");

            // Nivel jefe_area: superusuario o jefe del area del destinatario.
            // Nivel superusuario: solo superusuario. Un delegado no re-delega.
            var autorizado = actor.Tiene(Permisos.AdministrarSistema)
                || (definicion.NivelDelegante == Roles.JefeArea
                    && actor.Tiene(Permisos.DelegarPermisosArea)
                    && actor.AreaId == f.AreaId);
            if (!autorizado)
                throw new PermisoDenegadoException("No cuenta con permisos para delegar ese permiso a este funcionario.");

            bool cambio;
            if (codigo == Permisos.AsignarDenuncias)
            {
                cambio = f.PermiteAsignar != otorgar;
                if (cambio)
                    await repositorio.ActualizarPermiteAsignarAsync(c, t, f.IdFuncionario, otorgar, ct);
            }
            else
            {
                cambio = otorgar
                    ? await repositorio.InsertarPermisoDelegadoAsync(c, t, f.IdFuncionario, codigo, actor.FuncionarioId, ct)
                    : await repositorio.EliminarPermisoDelegadoAsync(c, t, f.IdFuncionario, codigo, ct);
            }
            if (!cambio)
                throw new ReglaNegocioException("El funcionario ya se encuentra en ese estado para el permiso indicado.");

            await repositorio.RegistrarBitacoraAsync(c, t, new RegistroBitacoraAdministrativa(
                actor.FuncionarioId, f.IdFuncionario, f.AreaId, CategoriaPermisos,
                $"{(otorgar ? "Otorgado" : "Revocado")} el permiso {codigo} a {f.Nombre}.",
                $"{codigo}={(otorgar ? "NO" : "SI")}",
                $"{codigo}={(otorgar ? "SI" : "NO")}"), ct);
            return true;
        }, ct);

        permisos.Invalidar(funcionarioId);
    }

    // Otorgar / revocar un AREA ADICIONAL: permite que un funcionario (validador)
    // trabaje en varias areas a la vez. Solo un superusuario. El area base
    // (funcionario.area_id) no cambia; las adicionales se suman a ella.
    public async Task GestionarAreaAdicionalAsync(long funcionarioId, GestionarAreaAdicionalRequest request, CancellationToken ct)
    {
        GestionUsuariosValidator.ValidarAreaAdicional(funcionarioId, request);
        var otorgar = request.Accion.Trim().ToUpperInvariant() == "OTORGAR";

        var actor = await ActorAsync(ct);
        if (!actor.Tiene(Permisos.AdministrarSistema))
            throw new PermisoDenegadoException("Solo un superusuario puede otorgar o revocar áreas adicionales.");
        if (funcionarioId == actor.FuncionarioId)
            throw new ReglaNegocioException("No puede modificar su propio rol, estado ni permisos.");

        await EnTransaccionAsync(async (c, t) =>
        {
            var f = await repositorio.BloquearFuncionarioAsync(c, t, funcionarioId, ct)
                ?? throw new RecursoNoEncontradoException("El funcionario indicado no existe.");

            if (f.Rol != Roles.Validador)
                throw new ReglaNegocioException(
                    "Solo los funcionarios pueden trabajar en varias áreas; los jefes de área y superusuarios no.");
            if (otorgar && !f.Activo)
                throw new ReglaNegocioException("No se pueden otorgar áreas a un funcionario inactivo.");

            var area = await repositorio.BloquearAreaAsync(c, t, request.AreaId, ct)
                ?? throw new RecursoNoEncontradoException("El área indicada no existe.");
            if (area.IdArea == f.AreaId)
                throw new ReglaNegocioException($"{area.Nombre} ya es el área base del funcionario.");

            var antes = await repositorio.ListarAreasAdicionalesAsync(c, t, f.IdFuncionario, ct);

            if (otorgar)
            {
                if (!await repositorio.InsertarAreaAdicionalAsync(c, t, f.IdFuncionario, area.IdArea, actor.FuncionarioId, ct))
                    throw new ReglaNegocioException($"El funcionario ya trabaja en {area.Nombre}.");
            }
            else
            {
                // No se le quita un area donde todavia atiende denuncias.
                var asignadas = await repositorio.ContarDenunciasAsignadasActivasEnAreaAsync(c, t, f.IdFuncionario, area.IdArea, ct);
                if (asignadas > 0)
                    throw new ReglaNegocioException(
                        $"El funcionario tiene {asignadas} denuncias asignadas activas en {area.Nombre}. Reasígnelas antes de quitarle esa área.");
                if (!await repositorio.EliminarAreaAdicionalAsync(c, t, f.IdFuncionario, area.IdArea, ct))
                    throw new ReglaNegocioException($"El funcionario no tiene {area.Nombre} como área adicional.");
            }

            var despues = await repositorio.ListarAreasAdicionalesAsync(c, t, f.IdFuncionario, ct);
            await repositorio.RegistrarBitacoraAsync(c, t, new RegistroBitacoraAdministrativa(
                actor.FuncionarioId, f.IdFuncionario, area.IdArea, CategoriaPermisos,
                $"{(otorgar ? "Otorgada" : "Revocada")} el área adicional {area.Nombre} a {f.Nombre}.",
                DescribirAreasAdicionales(antes), DescribirAreasAdicionales(despues)), ct);
            return true;
        }, ct);

        permisos.Invalidar(funcionarioId);
    }

    private static string DescribirAreasAdicionales(IReadOnlyList<AreaAdicionalRegistro> areas)
        => "areas_adicionales=" + (areas.Count == 0 ? "-" : string.Join(", ", areas.Select(a => a.Nombre)));

    // El superusuario renuncia a su rol: queda como jefe en renuncia de su
    // area actual (no puede elegir otra) y libera su plaza. Debe quedar al
    // menos un superusuario.
    public async Task RenunciarSuperusuarioAsync(CancellationToken ct)
    {
        var actor = await ActorAsync(ct);
        if (!actor.EsRol(Roles.Superusuario))
            throw new ReglaNegocioException("Solo un superusuario puede renunciar a su rol.");

        await EnTransaccionAsync(async (c, t) =>
        {
            var f = await repositorio.BloquearFuncionarioAsync(c, t, actor.FuncionarioId, ct)
                ?? throw new RecursoNoEncontradoException("El funcionario indicado no existe.");

            var otros = await repositorio.ContarSuperusuariosActivosAsync(c, t, f.IdFuncionario, ct);
            if (otros == 0)
                throw new ReglaNegocioException("Debe existir al menos un superusuario activo; no puede renunciar siendo el unico.");

            var asignadas = await repositorio.ContarDenunciasAsignadasActivasAsync(c, t, f.IdFuncionario, ct);
            if (asignadas > 0)
                throw new ReglaNegocioException(
                    $"Tiene {asignadas} denuncias asignadas activas; un jefe de area no puede tener denuncias asignadas. Reasignelas primero.");

            // Queda como jefe "en renuncia" en su area actual (normalmente la
            // UVC). NO ocupa cupo de jefes; se marca para que el otro
            // superusuario pueda luego retirarle el rol o darlo de baja.
            var destino = await repositorio.BloquearAreaAsync(c, t, f.AreaId, ct)
                ?? throw new RecursoNoEncontradoException("El area indicada no existe.");
            if (destino.Tipo is null || !ReglasGestion.TiposAreaConJefe.Contains(destino.Tipo))
                throw new ReglaNegocioException("Esa area no admite jefe de area.");

            await repositorio.CambiarRolYAreaAsync(c, t, f.IdFuncionario, Roles.JefeArea, destino.IdArea,
                permiteAsignar: true, ct);
            await repositorio.EliminarPermisosDelegadosAsync(c, t, f.IdFuncionario,
                CatalogoPermisos.PermisosDeRol(Roles.JefeArea).ToList(), ct);
            await repositorio.InsertarPermisoDelegadoAsync(c, t, f.IdFuncionario,
                ReglasGestion.MarcaRenunciaSuperusuario, f.IdFuncionario, ct);

            await repositorio.RegistrarBitacoraAsync(c, t, new RegistroBitacoraAdministrativa(
                f.IdFuncionario, f.IdFuncionario, destino.IdArea, CategoriaFuncionarios,
                $"{f.Nombre} renunció al rol de superusuario. Queda como jefe de área en renuncia en {destino.Nombre} (no ocupa cupo de jefes). Plaza de superusuario liberada.",
                "rol=superusuario",
                "rol=jefe_area"), ct);
            return true;
        }, ct);

        permisos.Invalidar(actor.FuncionarioId);
    }

    // Cupo de jefes por area: lo decide el superusuario (sin tope fijo),
    // nunca por debajo de los jefes activos.
    public async Task ConfigurarMaxJefesAsync(long areaId, ConfigurarMaxJefesRequest request, CancellationToken ct)
    {
        GestionUsuariosValidator.ValidarMaxJefes(areaId, request);
        var actor = await ActorAsync(ct);
        if (!actor.Tiene(Permisos.AdministrarSistema))
            throw new PermisoDenegadoException("Solo un superusuario puede modificar el cupo de jefes.");

        await EnTransaccionAsync(async (c, t) =>
        {
            var area = await repositorio.BloquearAreaAsync(c, t, areaId, ct)
                ?? throw new RecursoNoEncontradoException("El area indicada no existe.");
            if (area.Tipo is null || !ReglasGestion.TiposAreaConJefe.Contains(area.Tipo))
                throw new ReglaNegocioException("Esa area no admite jefe de area.");

            var activos = await repositorio.ContarJefesActivosAsync(c, t, areaId, null, incluirEnRenuncia: false, ct);
            if (request.MaxJefes < activos)
                throw new ReglaNegocioException($"El cupo no puede ser menor a los jefes activos actuales ({activos}).");

            if (request.MaxJefes != area.MaxJefes)
            {
                await repositorio.ActualizarMaxJefesAsync(c, t, areaId, request.MaxJefes, ct);
                await repositorio.RegistrarBitacoraAsync(c, t, new RegistroBitacoraAdministrativa(
                    actor.FuncionarioId, null, areaId, CategoriaCupos,
                    $"Cupo de jefes del área {area.Nombre} actualizado.",
                    $"max_jefes={area.MaxJefes}", $"max_jefes={request.MaxJefes}"), ct);
            }
            return true;
        }, ct);
    }

    // TEMPORAL - ELIMINAR AL INTEGRAR EL MSI.
    // Unico punto que escribe en usuario_temporal desde HU-015: permite
    // iniciar sesion localmente con un funcionario recien creado.
    public async Task CrearCredencialTemporalAsync(long funcionarioId, CrearCredencialTemporalRequest request, CancellationToken ct)
    {
        if (MsiIntegrado())
            throw new ReglaNegocioException("Con el MSI integrado las credenciales las gestiona el MSI.");

        GestionUsuariosValidator.ValidarCredencialTemporal(funcionarioId, request);
        var actor = await ActorAsync(ct);

        await EnTransaccionAsync(async (c, t) =>
        {
            var f = await repositorio.BloquearFuncionarioAsync(c, t, funcionarioId, ct)
                ?? throw new RecursoNoEncontradoException("El funcionario indicado no existe.");
            if (!PuedeGestionar(actor, f.AreaId, f.Rol ?? Roles.Validador))
                throw new PermisoDenegadoException("No cuenta con permisos sobre este funcionario.");
            if (string.IsNullOrWhiteSpace(f.Correo))
                throw new ReglaNegocioException("El funcionario no tiene correo institucional registrado.");

            var hash = BCrypt.Net.BCrypt.HashPassword(request.Clave, workFactor: 12);
            var creada = await repositorio.InsertarCredencialTemporalAsync(c, t, new NuevaCredencialTemporal(
                f.IdFuncionario, request.Identificacion.Trim(), request.Nombre.Trim(), request.Apellido1.Trim(),
                Limpiar(request.Apellido2), f.Correo, f.Rol ?? Roles.Validador, hash), ct);
            if (!creada)
                throw new ReglaNegocioException("Ya existe una credencial temporal con esa identificacion o correo.");

            await repositorio.RegistrarBitacoraAsync(c, t, new RegistroBitacoraAdministrativa(
                actor.FuncionarioId, f.IdFuncionario, f.AreaId, CategoriaFuncionarios,
                $"Credencial local temporal creada para {f.Nombre} (provisional hasta integrar el MSI)."), ct);
            return true;
        }, ct);
    }

    // =====================================================
    // Reglas compartidas
    // =====================================================

    private async Task<ContextoPermisos> ActorAsync(CancellationToken ct)
    {
        var actor = await permisos.ObtenerContextoAsync(usuario.FuncionarioId, ct);
        if (actor is null || !actor.Activo)
            throw new PermisoDenegadoException("El usuario que realiza la accion no existe o esta inactivo.");
        return actor;
    }

    // ADMINISTRAR_SISTEMA: validadores y jefes de cualquier area (y crear
    // superusuarios). CREAR_FUNCIONARIOS_AREA: solo validadores de su area.
    private static bool PuedeGestionar(ContextoPermisos actor, long areaObjetivo, string rolObjetivo)
    {
        if (actor.Tiene(Permisos.AdministrarSistema))
            return true;
        return rolObjetivo == Roles.Validador
            && actor.Tiene(Permisos.CrearFuncionariosArea)
            && actor.AreaId == areaObjetivo;
    }

    // Bloquea el area (y las filas de superusuarios si aplica) y valida:
    //   superusuario: maximo 2 activos (fijo).
    //   jefe_area   : maximo area.max_jefes activos, solo en AR/ARS.
    private async Task<AreaRegistro> ValidarCupoAsync(OracleConnection c, OracleTransaction t,
        string rol, long areaId, long? excluirId, CancellationToken ct)
    {
        var area = await repositorio.BloquearAreaAsync(c, t, areaId, ct)
            ?? throw new RecursoNoEncontradoException("El area indicada no existe.");

        if (rol == Roles.Superusuario)
        {
            var activos = await repositorio.ContarSuperusuariosActivosAsync(c, t, excluirId, ct);
            if (activos >= ReglasGestion.MaxSuperusuarios)
                throw new ReglaNegocioException(
                    $"Ya existen {ReglasGestion.MaxSuperusuarios} superusuarios activos; no se permite otro.");
        }
        else if (rol == Roles.JefeArea)
        {
            if (area.Tipo is null || !ReglasGestion.TiposAreaConJefe.Contains(area.Tipo))
                throw new ReglaNegocioException("Esa area no admite jefe de area.");

            var activos = await repositorio.ContarJefesActivosAsync(c, t, areaId, excluirId, incluirEnRenuncia: false, ct);
            if (activos >= area.MaxJefes)
                throw new ReglaNegocioException(
                    $"El area ya tiene {area.MaxJefes} jefes activos (cupo maximo). El superusuario puede ampliarlo.");
        }

        return area;
    }

    private async Task<T> EnTransaccionAsync<T>(
        Func<OracleConnection, OracleTransaction, Task<T>> accion, CancellationToken ct)
    {
        await using var conexion = await repositorio.AbrirConexionAsync(ct);
        await using var transaccion = conexion.BeginTransaction();
        try
        {
            var resultado = await accion(conexion, transaccion);
            await transaccion.CommitAsync(ct);
            return resultado;
        }
        catch
        {
            await transaccion.RollbackAsync(ct);
            throw;
        }
    }

    private static string? Limpiar(string? texto)
        => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

    private static string SiNo(bool valor) => valor ? "S" : "N";
}
