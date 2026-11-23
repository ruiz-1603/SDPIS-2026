using System.Data;
using System.Globalization;
using Oracle.ManagedDataAccess.Client;
using SDPIS.Api.Models.DTOs;

namespace SDPIS.Api.Repositories;

public class AsignacionRepository(IConfiguration configuration, IBitacoraRepository bitacoraRepository) : IAsignacionRepository
{
    private static readonly CultureInfo Cultura = CultureInfo.InvariantCulture;
    private readonly string _connectionString = configuration.GetConnectionString("SDPIS")
        ?? throw new InvalidOperationException("No se encontro la cadena de conexion 'SDPIS'.");

    // HU-009 + HU-015: areaId null = todas las areas (superusuario).
    public async Task<List<DenunciaPendienteDto>> ObtenerDenunciasPendientesAsignacionAsync(long? areaId, CancellationToken ct)
    {
        var denuncias = new List<DenunciaPendienteDto>();

        await using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = """
            SELECT d.id_denuncia, d.consecutivo, d.fecha_creacion,
                   d.area_actual_id, a.nombre AS area_nombre
            FROM denuncia d
            JOIN area a ON a.id_area = d.area_actual_id
            WHERE (:p_area_id IS NULL OR d.area_actual_id = :p_area_id)
              AND d.estado = 'PENDIENTE_ASIGNACION'
              AND NOT EXISTS (
                  SELECT 1 FROM evaluacion_criticidad ec
                  WHERE ec.denuncia_id = d.id_denuncia
                    AND ec.nivel_criticidad IS NOT NULL
              )
            ORDER BY d.fecha_creacion ASC
            """;
        command.BindByName = true;
        command.Parameters.Add(new OracleParameter("p_area_id", OracleDbType.Decimal,
            (object?)areaId ?? DBNull.Value, ParameterDirection.Input));

        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            denuncias.Add(new DenunciaPendienteDto(
                reader.GetInt64(reader.GetOrdinal("id_denuncia")),
                reader.GetString(reader.GetOrdinal("consecutivo")),
                reader.GetDateTime(reader.GetOrdinal("fecha_creacion")).ToString("dd MMM. yyyy", Cultura).ToLowerInvariant(),
                reader.GetInt64(reader.GetOrdinal("area_actual_id")),
                reader.GetString(reader.GetOrdinal("area_nombre"))
            ));
        }

