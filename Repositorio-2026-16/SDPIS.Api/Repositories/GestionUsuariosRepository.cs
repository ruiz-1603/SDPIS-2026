using System.Data;
using System.Data.Common;
using System.Globalization;
using Oracle.ManagedDataAccess.Client;
using SDPIS.Api.Models.DTOs;

namespace SDPIS.Api.Repositories;

// ---------- Registros internos (no salen al cliente) ----------
public record DenunciaAlcanceRegistro(long IdDenuncia, long AreaActualId, long? FuncionarioAsignadoId);
public record FuncionarioRegistro(
    long IdFuncionario, string Nombre, string? Rol, string? Correo, bool Activo,
    long AreaId, string? Especialidad, bool PermiteAsignar);

public record AreaRegistro(long IdArea, string Nombre, string? Tipo, int MaxJefes);

// Area adicional de un funcionario (funcionario_area_adicional).
public record AreaAdicionalRegistro(long IdArea, string Nombre);

public record NuevoFuncionario(
    string Nombre, string Rol, string Correo, long AreaId, string? Especialidad, bool PermiteAsignar);

public record RegistroBitacoraAdministrativa(
    long? RealizadoPorId, long? FuncionarioAfectadoId, long? AreaId,
    string Categoria, string Detalle, string? ValorAnterior = null, string? ValorNuevo = null);

// Filtro: TODAS | POR_ASIGNAR | ASIGNADAS
// Criterios (opcional): REGISTRADOS (con evaluacion de criterios tecnicos) |
// PENDIENTES (sin evaluacion) | null (todas).
public record FiltroDenuncias(long? AreaId, long? FuncionarioAsignadoId, string Filtro, string? Criterios = null);

// Filtros del historial administrativo. HastaExclusivo: el servicio ya le
// suma un dia a la fecha "hasta" que elige el usuario.
public record FiltroBitacora(
    long? AreaId, string? Categoria, DateTime? Desde, DateTime? HastaExclusivo,
    string? Texto, bool IncluirConsultas, int Pagina, int Tamano);

// Evento crudo del historial (bitacora_administrativa o bitacora_sesion).
// El servicio lo convierte en un texto entendible para el usuario.
public record EventoBitacoraRegistro(
    string Origen, long IdRegistro, DateTime Fecha, string Categoria,
    long? RealizadoPorId, string? RealizadoPorNombre,
    long? FuncionarioAfectadoId, string? FuncionarioAfectadoNombre,
    long? AreaId, string? AreaNombre,
    string Detalle, string? ValorAnterior, string? ValorNuevo,
    string? ResultadoSesion, string? CodigoUsuario, string? DireccionIp);

// TEMPORAL (eliminar al integrar MSI).
public record NuevaCredencialTemporal(
    long FuncionarioId, string Identificacion, string Nombre, string Apellido1,
    string? Apellido2, string Correo, string Rol, string ClaveHash);

// HU-015: acceso a datos de gestion de usuarios. Solo SQL de lectura y
// escritura; TODAS las reglas de negocio viven en GestionUsuariosService.
// Los metodos que reciben (connection, transaction) participan de la
// transaccion del servicio; los demas abren su propia conexion.
public interface IGestionUsuariosRepository
{
    Task<OracleConnection> AbrirConexionAsync(CancellationToken ct);

    // ----- Lecturas sin transaccion -----
    Task<FuncionarioRegistro?> ObtenerFuncionarioAsync(long id, CancellationToken ct);
    Task<List<FuncionarioGestionDto>> ListarFuncionariosAsync(long? areaId, string? rol, CancellationToken ct);
    // true si el area es la base del funcionario o una de sus areas adicionales.
    Task<bool> FuncionarioPerteneceAAreaAsync(long funcionarioId, long areaId, CancellationToken ct);
    Task<List<AreaGestionDto>> ListarAreasAsync(CancellationToken ct);
    Task<List<DenunciaVisibleDto>> ListarDenunciasAsync(FiltroDenuncias filtro, CancellationToken ct);
    Task<DenunciaAlcanceRegistro?> ObtenerAlcanceDenunciaAsync(long denunciaId, CancellationToken ct);
    Task<(List<EventoBitacoraRegistro> Items, int Total)> ListarBitacoraAsync(FiltroBitacora filtro, CancellationToken ct);
    Task RegistrarBitacoraAsync(RegistroBitacoraAdministrativa registro, CancellationToken ct);
    // Consultas a listas: como maximo un registro por usuario, lista y dia
    // (evita que el historial crezca con cada apertura de pantalla).
    Task RegistrarConsultaDiariaAsync(RegistroBitacoraAdministrativa registro, CancellationToken ct);

    // ----- Dentro de la transaccion del servicio -----
    Task<FuncionarioRegistro?> BloquearFuncionarioAsync(OracleConnection c, OracleTransaction t, long id, CancellationToken ct);
    Task<AreaRegistro?> BloquearAreaAsync(OracleConnection c, OracleTransaction t, long areaId, CancellationToken ct);
    Task<int> ContarSuperusuariosActivosAsync(OracleConnection c, OracleTransaction t, long? excluirId, CancellationToken ct);
    // incluirEnRenuncia = false para cupos (los ex superusuarios no ocupan
    // cupo); true para "ultimo jefe activo" (si pueden asignar denuncias).
    Task<int> ContarJefesActivosAsync(OracleConnection c, OracleTransaction t, long areaId, long? excluirId,
        bool incluirEnRenuncia, CancellationToken ct);
    // excluirId: al corregir el correo de un funcionario, no compararlo consigo mismo.
    Task<bool> ExisteCorreoAsync(OracleConnection c, OracleTransaction t, string correo, long? excluirId, CancellationToken ct);
    Task<int> ContarDenunciasAsignadasActivasAsync(OracleConnection c, OracleTransaction t, long funcionarioId, CancellationToken ct);
    Task<long> InsertarFuncionarioAsync(OracleConnection c, OracleTransaction t, NuevoFuncionario nuevo, CancellationToken ct);
    Task ActualizarFuncionarioAsync(OracleConnection c, OracleTransaction t, long id, string nombre, string? correo,
        string rol, bool activo, string? especialidad, bool permiteAsignar, CancellationToken ct);
    Task<List<string>> ObtenerPermisosDelegadosAsync(OracleConnection c, OracleTransaction t, long id, CancellationToken ct);
    Task CambiarRolYAreaAsync(OracleConnection c, OracleTransaction t, long id, string rol, long areaId,
        bool permiteAsignar, CancellationToken ct);
    Task ActualizarPermiteAsignarAsync(OracleConnection c, OracleTransaction t, long id, bool permite, CancellationToken ct);
    Task<bool> InsertarPermisoDelegadoAsync(OracleConnection c, OracleTransaction t, long id, string codigo,
        long otorgadoPor, CancellationToken ct);
    Task<bool> EliminarPermisoDelegadoAsync(OracleConnection c, OracleTransaction t, long id, string codigo, CancellationToken ct);
    Task EliminarPermisosDelegadosAsync(OracleConnection c, OracleTransaction t, long id,
        IReadOnlyCollection<string> codigos, CancellationToken ct);
    Task ActualizarMaxJefesAsync(OracleConnection c, OracleTransaction t, long areaId, int maxJefes, CancellationToken ct);

