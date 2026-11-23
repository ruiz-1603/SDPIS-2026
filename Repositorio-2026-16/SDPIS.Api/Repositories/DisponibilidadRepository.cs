using System.Data;
using System.Globalization;
using Oracle.ManagedDataAccess.Client;

namespace SDPIS.Api.Repositories;

public class DisponibilidadRepository(IConfiguration configuration) : IDisponibilidadRepository
{
    private readonly string _connectionString = configuration.GetConnectionString("SDPIS")
        ?? throw new InvalidOperationException("No se encontro la cadena de conexion 'SDPIS'.");

    public async Task<PeriodoIndisponibilidadRegistro> InsertarPeriodoAsync(
        long funcionarioId, string motivo, DateOnly fechaInicio, DateOnly? fechaFinEstimada,
        string? registradoPor, CancellationToken ct)
    {
        await using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = """
            INSERT INTO periodo_indisponibilidad
                (funcionario_id, motivo, fecha_inicio, fecha_fin_estimada, registrado_por, fecha_registro)
            VALUES
                (:p_funcionario_id, :p_motivo, :p_fecha_inicio, :p_fecha_fin_estimada, :p_registrado_por, SYSDATE)
            RETURNING id_periodo INTO :p_id_periodo
            """;
        command.BindByName = true;

        command.Parameters.Add(new OracleParameter("p_funcionario_id", OracleDbType.Decimal, funcionarioId, ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter("p_motivo", OracleDbType.Varchar2, motivo, ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter("p_fecha_inicio", OracleDbType.Date, fechaInicio.ToDateTime(TimeOnly.MinValue), ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter("p_fecha_fin_estimada", OracleDbType.Date,
            fechaFinEstimada is { } ff ? ff.ToDateTime(TimeOnly.MinValue) : DBNull.Value,
            ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter("p_registrado_por", OracleDbType.Varchar2,
            registradoPor ?? (object)DBNull.Value, ParameterDirection.Input));
        var pIdPeriodo = new OracleParameter("p_id_periodo", OracleDbType.Decimal, ParameterDirection.Output);
        command.Parameters.Add(pIdPeriodo);

        try
        {
            await command.ExecuteNonQueryAsync(ct);
        }
        catch (OracleException ex) when (ex.Number == 2291)
        {
            // ORA-02291: el funcionario indicado no existe (violacion de FK).
            throw new DenunciaBaseDatosException("El funcionario indicado no existe.", ex);
        }
        catch (OracleException ex) when (ex.Number is >= 20000 and <= 20999)
        {
            throw new DenunciaBaseDatosException(ex.Message, ex);
        }

        var idPeriodo = long.Parse(pIdPeriodo.Value!.ToString()!, CultureInfo.InvariantCulture);
        return new PeriodoIndisponibilidadRegistro(
            idPeriodo, funcionarioId, motivo, fechaInicio, fechaFinEstimada,
            registradoPor, DateOnly.FromDateTime(DateTime.Today));
    }

    public async Task<List<PeriodoIndisponibilidadRegistro>> ObtenerPeriodosAsync(
        long funcionarioId, CancellationToken ct)
    {
        var periodos = new List<PeriodoIndisponibilidadRegistro>();

        await using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = """
            SELECT id_periodo, funcionario_id, motivo, fecha_inicio, fecha_fin_estimada,
                   registrado_por, fecha_registro
            FROM periodo_indisponibilidad
            WHERE funcionario_id = :p_funcionario_id
            ORDER BY fecha_inicio DESC
            """;
        command.BindByName = true;
        command.Parameters.Add(new OracleParameter("p_funcionario_id", OracleDbType.Decimal, funcionarioId, ParameterDirection.Input));

        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            periodos.Add(new PeriodoIndisponibilidadRegistro(
                reader.GetInt64(reader.GetOrdinal("id_periodo")),
                reader.GetInt64(reader.GetOrdinal("funcionario_id")),
                reader.GetString(reader.GetOrdinal("motivo")),
                DateOnly.FromDateTime(reader.GetDateTime(reader.GetOrdinal("fecha_inicio"))),
                reader.IsDBNull(reader.GetOrdinal("fecha_fin_estimada"))
                    ? null
                    : DateOnly.FromDateTime(reader.GetDateTime(reader.GetOrdinal("fecha_fin_estimada"))),
                reader.IsDBNull(reader.GetOrdinal("registrado_por"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("registrado_por")),
                reader.IsDBNull(reader.GetOrdinal("fecha_registro"))
                    ? null
                    : DateOnly.FromDateTime(reader.GetDateTime(reader.GetOrdinal("fecha_registro")))
            ));
        }

        return periodos;
    }

    public async Task<bool> ExisteFuncionarioAsync(long funcionarioId, CancellationToken ct)
    {
        await using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = "SELECT COUNT(1) FROM funcionario WHERE id_funcionario = :p_funcionario_id";
        command.BindByName = true;
        command.Parameters.Add(new OracleParameter("p_funcionario_id", OracleDbType.Decimal, funcionarioId, ParameterDirection.Input));

        var cantidad = Convert.ToInt64(await command.ExecuteScalarAsync(ct), CultureInfo.InvariantCulture);
        return cantidad > 0;
    }

    public async Task<List<FuncionarioInfo>> ObtenerFuncionariosAsync(long? areaId, CancellationToken ct)
    {
        var funcionarios = new List<FuncionarioInfo>();

        await using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = """
            SELECT f.id_funcionario, f.nombre, f.rol, a.nombre AS area_nombre
            FROM funcionario f
            JOIN area a ON a.id_area = f.area_id
            WHERE f.activo = 'S'
              AND (:p_area_id IS NULL OR f.area_id = :p_area_id
                   OR EXISTS (SELECT 1 FROM funcionario_area_adicional fa
                               WHERE fa.funcionario_id = f.id_funcionario
                                 AND fa.area_id = :p_area_id))
            ORDER BY f.nombre
            """;
        command.BindByName = true;
        command.Parameters.Add(new OracleParameter("p_area_id", OracleDbType.Decimal,
            (object?)areaId ?? DBNull.Value, ParameterDirection.Input));

        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            funcionarios.Add(new FuncionarioInfo(
                reader.GetInt64(reader.GetOrdinal("id_funcionario")),
                reader.GetString(reader.GetOrdinal("nombre")),
                reader.IsDBNull(reader.GetOrdinal("rol")) ? null : reader.GetString(reader.GetOrdinal("rol")),
                reader.GetString(reader.GetOrdinal("area_nombre"))
            ));
        }

        return funcionarios;
    }

    public async Task<PeriodoIndisponibilidadRegistro> ActualizarPeriodoAsync(
        long idPeriodo, long funcionarioId, string motivo, DateOnly fechaInicio,
        DateOnly? fechaFinEstimada, string? registradoPor, CancellationToken ct)
    {
        await using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = """
            UPDATE periodo_indisponibilidad
            SET motivo = :p_motivo,
                fecha_inicio = :p_fecha_inicio,
                fecha_fin_estimada = :p_fecha_fin_estimada,
                registrado_por = :p_registrado_por,
                fecha_registro = SYSDATE
            WHERE id_periodo = :p_id_periodo
              AND funcionario_id = :p_funcionario_id
            """;
        command.BindByName = true;

        command.Parameters.Add(new OracleParameter("p_motivo", OracleDbType.Varchar2, motivo, ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter("p_fecha_inicio", OracleDbType.Date, fechaInicio.ToDateTime(TimeOnly.MinValue), ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter("p_fecha_fin_estimada", OracleDbType.Date,
            fechaFinEstimada is { } ff ? ff.ToDateTime(TimeOnly.MinValue) : DBNull.Value,
            ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter("p_registrado_por", OracleDbType.Varchar2,
            registradoPor ?? (object)DBNull.Value, ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter("p_id_periodo", OracleDbType.Decimal, idPeriodo, ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter("p_funcionario_id", OracleDbType.Decimal, funcionarioId, ParameterDirection.Input));

        var filas = await command.ExecuteNonQueryAsync(ct);
        if (filas == 0)
        {
            throw new DenunciaBaseDatosException(
                "El periodo de indisponibilidad indicado no existe.",
                new InvalidOperationException($"No se encontro el periodo {idPeriodo} del funcionario {funcionarioId}."));
        }

        return new PeriodoIndisponibilidadRegistro(
            idPeriodo, funcionarioId, motivo, fechaInicio, fechaFinEstimada,
            registradoPor, DateOnly.FromDateTime(DateTime.Today));
    }

    public async Task<List<PeriodoIndisponibilidadRegistro>> ObtenerTodosLosPeriodosAsync(CancellationToken ct)
    {
        var periodos = new List<PeriodoIndisponibilidadRegistro>();

        await using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = """
            SELECT id_periodo, funcionario_id, motivo, fecha_inicio, fecha_fin_estimada,
                   registrado_por, fecha_registro
            FROM periodo_indisponibilidad
            ORDER BY funcionario_id, fecha_inicio
            """;
        command.BindByName = true;

        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            periodos.Add(new PeriodoIndisponibilidadRegistro(
                reader.GetInt64(reader.GetOrdinal("id_periodo")),
                reader.GetInt64(reader.GetOrdinal("funcionario_id")),
                reader.GetString(reader.GetOrdinal("motivo")),
                DateOnly.FromDateTime(reader.GetDateTime(reader.GetOrdinal("fecha_inicio"))),
                reader.IsDBNull(reader.GetOrdinal("fecha_fin_estimada"))
                    ? null
                    : DateOnly.FromDateTime(reader.GetDateTime(reader.GetOrdinal("fecha_fin_estimada"))),
                reader.IsDBNull(reader.GetOrdinal("registrado_por"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("registrado_por")),
                reader.IsDBNull(reader.GetOrdinal("fecha_registro"))
                    ? null
                    : DateOnly.FromDateTime(reader.GetDateTime(reader.GetOrdinal("fecha_registro")))
            ));
        }

        return periodos;
    }

    public async Task EliminarPeriodoAsync(long idPeriodo, long funcionarioId, CancellationToken ct)
    {
        await using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = "DELETE FROM periodo_indisponibilidad WHERE id_periodo = :p_id_periodo AND funcionario_id = :p_funcionario_id";
        command.BindByName = true;
        command.Parameters.Add(new OracleParameter("p_id_periodo", OracleDbType.Decimal, idPeriodo, ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter("p_funcionario_id", OracleDbType.Decimal, funcionarioId, ParameterDirection.Input));

        var filas = await command.ExecuteNonQueryAsync(ct);
        if (filas == 0)
        {
            throw new DenunciaBaseDatosException(
                "El periodo de indisponibilidad indicado no existe.",
                new InvalidOperationException($"No se encontro el periodo {idPeriodo} del funcionario {funcionarioId}."));
        }
    }
}
