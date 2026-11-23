using System.Data;
using System.Globalization;
using Oracle.ManagedDataAccess.Client;
using SDPIS.Api.Models.DTOs;

namespace SDPIS.Api.Repositories;

public class BitacoraRepository(IConfiguration configuration) : IBitacoraRepository
{
    private static readonly CultureInfo Cultura = CultureInfo.InvariantCulture;
    private readonly string _connectionString = configuration.GetConnectionString("SDPIS")
        ?? throw new InvalidOperationException("No se encontro la cadena de conexion 'SDPIS'.");

    private const string SelectEntrada = """
        SELECT b.id_bitacora, b.denuncia_id, b.funcionario_id, f.nombre AS funcionario_nombre,
               b.area_origen_id, a.nombre AS area_nombre, b.fecha, b.tipo_registro, b.contenido
        FROM bitacora b
        LEFT JOIN funcionario f ON f.id_funcionario = b.funcionario_id
        LEFT JOIN area a ON a.id_area = b.area_origen_id
        """;

    public async Task<BitacoraEntradaDto> RegistrarAsync(RegistroBitacora registro, CancellationToken ct)
    {
        await using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);

        try
        {
            var tx = (OracleTransaction)transaction;
            await RegistrarAsync(connection, tx, registro, ct);

            // sp_insertar_bitacora no devuelve el id: se lee la ultima entrada de
            // la misma denuncia/funcionario dentro de la misma transaccion.
            await using var command = connection.CreateCommand();
            command.Transaction = tx;
            command.CommandType = CommandType.Text;
            command.CommandText = SelectEntrada + """

                WHERE b.denuncia_id = :p_denuncia_id
                  AND (b.funcionario_id = :p_funcionario_id
                       OR (b.funcionario_id IS NULL AND :p_funcionario_id IS NULL))
                ORDER BY b.id_bitacora DESC
                FETCH FIRST 1 ROW ONLY
                """;
            command.BindByName = true;
            command.Parameters.Add(NumeroNullable("p_denuncia_id", registro.DenunciaId));
            command.Parameters.Add(NumeroNullable("p_funcionario_id", registro.FuncionarioId));

            BitacoraEntradaDto entrada;
            await using (var reader = await command.ExecuteReaderAsync(ct))
            {
                if (!await reader.ReadAsync(ct))
                    throw new InvalidOperationException("No se pudo leer la entrada de bitacora recien registrada.");
                entrada = Mapear(reader);
            }

            await transaction.CommitAsync(ct);
            return entrada;
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    public async Task RegistrarAsync(OracleConnection connection, OracleTransaction transaction,
        RegistroBitacora registro, CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandType = CommandType.StoredProcedure;
        command.CommandText = "sp_insertar_bitacora";
        command.BindByName = true;
        command.Parameters.Add(NumeroNullable("p_denuncia_id", registro.DenunciaId));
        command.Parameters.Add(NumeroNullable("p_funcionario_id", registro.FuncionarioId));
        command.Parameters.Add(NumeroNullable("p_area_origen_id", registro.AreaOrigenId));
        command.Parameters.Add(new OracleParameter("p_tipo_registro", OracleDbType.Varchar2, registro.TipoRegistro, ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter("p_contenido", OracleDbType.Varchar2, registro.Contenido, ParameterDirection.Input));
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<(List<BitacoraEntradaDto> Items, int Total)> ListarPorDenunciaAsync(
        long denunciaId, string? tipoRegistro, int pagina, int tamano, CancellationToken ct)
    {
        await using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync(ct);

        int total;
        await using (var count = connection.CreateCommand())
        {
            count.CommandType = CommandType.Text;
            count.CommandText = """
                SELECT COUNT(*) FROM bitacora
                WHERE denuncia_id = :p_denuncia_id
                  AND (:p_tipo IS NULL OR tipo_registro = :p_tipo)
                """;
            count.BindByName = true;
            count.Parameters.Add(new OracleParameter("p_denuncia_id", OracleDbType.Decimal, denunciaId, ParameterDirection.Input));
            count.Parameters.Add(new OracleParameter("p_tipo", OracleDbType.Varchar2, (object?)tipoRegistro ?? DBNull.Value, ParameterDirection.Input));
            total = Convert.ToInt32(await count.ExecuteScalarAsync(ct), Cultura);
        }

        var items = new List<BitacoraEntradaDto>();
        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = SelectEntrada + """

            WHERE b.denuncia_id = :p_denuncia_id
              AND (:p_tipo IS NULL OR b.tipo_registro = :p_tipo)
            ORDER BY b.fecha ASC, b.id_bitacora ASC
            OFFSET :p_offset ROWS FETCH NEXT :p_tamano ROWS ONLY
            """;
        command.BindByName = true;
        command.Parameters.Add(new OracleParameter("p_denuncia_id", OracleDbType.Decimal, denunciaId, ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter("p_tipo", OracleDbType.Varchar2, (object?)tipoRegistro ?? DBNull.Value, ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter("p_offset", OracleDbType.Int32, (pagina - 1) * tamano, ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter("p_tamano", OracleDbType.Int32, tamano, ParameterDirection.Input));

        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            items.Add(Mapear(reader));

        return (items, total);
    }

    public async Task<FuncionarioAccesoInfo?> ObtenerFuncionarioAsync(long funcionarioId, CancellationToken ct)
    {
        await using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = """
            SELECT id_funcionario, rol, area_id, activo
            FROM funcionario
            WHERE id_funcionario = :p_funcionario_id
            """;
        command.BindByName = true;
        command.Parameters.Add(new OracleParameter("p_funcionario_id", OracleDbType.Decimal, funcionarioId, ParameterDirection.Input));

        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
            return null;

        return new FuncionarioAccesoInfo(
            reader.GetInt64(reader.GetOrdinal("id_funcionario")),
            reader.IsDBNull(reader.GetOrdinal("rol")) ? null : reader.GetString(reader.GetOrdinal("rol")),
            reader.GetInt64(reader.GetOrdinal("area_id")),
            reader.GetString(reader.GetOrdinal("activo")) == "S");
    }

    public async Task<DenunciaAccesoInfo?> ObtenerDenunciaAsync(long denunciaId, CancellationToken ct)
    {
        await using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = """
            SELECT d.id_denuncia, d.area_actual_id, d.funcionario_asignado_id,
                   CASE WHEN EXISTS (
                       SELECT 1 FROM solicitud_cierre s
                       WHERE s.denuncia_id = d.id_denuncia AND s.estado = 'PENDIENTE'
                   ) THEN 'S' ELSE 'N' END AS cierre_pendiente
            FROM denuncia d
            WHERE d.id_denuncia = :p_denuncia_id
            """;
        command.BindByName = true;
        command.Parameters.Add(new OracleParameter("p_denuncia_id", OracleDbType.Decimal, denunciaId, ParameterDirection.Input));

        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
            return null;

        return new DenunciaAccesoInfo(
            reader.GetInt64(reader.GetOrdinal("id_denuncia")),
            LeerLongNullable(reader, "area_actual_id"),
            LeerLongNullable(reader, "funcionario_asignado_id"),
            reader.GetString(reader.GetOrdinal("cierre_pendiente")) == "S");
    }

    public async Task<List<DenunciaBitacoraListadoDto>> ListarDenunciasVisiblesAsync(
        long? areaId, long? funcionarioAsignadoId, CancellationToken ct)
    {
        var denuncias = new List<DenunciaBitacoraListadoDto>();

        await using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = """
            SELECT d.id_denuncia, d.consecutivo, d.estado, d.fecha_creacion,
                   d.funcionario_asignado_id, f.nombre AS funcionario_asignado_nombre,
                   d.area_actual_id, a.nombre AS area_nombre
            FROM denuncia d
            LEFT JOIN funcionario f ON f.id_funcionario = d.funcionario_asignado_id
            LEFT JOIN area a ON a.id_area = d.area_actual_id
            WHERE (:p_area_id IS NULL OR d.area_actual_id = :p_area_id)
              AND (:p_funcionario_id IS NULL OR d.funcionario_asignado_id = :p_funcionario_id)
            ORDER BY d.fecha_creacion DESC
            """;
        command.BindByName = true;
        command.Parameters.Add(NumeroNullable("p_area_id", areaId));
        command.Parameters.Add(NumeroNullable("p_funcionario_id", funcionarioAsignadoId));

        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            denuncias.Add(new DenunciaBitacoraListadoDto(
                reader.GetInt64(reader.GetOrdinal("id_denuncia")),
                reader.GetString(reader.GetOrdinal("consecutivo")),
                reader.GetString(reader.GetOrdinal("estado")),
                reader.GetDateTime(reader.GetOrdinal("fecha_creacion")).ToString("dd MMM. yyyy", Cultura).ToLowerInvariant(),
                LeerLongNullable(reader, "funcionario_asignado_id"),
                LeerStringNullable(reader, "funcionario_asignado_nombre"),
                LeerLongNullable(reader, "area_actual_id"),
                LeerStringNullable(reader, "area_nombre"),
                reader.GetDateTime(reader.GetOrdinal("fecha_creacion")).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            ));
        }

        return denuncias;
    }

    private static BitacoraEntradaDto Mapear(OracleDataReader reader) => new(
        reader.GetInt64(reader.GetOrdinal("id_bitacora")),
        reader.GetInt64(reader.GetOrdinal("denuncia_id")),
        LeerLongNullable(reader, "funcionario_id"),
        LeerStringNullable(reader, "funcionario_nombre"),
        LeerLongNullable(reader, "area_origen_id"),
        LeerStringNullable(reader, "area_nombre"),
        reader.GetDateTime(reader.GetOrdinal("fecha")).ToString("yyyy-MM-dd'T'HH:mm:ss.fff", Cultura),
        reader.GetString(reader.GetOrdinal("tipo_registro")),
        LeerStringNullable(reader, "contenido"));

    private static long? LeerLongNullable(OracleDataReader reader, string columna)
    {
        var ordinal = reader.GetOrdinal(columna);
        return reader.IsDBNull(ordinal) ? null : reader.GetInt64(ordinal);
    }

    private static string? LeerStringNullable(OracleDataReader reader, string columna)
    {
        var ordinal = reader.GetOrdinal(columna);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static OracleParameter NumeroNullable(string nombre, long? valor)
        => new(nombre, OracleDbType.Decimal, (object?)valor ?? DBNull.Value, ParameterDirection.Input);
}