    // ----- Areas adicionales del funcionario (solo las otorga un superusuario) -----
    Task<List<AreaAdicionalRegistro>> ListarAreasAdicionalesAsync(OracleConnection c, OracleTransaction t, long funcionarioId, CancellationToken ct);
    // false si ya la tenia (llave primaria duplicada).
    Task<bool> InsertarAreaAdicionalAsync(OracleConnection c, OracleTransaction t, long funcionarioId, long areaId,
        long otorgadoPor, CancellationToken ct);
    Task<bool> EliminarAreaAdicionalAsync(OracleConnection c, OracleTransaction t, long funcionarioId, long areaId, CancellationToken ct);
    Task EliminarAreasAdicionalesAsync(OracleConnection c, OracleTransaction t, long funcionarioId, CancellationToken ct);
    // Denuncias no finalizadas del funcionario DENTRO de un area concreta.
    Task<int> ContarDenunciasAsignadasActivasEnAreaAsync(OracleConnection c, OracleTransaction t, long funcionarioId,
        long areaId, CancellationToken ct);
    Task RegistrarBitacoraAsync(OracleConnection c, OracleTransaction t, RegistroBitacoraAdministrativa registro, CancellationToken ct);

    // TEMPORAL (eliminar al integrar MSI). Devuelve false si la
    // identificacion o el correo ya tienen credencial.
    Task<bool> InsertarCredencialTemporalAsync(OracleConnection c, OracleTransaction t,
        NuevaCredencialTemporal credencial, CancellationToken ct);
}

public class GestionUsuariosRepository(IConfiguration configuration) : IGestionUsuariosRepository
{
    private static readonly CultureInfo Cultura = CultureInfo.InvariantCulture;
    private const int OraUnicidad = 1; // ORA-00001

    private readonly string _connectionString = configuration.GetConnectionString("SDPIS")
        ?? throw new InvalidOperationException("No se encontro la cadena de conexion 'SDPIS'.");

    public async Task<OracleConnection> AbrirConexionAsync(CancellationToken ct)
    {
        var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync(ct);
        return connection;
    }

    // =====================================================
    // Lecturas
    // =====================================================

    public async Task<FuncionarioRegistro?> ObtenerFuncionarioAsync(long id, CancellationToken ct)
    {
        await using var connection = await AbrirConexionAsync(ct);
        return await LeerFuncionarioAsync(connection, null, id, bloquear: false, ct);
    }

