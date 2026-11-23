using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace SDPIS.Api.Repositories;

public class UsuarioTemporalRepository(IConfiguration configuration) : IUsuarioTemporalRepository
{
    private readonly string _connectionString = configuration.GetConnectionString("SDPIS")
        ?? throw new InvalidOperationException("No se encontro la cadena de conexion 'SDPIS'.");

    public async Task<UsuarioTemporalAuth?> ObtenerPorCodigoAsync(string codigoUsuario, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(codigoUsuario))
            return null;

        await using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        command.CommandType = System.Data.CommandType.Text;
        // Cada usuario temporal es un funcionario. Los permisos internos (rol,
        // estado activo y area) son del SDPIS y se toman SOLO de funcionario;
        // usuario_temporal solo aporta credenciales e identidad, igual que
        // hara el MSI (que no conoce roles ni areas del SDPIS).
        command.CommandText = """
            SELECT u.id_usuario_temporal, u.identificacion, u.nombre, u.apellido1, u.apellido2,
                   u.correo_institucional, u.clave_hash,
                   f.id_funcionario, f.area_id, f.rol, NVL(f.activo, 'S') AS activo
            FROM usuario_temporal u
            JOIN funcionario f ON f.id_funcionario = u.funcionario_id
            WHERE u.identificacion = :p_codigo OR u.correo_institucional = LOWER(:p_codigo)
            FETCH FIRST 1 ROW ONLY
            """;
        command.BindByName = true;
        command.Parameters.Add(new OracleParameter("p_codigo", OracleDbType.Varchar2, codigoUsuario, ParameterDirection.Input));

        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
            return null;

        return new UsuarioTemporalAuth(
            reader.GetInt64(reader.GetOrdinal("id_usuario_temporal")),
            reader.GetString(reader.GetOrdinal("identificacion")),
            reader.GetString(reader.GetOrdinal("nombre")),
            reader.GetString(reader.GetOrdinal("apellido1")),
            reader.IsDBNull(reader.GetOrdinal("apellido2")) ? null : reader.GetString(reader.GetOrdinal("apellido2")),
            reader.IsDBNull(reader.GetOrdinal("correo_institucional")) ? null : reader.GetString(reader.GetOrdinal("correo_institucional")),
            reader.GetString(reader.GetOrdinal("clave_hash")),
            reader.GetInt64(reader.GetOrdinal("id_funcionario")),
            reader.GetInt64(reader.GetOrdinal("area_id")),
            reader.IsDBNull(reader.GetOrdinal("rol")) ? null : reader.GetString(reader.GetOrdinal("rol")),
            reader.GetString(reader.GetOrdinal("activo")) == "S");
    }

    public async Task<int> ContarExitosAsync(long idUsuarioTemporal, CancellationToken ct)
    {
        await using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        command.CommandType = System.Data.CommandType.Text;
        command.CommandText = """
            SELECT COUNT(*)
            FROM bitacora_sesion
            WHERE id_usuario_temporal = :p_id AND resultado = 'EXITO'
            """;
        command.BindByName = true;
        command.Parameters.Add(new OracleParameter("p_id", OracleDbType.Decimal, idUsuarioTemporal, ParameterDirection.Input));

        return Convert.ToInt32(await command.ExecuteScalarAsync(ct), System.Globalization.CultureInfo.InvariantCulture);
    }
}