        return denuncias;
    }

    public async Task<List<DenunciaAsignadaDto>> ObtenerDenunciasAsignadasAsync(long? areaId, CancellationToken ct)
    {
        var denuncias = new List<DenunciaAsignadaDto>();

        await using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = """
            SELECT d.id_denuncia, d.consecutivo, d.fecha_creacion,
                   d.funcionario_asignado_id, f.nombre AS nombre_validador,
                   d.area_actual_id, a.nombre AS area_nombre
            FROM denuncia d
            JOIN funcionario f ON d.funcionario_asignado_id = f.id_funcionario
            JOIN area a ON a.id_area = d.area_actual_id
            WHERE (:p_area_id IS NULL OR d.area_actual_id = :p_area_id)
              AND d.estado = 'EN_TIEMPO'
              AND NOT EXISTS (
                  SELECT 1 FROM evaluacion_criticidad ec
                  WHERE ec.denuncia_id = d.id_denuncia
                    AND ec.nivel_criticidad IS NOT NULL
              )
            ORDER BY d.fecha_creacion DESC
            """;
        command.BindByName = true;
        command.Parameters.Add(new OracleParameter("p_area_id", OracleDbType.Decimal,
            (object?)areaId ?? DBNull.Value, ParameterDirection.Input));

        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            denuncias.Add(new DenunciaAsignadaDto(
                reader.GetInt64(reader.GetOrdinal("id_denuncia")),
                reader.GetString(reader.GetOrdinal("consecutivo")),
                reader.GetDateTime(reader.GetOrdinal("fecha_creacion")).ToString("dd MMM. yyyy", Cultura).ToLowerInvariant(),
                reader.GetInt64(reader.GetOrdinal("funcionario_asignado_id")),
                reader.GetString(reader.GetOrdinal("nombre_validador")),
                reader.GetInt64(reader.GetOrdinal("area_actual_id")),
                reader.GetString(reader.GetOrdinal("area_nombre"))
            ));
        }

        return denuncias;
    }

    public async Task<List<ValidadorCargaDto>> ObtenerValidadoresDisponiblesConCargaAsync(long areaId, CancellationToken ct)
    {
        var validadores = new List<ValidadorCargaDto>();

        await using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        // Cuenta denuncias que no estén finalizadas
        command.CommandText = """
            SELECT f.id_funcionario, f.nombre,
                   (SELECT COUNT(d.id_denuncia) 
                    FROM denuncia d 
                    WHERE d.funcionario_asignado_id = f.id_funcionario 
                      AND d.estado != 'FINALIZADA') AS carga_activa
            FROM funcionario f
            WHERE (f.area_id = :p_area_id
                   OR EXISTS (SELECT 1 FROM funcionario_area_adicional fa
                               WHERE fa.funcionario_id = f.id_funcionario
                                 AND fa.area_id = :p_area_id))
              AND f.rol = 'validador'
              AND f.activo = 'S'
              AND NOT EXISTS (
                  SELECT 1 
                  FROM periodo_indisponibilidad pi 
                  WHERE pi.funcionario_id = f.id_funcionario
                    AND pi.fecha_inicio <= TRUNC(SYSDATE)
                    AND (pi.fecha_fin_estimada IS NULL OR pi.fecha_fin_estimada >= TRUNC(SYSDATE))
              )
            ORDER BY carga_activa ASC, f.id_funcionario ASC
            """;
        command.BindByName = true;
        command.Parameters.Add(new OracleParameter("p_area_id", OracleDbType.Decimal, areaId, ParameterDirection.Input));

        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            validadores.Add(new ValidadorCargaDto(
                reader.GetInt64(reader.GetOrdinal("id_funcionario")),
                reader.GetString(reader.GetOrdinal("nombre")),
                Convert.ToInt32(reader.GetDecimal(reader.GetOrdinal("carga_activa")))
            ));
        }

        return validadores;
    }

    public async Task AsignarValidadorManualAsync(long denunciaId, long validadorId, long jefeAreaId, CancellationToken ct)
    {
        await using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);

        try
        {
            // 0. Datos para la bitácora (HU-016), leidos antes de asignar: area de
            // origen real de la denuncia (igual que las asignaciones de HU-012,
            // scripts 13/14) y quien la tenia, para registrar si es reasignacion.
            long? areaOrigenId = null;
            string? nombreAnterior = null;
            string nombreValidador = $"#{validadorId}";
            await using (var cmdDatos = connection.CreateCommand())
            {
                cmdDatos.Transaction = (OracleTransaction)transaction;
                cmdDatos.CommandType = CommandType.Text;
                cmdDatos.CommandText = """
                    SELECT d.area_actual_id,
                           CASE WHEN d.funcionario_asignado_id != :p_validador_id THEN fa.nombre END AS nombre_anterior,
                           fv.nombre AS nombre_validador
                    FROM denuncia d
                    LEFT JOIN funcionario fa ON fa.id_funcionario = d.funcionario_asignado_id
                    LEFT JOIN funcionario fv ON fv.id_funcionario = :p_validador_id
                    WHERE d.id_denuncia = :p_denuncia_id
                    """;
                cmdDatos.BindByName = true;
                cmdDatos.Parameters.Add(new OracleParameter("p_denuncia_id", OracleDbType.Decimal, denunciaId, ParameterDirection.Input));
                cmdDatos.Parameters.Add(new OracleParameter("p_validador_id", OracleDbType.Decimal, validadorId, ParameterDirection.Input));
                await using var reader = await cmdDatos.ExecuteReaderAsync(ct);
                if (await reader.ReadAsync(ct))
                {
                    var ordArea = reader.GetOrdinal("area_actual_id");
                    var ordAnterior = reader.GetOrdinal("nombre_anterior");
                    var ordValidador = reader.GetOrdinal("nombre_validador");
                    areaOrigenId = reader.IsDBNull(ordArea) ? null : reader.GetInt64(ordArea);
                    nombreAnterior = reader.IsDBNull(ordAnterior) ? null : reader.GetString(ordAnterior);
                    if (!reader.IsDBNull(ordValidador))
                        nombreValidador = reader.GetString(ordValidador);
                }
            }

            // 1. Asignar denuncia
            await using var cmdAsignar = connection.CreateCommand();
            cmdAsignar.Transaction = (OracleTransaction)transaction;
            cmdAsignar.CommandType = CommandType.StoredProcedure;
            cmdAsignar.CommandText = "sp_asignar_validador_denuncia";
            cmdAsignar.BindByName = true;
            cmdAsignar.Parameters.Add(new OracleParameter("p_denuncia_id", OracleDbType.Decimal, denunciaId, ParameterDirection.Input));
            cmdAsignar.Parameters.Add(new OracleParameter("p_validador_id", OracleDbType.Decimal, validadorId, ParameterDirection.Input));
            await cmdAsignar.ExecuteNonQueryAsync(ct);

            // 2. Insertar bitácora (HU-016)
            var contenido = nombreAnterior is null
                ? $"Denuncia asignada al validador {nombreValidador}."
                : $"Denuncia reasignada del validador {nombreAnterior} al validador {nombreValidador}.";
            await bitacoraRepository.RegistrarAsync(connection, (OracleTransaction)transaction,
                new RegistroBitacora(denunciaId, jefeAreaId, areaOrigenId, TiposBitacora.Asignacion, contenido), ct);

            // 3. Insertar notificación
            await using var cmdNotificacion = connection.CreateCommand();
            cmdNotificacion.Transaction = (OracleTransaction)transaction;
            cmdNotificacion.CommandType = CommandType.StoredProcedure;
            cmdNotificacion.CommandText = "sp_insertar_notificacion";
            cmdNotificacion.BindByName = true;
            cmdNotificacion.Parameters.Add(new OracleParameter("p_denuncia_id", OracleDbType.Decimal, denunciaId, ParameterDirection.Input));
            cmdNotificacion.Parameters.Add(new OracleParameter("p_tipo", OracleDbType.Varchar2, "asignacion", ParameterDirection.Input));
            cmdNotificacion.Parameters.Add(new OracleParameter("p_destinatario_tipo", OracleDbType.Varchar2, "funcionario", ParameterDirection.Input));
            cmdNotificacion.Parameters.Add(new OracleParameter("p_destinatario_funcionario_id", OracleDbType.Decimal, validadorId, ParameterDirection.Input));
            cmdNotificacion.Parameters.Add(new OracleParameter("p_mensaje", OracleDbType.Varchar2, "Se le ha asignado una nueva denuncia para validación", ParameterDirection.Input));
            await cmdNotificacion.ExecuteNonQueryAsync(ct);

            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<List<DenunciaAtrasadaDto>> ObtenerDenunciasAtrasadasAsignacionAsync(int diasLimite, CancellationToken ct)
    {
        var denuncias = new List<DenunciaAtrasadaDto>();

        await using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = """
            SELECT id_denuncia, area_actual_id, consecutivo
            FROM denuncia
            WHERE estado = 'PENDIENTE_ASIGNACION'
              AND (TRUNC(SYSDATE) - TRUNC(fecha_creacion)) >= :p_dias_limite
            """;
        command.BindByName = true;
        command.Parameters.Add(new OracleParameter("p_dias_limite", OracleDbType.Int32, diasLimite, ParameterDirection.Input));

        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            denuncias.Add(new DenunciaAtrasadaDto(
                reader.GetInt64(reader.GetOrdinal("id_denuncia")),
                reader.GetInt64(reader.GetOrdinal("area_actual_id")),
                reader.GetString(reader.GetOrdinal("consecutivo"))
            ));
        }

        return denuncias;
    }

    public async Task GenerarRecordatorioAtrasoAsync(long denunciaId, string consecutivo, CancellationToken ct)
    {
        await using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync(ct);

        await using var cmdNotificacion = connection.CreateCommand();
        cmdNotificacion.CommandType = CommandType.StoredProcedure;
        cmdNotificacion.CommandText = "sp_insertar_notificacion";
        cmdNotificacion.BindByName = true;
        cmdNotificacion.Parameters.Add(new OracleParameter("p_denuncia_id", OracleDbType.Decimal, denunciaId, ParameterDirection.Input));
        cmdNotificacion.Parameters.Add(new OracleParameter("p_tipo", OracleDbType.Varchar2, "recordatorio", ParameterDirection.Input));
        cmdNotificacion.Parameters.Add(new OracleParameter("p_destinatario_tipo", OracleDbType.Varchar2, "jefe_area", ParameterDirection.Input));
        cmdNotificacion.Parameters.Add(new OracleParameter("p_destinatario_funcionario_id", OracleDbType.Decimal, DBNull.Value, ParameterDirection.Input));
        cmdNotificacion.Parameters.Add(new OracleParameter("p_mensaje", OracleDbType.Varchar2, $"La denuncia {consecutivo} lleva más de 3 días sin ser asignada.", ParameterDirection.Input));
        
        await cmdNotificacion.ExecuteNonQueryAsync(ct);
    }
}