    public async Task<List<FuncionarioGestionDto>> ListarFuncionariosAsync(long? areaId, string? rol, CancellationToken ct)
    {
        await using var connection = await AbrirConexionAsync(ct);
        await using var command = Comando(connection, null, """
            SELECT f.id_funcionario, f.nombre, f.rol, f.correo,
                   NVL(f.activo, 'S') AS activo, f.especialidad,
                   NVL(f.permite_asignar, 'N') AS permite_asignar,
                   f.area_id, a.nombre AS area_nombre,
                   (SELECT LISTAGG(fp.codigo_permiso, ',') WITHIN GROUP (ORDER BY fp.codigo_permiso)
                      FROM funcionario_permiso fp
                     WHERE fp.funcionario_id = f.id_funcionario
                       AND fp.codigo_permiso <> 'RENUNCIA_SUPERUSUARIO') AS permisos_delegados,
                   (SELECT MAX(fp.fecha_otorgamiento)
                      FROM funcionario_permiso fp
                     WHERE fp.funcionario_id = f.id_funcionario
                       AND fp.codigo_permiso = 'RENUNCIA_SUPERUSUARIO') AS renuncia_fecha,
                   (SELECT LISTAGG(fa.area_id || '|' || aa.nombre, ';') WITHIN GROUP (ORDER BY aa.nombre)
                      FROM funcionario_area_adicional fa
                      JOIN area aa ON aa.id_area = fa.area_id
                     WHERE fa.funcionario_id = f.id_funcionario) AS areas_adicionales,
                   CASE WHEN f.rol = 'validador' THEN
                     (SELECT COUNT(*) FROM denuncia d
                       WHERE d.funcionario_asignado_id = f.id_funcionario
                         AND d.estado <> 'FINALIZADA'
                         AND (:p_area_id IS NULL OR d.area_actual_id = :p_area_id))
                   END AS denuncias_asignadas,
                   CASE WHEN f.rol = 'jefe_area' THEN
                     (SELECT COUNT(*) FROM denuncia d
                       WHERE d.area_actual_id = f.area_id
                         AND d.funcionario_asignado_id IS NULL
                         AND d.estado <> 'FINALIZADA')
                   END AS denuncias_por_asignar
              FROM funcionario f
              JOIN area a ON a.id_area = f.area_id
             WHERE (:p_area_id IS NULL OR f.area_id = :p_area_id
                    OR EXISTS (SELECT 1 FROM funcionario_area_adicional fx
                                WHERE fx.funcionario_id = f.id_funcionario
                                  AND fx.area_id = :p_area_id))
               AND (:p_rol IS NULL OR f.rol = :p_rol)
             ORDER BY a.nombre,
                      CASE f.rol WHEN 'superusuario' THEN 1 WHEN 'jefe_area' THEN 2 ELSE 3 END,
                      f.nombre
            """);
        command.Parameters.Add(Numero("p_area_id", areaId));
        command.Parameters.Add(Texto("p_rol", rol));

        var lista = new List<FuncionarioGestionDto>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var delegados = TextoNulo(reader, "permisos_delegados");
            lista.Add(new FuncionarioGestionDto(
                Largo(reader, "id_funcionario"),
                reader.GetString(reader.GetOrdinal("nombre")),
                TextoNulo(reader, "rol"),
                TextoNulo(reader, "correo"),
                reader.GetString(reader.GetOrdinal("activo")) == "S",
                TextoNulo(reader, "especialidad"),
                reader.GetString(reader.GetOrdinal("permite_asignar")) == "S",
                Largo(reader, "area_id"),
                reader.GetString(reader.GetOrdinal("area_nombre")),
                delegados is null ? Array.Empty<string>() : delegados.Split(',', StringSplitOptions.RemoveEmptyEntries),
                EnteroNulo(reader, "denuncias_asignadas"),
                EnteroNulo(reader, "denuncias_por_asignar"),
                reader.IsDBNull(reader.GetOrdinal("renuncia_fecha"))
                    ? null
                    : reader.GetDateTime(reader.GetOrdinal("renuncia_fecha")),
                LeerAreasAdicionales(TextoNulo(reader, "areas_adicionales"))));
        }
        return lista;
    }

    // "12|A.R.S Abangares;15|A.R.S Acosta" -> lista de areas.
    private static List<AreaAdicionalDto> LeerAreasAdicionales(string? texto)
    {
        var lista = new List<AreaAdicionalDto>();
        if (string.IsNullOrWhiteSpace(texto)) return lista;
        foreach (var par in texto.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var corte = par.IndexOf('|');
            if (corte > 0 && long.TryParse(par[..corte], NumberStyles.Integer, Cultura, out var id))
                lista.Add(new AreaAdicionalDto(id, par[(corte + 1)..]));
        }
        return lista;
    }

    public async Task<bool> FuncionarioPerteneceAAreaAsync(long funcionarioId, long areaId, CancellationToken ct)
    {
        await using var connection = await AbrirConexionAsync(ct);
        return await ContarAsync(connection, null, """
            SELECT COUNT(*) FROM funcionario f
             WHERE f.id_funcionario = :p_id
               AND (f.area_id = :p_area_id
                    OR EXISTS (SELECT 1 FROM funcionario_area_adicional fa
                                WHERE fa.funcionario_id = f.id_funcionario
                                  AND fa.area_id = :p_area_id))
            """, ct, Numero("p_id", funcionarioId), Numero("p_area_id", areaId)) > 0;
    }

    public async Task<List<AreaGestionDto>> ListarAreasAsync(CancellationToken ct)
    {
        await using var connection = await AbrirConexionAsync(ct);
        await using var command = Comando(connection, null, """
            SELECT a.id_area, a.nombre, a.tipo, a.codigo_area,
                   r.nombre AS region_nombre, a.max_jefes,
                   (SELECT COUNT(*) FROM funcionario f
                     WHERE f.area_id = a.id_area AND f.rol = 'jefe_area' AND f.activo = 'S'
                       AND NOT EXISTS (SELECT 1 FROM funcionario_permiso fp
                                        WHERE fp.funcionario_id = f.id_funcionario
                                          AND fp.codigo_permiso = 'RENUNCIA_SUPERUSUARIO')) AS jefes_activos,
                   (SELECT COUNT(*) FROM funcionario f
                     WHERE f.area_id = a.id_area AND f.rol = 'jefe_area' AND f.activo = 'S'
                       AND EXISTS (SELECT 1 FROM funcionario_permiso fp
                                    WHERE fp.funcionario_id = f.id_funcionario
                                      AND fp.codigo_permiso = 'RENUNCIA_SUPERUSUARIO')) AS jefes_en_renuncia,
                   (SELECT COUNT(*) FROM funcionario f
                     WHERE f.area_id = a.id_area AND f.activo = 'S')
                   + (SELECT COUNT(*) FROM funcionario_area_adicional fa
                        JOIN funcionario f ON f.id_funcionario = fa.funcionario_id
                       WHERE fa.area_id = a.id_area AND f.activo = 'S') AS funcionarios_activos,
                   (SELECT COUNT(*) FROM denuncia d
                     WHERE d.area_actual_id = a.id_area
                       AND d.funcionario_asignado_id IS NULL
                       AND d.estado <> 'FINALIZADA') AS denuncias_por_asignar,
                   (SELECT COUNT(*) FROM denuncia d
                     WHERE d.area_actual_id = a.id_area AND d.estado <> 'FINALIZADA') AS denuncias_activas
              FROM area a
              LEFT JOIN region r ON r.id_region = a.region_id
             ORDER BY a.nombre
            """);

        var lista = new List<AreaGestionDto>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            lista.Add(new AreaGestionDto(
                Largo(reader, "id_area"),
                reader.GetString(reader.GetOrdinal("nombre")),
                TextoNulo(reader, "tipo"),
                reader.GetString(reader.GetOrdinal("codigo_area")),
                TextoNulo(reader, "region_nombre"),
                Entero(reader, "max_jefes"),
                Entero(reader, "jefes_activos"),
                Entero(reader, "jefes_en_renuncia"),
                Entero(reader, "funcionarios_activos"),
                Entero(reader, "denuncias_por_asignar"),
                Entero(reader, "denuncias_activas")));
        }
        return lista;
    }

    public async Task<List<DenunciaVisibleDto>> ListarDenunciasAsync(FiltroDenuncias filtro, CancellationToken ct)
    {
        await using var connection = await AbrirConexionAsync(ct);
        await using var command = Comando(connection, null, """
            SELECT d.id_denuncia, d.consecutivo, d.estado, d.fecha_creacion,
                   d.area_actual_id, a.nombre AS area_nombre,
                   d.funcionario_asignado_id, f.nombre AS funcionario_asignado_nombre
              FROM denuncia d
              JOIN area a             ON a.id_area = d.area_actual_id
              LEFT JOIN funcionario f ON f.id_funcionario = d.funcionario_asignado_id
             WHERE (:p_area_id IS NULL OR d.area_actual_id = :p_area_id)
               AND (:p_funcionario_id IS NULL OR d.funcionario_asignado_id = :p_funcionario_id)
               AND (   :p_filtro = 'TODAS'
                    OR (:p_filtro = 'POR_ASIGNAR' AND d.funcionario_asignado_id IS NULL AND d.estado <> 'FINALIZADA')
                    OR (:p_filtro = 'ASIGNADAS'   AND d.funcionario_asignado_id IS NOT NULL))
             ORDER BY d.fecha_creacion DESC, d.id_denuncia DESC
            """);
        command.Parameters.Add(Numero("p_area_id", filtro.AreaId));
        command.Parameters.Add(Numero("p_funcionario_id", filtro.FuncionarioAsignadoId));
        command.Parameters.Add(Texto("p_filtro", filtro.Filtro));

        var lista = new List<DenunciaVisibleDto>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            lista.Add(new DenunciaVisibleDto(
                Largo(reader, "id_denuncia"),
                TextoNulo(reader, "consecutivo"),
                reader.GetString(reader.GetOrdinal("estado")),
                reader.GetDateTime(reader.GetOrdinal("fecha_creacion")),
                Largo(reader, "area_actual_id"),
                reader.GetString(reader.GetOrdinal("area_nombre")),
                LargoNulo(reader, "funcionario_asignado_id"),
                TextoNulo(reader, "funcionario_asignado_nombre")));
        }
        return lista;
    }
    public async Task<DenunciaAlcanceRegistro?> ObtenerAlcanceDenunciaAsync(long denunciaId, CancellationToken ct)
    {
        await using var connection = await AbrirConexionAsync(ct);
        await using var command = Comando(connection, null, """
            SELECT id_denuncia, area_actual_id, funcionario_asignado_id
              FROM denuncia
             WHERE id_denuncia = :p_id
            """);
        command.Parameters.Add(Numero("p_id", denunciaId));

        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
            return null;

        return new DenunciaAlcanceRegistro(
            Largo(reader, "id_denuncia"),
            Largo(reader, "area_actual_id"),
            LargoNulo(reader, "funcionario_asignado_id"));
    }

    // Eventos administrativos + sesiones (bitacora_sesion de HU-001/028),
    // filtrados en una sola CTE para que el conteo y la pagina coincidan.
    // PROVISIONAL: el bloque SESION llega al funcionario a traves de
    // usuario_temporal (solo lectura). Al integrar el MSI, ajustar ese bloque.
    // Las consultas (CONSULTA) se excluyen salvo que se pidan: son muchas y
    // tapan los cambios reales.
    private const string HistorialFiltradoSql = """
        WITH eventos AS (
          SELECT 'ADMIN' AS origen, b.id_bitacora_admin AS id_registro, b.fecha, b.categoria,
                 b.realizado_por_id, b.funcionario_afectado_id, b.area_id,
                 b.detalle, b.valor_anterior, b.valor_nuevo,
                 CAST(NULL AS VARCHAR2(20))  AS resultado_sesion,
                 CAST(NULL AS VARCHAR2(150)) AS codigo_usuario,
                 CAST(NULL AS VARCHAR2(45))  AS direccion_ip
            FROM bitacora_administrativa b
          UNION ALL
          SELECT 'SESION', s.id_bitacora_sesion, CAST(s.fecha_hora AS TIMESTAMP), 'SESION',
                 u.funcionario_id, u.funcionario_id, f.area_id,
                 s.resultado, CAST(NULL AS VARCHAR2(500)), CAST(NULL AS VARCHAR2(500)),
                 s.resultado, s.codigo_usuario, s.direccion_ip
            FROM bitacora_sesion s
            LEFT JOIN usuario_temporal u ON u.id_usuario_temporal = s.id_usuario_temporal
            LEFT JOIN funcionario f      ON f.id_funcionario = u.funcionario_id
        ),
        filtrados AS (
          SELECT e.origen, e.id_registro, e.fecha, e.categoria,
                 e.realizado_por_id, fr.nombre AS realizado_por_nombre,
                 e.funcionario_afectado_id, fa.nombre AS funcionario_afectado_nombre,
                 e.area_id, a.nombre AS area_nombre,
                 e.detalle, e.valor_anterior, e.valor_nuevo,
                 e.resultado_sesion, e.codigo_usuario, e.direccion_ip
            FROM eventos e
            LEFT JOIN funcionario fr ON fr.id_funcionario = e.realizado_por_id
            LEFT JOIN funcionario fa ON fa.id_funcionario = e.funcionario_afectado_id
            LEFT JOIN area a         ON a.id_area = e.area_id
           WHERE (:p_area_id IS NULL OR e.area_id = :p_area_id)
             AND (:p_categoria IS NULL OR e.categoria = :p_categoria)
             AND (:p_categoria IS NOT NULL OR :p_incluir_consultas = 1 OR e.categoria <> 'CONSULTA')
             AND (:p_desde IS NULL OR e.fecha >= :p_desde)
             AND (:p_hasta IS NULL OR e.fecha <  :p_hasta)
             AND (:p_texto IS NULL
                  OR LOWER(e.detalle)         LIKE :p_texto ESCAPE '\'
                  OR LOWER(fr.nombre)         LIKE :p_texto ESCAPE '\'
                  OR LOWER(fa.nombre)         LIKE :p_texto ESCAPE '\'
                  OR LOWER(e.codigo_usuario)  LIKE :p_texto ESCAPE '\')
        )
        """;

    public async Task<(List<EventoBitacoraRegistro> Items, int Total)> ListarBitacoraAsync(
        FiltroBitacora filtro, CancellationToken ct)
    {
        await using var connection = await AbrirConexionAsync(ct);

        int total;
        await using (var count = Comando(connection, null, HistorialFiltradoSql + "SELECT COUNT(*) FROM filtrados"))
        {
            AgregarFiltrosBitacora(count, filtro);
            total = Convert.ToInt32(await count.ExecuteScalarAsync(ct), Cultura);
        }

        await using var command = Comando(connection, null, HistorialFiltradoSql + """
            SELECT * FROM filtrados
             ORDER BY fecha DESC, id_registro DESC
             OFFSET :p_offset ROWS FETCH NEXT :p_tamano ROWS ONLY
            """);
        AgregarFiltrosBitacora(command, filtro);
        command.Parameters.Add(Numero("p_offset", (long)(filtro.Pagina - 1) * filtro.Tamano));
        command.Parameters.Add(Numero("p_tamano", filtro.Tamano));

        var lista = new List<EventoBitacoraRegistro>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            lista.Add(new EventoBitacoraRegistro(
                reader.GetString(reader.GetOrdinal("origen")).Trim(),
                Largo(reader, "id_registro"),
                reader.GetDateTime(reader.GetOrdinal("fecha")),
                reader.GetString(reader.GetOrdinal("categoria")).Trim(),
                LargoNulo(reader, "realizado_por_id"),
                TextoNulo(reader, "realizado_por_nombre"),
                LargoNulo(reader, "funcionario_afectado_id"),
                TextoNulo(reader, "funcionario_afectado_nombre"),
                LargoNulo(reader, "area_id"),
                TextoNulo(reader, "area_nombre"),
                reader.GetString(reader.GetOrdinal("detalle")),
                TextoNulo(reader, "valor_anterior"),
                TextoNulo(reader, "valor_nuevo"),
                TextoNulo(reader, "resultado_sesion"),
                TextoNulo(reader, "codigo_usuario"),
                TextoNulo(reader, "direccion_ip")));
        }
        return (lista, total);
    }

    private static void AgregarFiltrosBitacora(OracleCommand command, FiltroBitacora f)
    {
        command.Parameters.Add(Numero("p_area_id", f.AreaId));
        command.Parameters.Add(Texto("p_categoria", f.Categoria));
        command.Parameters.Add(Numero("p_incluir_consultas", f.IncluirConsultas ? 1 : 0));
        command.Parameters.Add(Fecha("p_desde", f.Desde));
        command.Parameters.Add(Fecha("p_hasta", f.HastaExclusivo));
        command.Parameters.Add(Texto("p_texto", PatronBusqueda(f.Texto)));
    }

    // "%texto%" en minusculas, escapando los comodines de LIKE.
    private static string? PatronBusqueda(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return null;
        var escapado = texto.Trim().ToLowerInvariant()
            .Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
        return $"%{escapado}%";
    }

    public async Task RegistrarBitacoraAsync(RegistroBitacoraAdministrativa registro, CancellationToken ct)
    {
        await using var connection = await AbrirConexionAsync(ct);
        await InsertarBitacoraAsync(connection, null, registro, ct);
    }

    public async Task RegistrarConsultaDiariaAsync(RegistroBitacoraAdministrativa r, CancellationToken ct)
    {
        await using var connection = await AbrirConexionAsync(ct);
        await using var command = Comando(connection, null, """
            INSERT INTO bitacora_administrativa
                   (realizado_por_id, funcionario_afectado_id, area_id, categoria, detalle)
            SELECT :p_realizado_por, :p_afectado, :p_area_id, :p_categoria, :p_detalle
              FROM dual
             WHERE NOT EXISTS (
                   SELECT 1 FROM bitacora_administrativa b
                    WHERE b.realizado_por_id = :p_realizado_por
                      AND b.categoria = :p_categoria
                      AND b.detalle = :p_detalle
                      AND NVL(b.area_id, -1) = NVL(:p_area_id, -1)
                      AND b.fecha >= TRUNC(SYSTIMESTAMP))
            """);
        command.Parameters.Add(Numero("p_realizado_por", r.RealizadoPorId));
        command.Parameters.Add(Numero("p_afectado", r.FuncionarioAfectadoId));
        command.Parameters.Add(Numero("p_area_id", r.AreaId));
        command.Parameters.Add(Texto("p_categoria", r.Categoria));
        command.Parameters.Add(Texto("p_detalle", Recortar(r.Detalle, 1000)));
        await command.ExecuteNonQueryAsync(ct);
    }

    // =====================================================
    // Dentro de transaccion
    // =====================================================

    public Task<FuncionarioRegistro?> BloquearFuncionarioAsync(OracleConnection c, OracleTransaction t, long id, CancellationToken ct)
        => LeerFuncionarioAsync(c, t, id, bloquear: true, ct);

    public async Task<AreaRegistro?> BloquearAreaAsync(OracleConnection c, OracleTransaction t, long areaId, CancellationToken ct)
    {
        await using var command = Comando(c, t, """
            SELECT id_area, nombre, tipo, max_jefes
              FROM area
             WHERE id_area = :p_area_id
               FOR UPDATE
            """);
        command.Parameters.Add(Numero("p_area_id", areaId));

        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
            return null;

        return new AreaRegistro(
            Largo(reader, "id_area"),
            reader.GetString(reader.GetOrdinal("nombre")),
            TextoNulo(reader, "tipo"),
            Entero(reader, "max_jefes"));
    }

    // Bloquea las filas de los superusuarios activos: dos altas o
    // promociones concurrentes quedan serializadas y no superan el cupo.
    public async Task<int> ContarSuperusuariosActivosAsync(OracleConnection c, OracleTransaction t, long? excluirId, CancellationToken ct)
    {
        await using var command = Comando(c, t, """
            SELECT id_funcionario
              FROM funcionario
             WHERE rol = 'superusuario' AND activo = 'S'
               AND id_funcionario <> NVL(:p_excluir, -1)
               FOR UPDATE
            """);
        command.Parameters.Add(Numero("p_excluir", excluirId));

        var cantidad = 0;
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            cantidad++;
        return cantidad;
    }

    public Task<int> ContarJefesActivosAsync(OracleConnection c, OracleTransaction t, long areaId, long? excluirId,
        bool incluirEnRenuncia, CancellationToken ct)
        => ContarAsync(c, t, """
            SELECT COUNT(*) FROM funcionario f
             WHERE f.rol = 'jefe_area' AND f.activo = 'S' AND f.area_id = :p_area_id
               AND f.id_funcionario <> NVL(:p_excluir, -1)
               AND (:p_incluir_renuncia = 1
                    OR NOT EXISTS (SELECT 1 FROM funcionario_permiso fp
                                    WHERE fp.funcionario_id = f.id_funcionario
                                      AND fp.codigo_permiso = 'RENUNCIA_SUPERUSUARIO'))
            """, ct, Numero("p_area_id", areaId), Numero("p_excluir", excluirId),
            Numero("p_incluir_renuncia", incluirEnRenuncia ? 1 : 0));

    public async Task<bool> ExisteCorreoAsync(OracleConnection c, OracleTransaction t, string correo, long? excluirId, CancellationToken ct)
        => await ContarAsync(c, t, """
            SELECT COUNT(*) FROM funcionario
             WHERE LOWER(correo) = :p_correo
               AND id_funcionario <> NVL(:p_excluir, -1)
            """, ct, Texto("p_correo", correo.ToLowerInvariant()), Numero("p_excluir", excluirId)) > 0;

    public Task<int> ContarDenunciasAsignadasActivasAsync(OracleConnection c, OracleTransaction t, long funcionarioId, CancellationToken ct)
        => ContarAsync(c, t, """
            SELECT COUNT(*) FROM denuncia
             WHERE funcionario_asignado_id = :p_id AND estado <> 'FINALIZADA'
            """, ct, Numero("p_id", funcionarioId));

    public async Task<long> InsertarFuncionarioAsync(OracleConnection c, OracleTransaction t, NuevoFuncionario nuevo, CancellationToken ct)
    {
        await using var command = Comando(c, t, """
            INSERT INTO funcionario (nombre, rol, correo, area_id, activo, especialidad, permite_asignar)
            VALUES (:p_nombre, :p_rol, :p_correo, :p_area_id, 'S', :p_especialidad, :p_permite)
            RETURNING id_funcionario INTO :p_id
            """);
        command.Parameters.Add(Texto("p_nombre", nuevo.Nombre));
        command.Parameters.Add(Texto("p_rol", nuevo.Rol));
        command.Parameters.Add(Texto("p_correo", nuevo.Correo));
        command.Parameters.Add(Numero("p_area_id", nuevo.AreaId));
        command.Parameters.Add(Texto("p_especialidad", nuevo.Especialidad));
        command.Parameters.Add(Texto("p_permite", SiNo(nuevo.PermiteAsignar)));
        var pId = new OracleParameter("p_id", OracleDbType.Decimal, ParameterDirection.Output);
        command.Parameters.Add(pId);

        await command.ExecuteNonQueryAsync(ct);
        return long.Parse(pId.Value!.ToString()!, Cultura);
    }

    public async Task ActualizarFuncionarioAsync(OracleConnection c, OracleTransaction t, long id, string nombre, string? correo,
        string rol, bool activo, string? especialidad, bool permiteAsignar, CancellationToken ct)
    {
        await using var command = Comando(c, t, """
            UPDATE funcionario
               SET nombre = :p_nombre, correo = :p_correo, rol = :p_rol, activo = :p_activo,
                   especialidad = :p_especialidad, permite_asignar = :p_permite
             WHERE id_funcionario = :p_id
            """);
        command.Parameters.Add(Texto("p_nombre", nombre));
        command.Parameters.Add(Texto("p_correo", correo));
        command.Parameters.Add(Texto("p_rol", rol));
        command.Parameters.Add(Texto("p_activo", SiNo(activo)));
        command.Parameters.Add(Texto("p_especialidad", especialidad));
        command.Parameters.Add(Texto("p_permite", SiNo(permiteAsignar)));
        command.Parameters.Add(Numero("p_id", id));
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task CambiarRolYAreaAsync(OracleConnection c, OracleTransaction t, long id, string rol, long areaId,
        bool permiteAsignar, CancellationToken ct)
    {
        await using var command = Comando(c, t, """
            UPDATE funcionario
               SET rol = :p_rol, area_id = :p_area_id, permite_asignar = :p_permite
             WHERE id_funcionario = :p_id
            """);
        command.Parameters.Add(Texto("p_rol", rol));
        command.Parameters.Add(Numero("p_area_id", areaId));
        command.Parameters.Add(Texto("p_permite", SiNo(permiteAsignar)));
        command.Parameters.Add(Numero("p_id", id));
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task ActualizarPermiteAsignarAsync(OracleConnection c, OracleTransaction t, long id, bool permite, CancellationToken ct)
    {
        await using var command = Comando(c, t,
            "UPDATE funcionario SET permite_asignar = :p_permite WHERE id_funcionario = :p_id");
        command.Parameters.Add(Texto("p_permite", SiNo(permite)));
        command.Parameters.Add(Numero("p_id", id));
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<bool> InsertarPermisoDelegadoAsync(OracleConnection c, OracleTransaction t, long id, string codigo,
        long otorgadoPor, CancellationToken ct)
    {
        await using var command = Comando(c, t, """
            INSERT INTO funcionario_permiso (funcionario_id, codigo_permiso, otorgado_por_id)
            VALUES (:p_id, :p_codigo, :p_otorgado_por)
            """);
        command.Parameters.Add(Numero("p_id", id));
        command.Parameters.Add(Texto("p_codigo", codigo));
        command.Parameters.Add(Numero("p_otorgado_por", otorgadoPor));
        try
        {
            await command.ExecuteNonQueryAsync(ct);
            return true;
        }
        catch (OracleException ex) when (ex.Number == OraUnicidad)
        {
            return false;
        }
    }

    public async Task<bool> EliminarPermisoDelegadoAsync(OracleConnection c, OracleTransaction t, long id, string codigo, CancellationToken ct)
    {
        await using var command = Comando(c, t,
            "DELETE FROM funcionario_permiso WHERE funcionario_id = :p_id AND codigo_permiso = :p_codigo");
        command.Parameters.Add(Numero("p_id", id));
        command.Parameters.Add(Texto("p_codigo", codigo));
        return await command.ExecuteNonQueryAsync(ct) > 0;
    }

    public async Task EliminarPermisosDelegadosAsync(OracleConnection c, OracleTransaction t, long id,
        IReadOnlyCollection<string> codigos, CancellationToken ct)
    {
        if (codigos.Count == 0) return;

        var nombres = codigos.Select((_, i) => $":p_c{i}").ToList();
        await using var command = Comando(c, t,
            $"DELETE FROM funcionario_permiso WHERE funcionario_id = :p_id AND codigo_permiso IN ({string.Join(", ", nombres)})");
        command.Parameters.Add(Numero("p_id", id));
        var i = 0;
        foreach (var codigo in codigos)
            command.Parameters.Add(Texto($"p_c{i++}", codigo));
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<List<string>> ObtenerPermisosDelegadosAsync(OracleConnection c, OracleTransaction t, long id, CancellationToken ct)
    {
        await using var command = Comando(c, t, """
            SELECT codigo_permiso FROM funcionario_permiso
             WHERE funcionario_id = :p_id
             ORDER BY codigo_permiso
            """);
        command.Parameters.Add(Numero("p_id", id));

        var codigos = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            codigos.Add(reader.GetString(0));
        return codigos;
    }

    public async Task<List<AreaAdicionalRegistro>> ListarAreasAdicionalesAsync(OracleConnection c, OracleTransaction t,
        long funcionarioId, CancellationToken ct)
    {
        await using var command = Comando(c, t, """
            SELECT fa.area_id, a.nombre
              FROM funcionario_area_adicional fa
              JOIN area a ON a.id_area = fa.area_id
             WHERE fa.funcionario_id = :p_id
             ORDER BY a.nombre
            """);
        command.Parameters.Add(Numero("p_id", funcionarioId));

        var lista = new List<AreaAdicionalRegistro>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            lista.Add(new AreaAdicionalRegistro(Largo(reader, "area_id"), reader.GetString(reader.GetOrdinal("nombre"))));
        return lista;
    }

    public async Task<bool> InsertarAreaAdicionalAsync(OracleConnection c, OracleTransaction t, long funcionarioId,
        long areaId, long otorgadoPor, CancellationToken ct)
    {
        await using var command = Comando(c, t, """
            INSERT INTO funcionario_area_adicional (funcionario_id, area_id, otorgado_por_id)
            VALUES (:p_id, :p_area_id, :p_otorgado_por)
            """);
        command.Parameters.Add(Numero("p_id", funcionarioId));
        command.Parameters.Add(Numero("p_area_id", areaId));
        command.Parameters.Add(Numero("p_otorgado_por", otorgadoPor));
        try
        {
            await command.ExecuteNonQueryAsync(ct);
            return true;
        }
        catch (OracleException ex) when (ex.Number == OraUnicidad)
        {
            return false;
        }
    }

    public async Task<bool> EliminarAreaAdicionalAsync(OracleConnection c, OracleTransaction t, long funcionarioId,
        long areaId, CancellationToken ct)
    {
        await using var command = Comando(c, t,
            "DELETE FROM funcionario_area_adicional WHERE funcionario_id = :p_id AND area_id = :p_area_id");
        command.Parameters.Add(Numero("p_id", funcionarioId));
        command.Parameters.Add(Numero("p_area_id", areaId));
        return await command.ExecuteNonQueryAsync(ct) > 0;
    }

    public async Task EliminarAreasAdicionalesAsync(OracleConnection c, OracleTransaction t, long funcionarioId, CancellationToken ct)
    {
        await using var command = Comando(c, t, "DELETE FROM funcionario_area_adicional WHERE funcionario_id = :p_id");
        command.Parameters.Add(Numero("p_id", funcionarioId));
        await command.ExecuteNonQueryAsync(ct);
    }

    public Task<int> ContarDenunciasAsignadasActivasEnAreaAsync(OracleConnection c, OracleTransaction t,
        long funcionarioId, long areaId, CancellationToken ct)
        => ContarAsync(c, t, """
            SELECT COUNT(*) FROM denuncia
             WHERE funcionario_asignado_id = :p_id AND area_actual_id = :p_area_id AND estado <> 'FINALIZADA'
            """, ct, Numero("p_id", funcionarioId), Numero("p_area_id", areaId));

    public async Task ActualizarMaxJefesAsync(OracleConnection c, OracleTransaction t, long areaId, int maxJefes, CancellationToken ct)
    {
        await using var command = Comando(c, t, "UPDATE area SET max_jefes = :p_max WHERE id_area = :p_area_id");
        command.Parameters.Add(Numero("p_max", maxJefes));
        command.Parameters.Add(Numero("p_area_id", areaId));
        await command.ExecuteNonQueryAsync(ct);
    }

    public Task RegistrarBitacoraAsync(OracleConnection c, OracleTransaction t, RegistroBitacoraAdministrativa registro, CancellationToken ct)
        => InsertarBitacoraAsync(c, t, registro, ct);

    public async Task<bool> InsertarCredencialTemporalAsync(OracleConnection c, OracleTransaction t,
        NuevaCredencialTemporal credencial, CancellationToken ct)
    {
        await using var command = Comando(c, t, """
            INSERT INTO usuario_temporal (funcionario_id, identificacion, nombre, apellido1, apellido2,
                                          correo_institucional, rol, clave_hash, activo)
            VALUES (:p_funcionario_id, :p_identificacion, :p_nombre, :p_apellido1, :p_apellido2,
                    :p_correo, :p_rol, :p_clave_hash, 'S')
            """);
        command.Parameters.Add(Numero("p_funcionario_id", credencial.FuncionarioId));
        command.Parameters.Add(Texto("p_identificacion", credencial.Identificacion));
        command.Parameters.Add(Texto("p_nombre", credencial.Nombre));
        command.Parameters.Add(Texto("p_apellido1", credencial.Apellido1));
        command.Parameters.Add(Texto("p_apellido2", credencial.Apellido2));
        command.Parameters.Add(Texto("p_correo", credencial.Correo.ToLowerInvariant()));
        command.Parameters.Add(Texto("p_rol", credencial.Rol));
        command.Parameters.Add(Texto("p_clave_hash", credencial.ClaveHash));
        try
        {
            await command.ExecuteNonQueryAsync(ct);
            return true;
        }
        catch (OracleException ex) when (ex.Number == OraUnicidad)
        {
            return false;
        }
    }

    // =====================================================
    // Auxiliares
    // =====================================================

    private static async Task<FuncionarioRegistro?> LeerFuncionarioAsync(OracleConnection c, OracleTransaction? t,
        long id, bool bloquear, CancellationToken ct)
    {
        await using var command = Comando(c, t, $"""
            SELECT id_funcionario, nombre, rol, correo, NVL(activo, 'S') AS activo,
                   area_id, especialidad, NVL(permite_asignar, 'N') AS permite_asignar
              FROM funcionario
             WHERE id_funcionario = :p_id
             {(bloquear ? "FOR UPDATE" : "")}
            """);
        command.Parameters.Add(Numero("p_id", id));

        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
            return null;

        return new FuncionarioRegistro(
            Largo(reader, "id_funcionario"),
            reader.GetString(reader.GetOrdinal("nombre")),
            TextoNulo(reader, "rol"),
            TextoNulo(reader, "correo"),
            reader.GetString(reader.GetOrdinal("activo")) == "S",
            Largo(reader, "area_id"),
            TextoNulo(reader, "especialidad"),
            reader.GetString(reader.GetOrdinal("permite_asignar")) == "S");
    }

    private static async Task InsertarBitacoraAsync(OracleConnection c, OracleTransaction? t,
        RegistroBitacoraAdministrativa r, CancellationToken ct)
    {
        await using var command = Comando(c, t, """
            INSERT INTO bitacora_administrativa
                   (realizado_por_id, funcionario_afectado_id, area_id, categoria,
                    detalle, valor_anterior, valor_nuevo)
            VALUES (:p_realizado_por, :p_afectado, :p_area_id, :p_categoria,
                    :p_detalle, :p_anterior, :p_nuevo)
            """);
        command.Parameters.Add(Numero("p_realizado_por", r.RealizadoPorId));
        command.Parameters.Add(Numero("p_afectado", r.FuncionarioAfectadoId));
        command.Parameters.Add(Numero("p_area_id", r.AreaId));
        command.Parameters.Add(Texto("p_categoria", r.Categoria));
        command.Parameters.Add(Texto("p_detalle", Recortar(r.Detalle, 1000)));
        command.Parameters.Add(Texto("p_anterior", Recortar(r.ValorAnterior, 500)));
        command.Parameters.Add(Texto("p_nuevo", Recortar(r.ValorNuevo, 500)));
        await command.ExecuteNonQueryAsync(ct);
    }

    private static async Task<int> ContarAsync(OracleConnection c, OracleTransaction? t, string sql,
        CancellationToken ct, params OracleParameter[] parametros)
    {
        await using var command = Comando(c, t, sql);
        command.Parameters.AddRange(parametros);
        return Convert.ToInt32(await command.ExecuteScalarAsync(ct), Cultura);
    }

    private static OracleCommand Comando(OracleConnection c, OracleTransaction? t, string sql)
    {
        var command = c.CreateCommand();
        command.Transaction = t;
        command.CommandType = CommandType.Text;
        command.BindByName = true;
        command.CommandText = sql;
        return command;
    }

    private static OracleParameter Numero(string nombre, long? valor)
        => new(nombre, OracleDbType.Decimal, valor.HasValue ? (object)valor.Value : DBNull.Value, ParameterDirection.Input);

    private static OracleParameter Fecha(string nombre, DateTime? valor)
        => new(nombre, OracleDbType.TimeStamp, valor.HasValue ? (object)valor.Value : DBNull.Value, ParameterDirection.Input);

    private static OracleParameter Texto(string nombre, string? valor)
        => new(nombre, OracleDbType.Varchar2, (object?)valor ?? DBNull.Value, ParameterDirection.Input);

    private static string SiNo(bool valor) => valor ? "S" : "N";

    private static string? Recortar(string? texto, int maximo)
        => texto is null || texto.Length <= maximo ? texto : texto[..maximo];

    private static long Largo(DbDataReader r, string col)
        => Convert.ToInt64(r.GetValue(r.GetOrdinal(col)), Cultura);

    private static long? LargoNulo(DbDataReader r, string col)
        => r.IsDBNull(r.GetOrdinal(col)) ? null : Convert.ToInt64(r.GetValue(r.GetOrdinal(col)), Cultura);

    private static int Entero(DbDataReader r, string col)
        => Convert.ToInt32(r.GetValue(r.GetOrdinal(col)), Cultura);

    private static int? EnteroNulo(DbDataReader r, string col)
        => r.IsDBNull(r.GetOrdinal(col)) ? null : Convert.ToInt32(r.GetValue(r.GetOrdinal(col)), Cultura);

    private static string? TextoNulo(DbDataReader r, string col)
        => r.IsDBNull(r.GetOrdinal(col)) ? null : r.GetString(r.GetOrdinal(col));
}
