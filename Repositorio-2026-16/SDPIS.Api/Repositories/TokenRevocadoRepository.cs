using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace SDPIS.Api.Repositories;

// HU-028: acceso a token_revocado. Registra el claim JTI cuando una sesion
// se cierra y permite consultarlo para rechazar (401) las peticiones que
// sigan usando ese token. Solo lectura + insercion; las filas viejas se
// eliminan al revocar para mantener la tabla pequena (limpieza de 24 h).
public class TokenRevocadoRepository(IConfiguration configuration) : ITokenRevocadoRepository
{
    private readonly string _connectionString = configuration.GetConnectionString("SDPIS")
        ?? throw new InvalidOperationException("No se encontro la cadena de conexion 'SDPIS'.");

    public async Task<bool> EstaRevocadoAsync(string jti, CancellationToken ct)
    {
        await using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        command.CommandType = System.Data.CommandType.Text;
        command.CommandText = """
            SELECT COUNT(*)
            FROM token_revocado
            WHERE jti = :p_jti
            """;
        command.BindByName = true;
        command.Parameters.Add(new OracleParameter("p_jti", OracleDbType.Varchar2, jti, ParameterDirection.Input));

        return Convert.ToInt32(await command.ExecuteScalarAsync(ct), System.Globalization.CultureInfo.InvariantCulture) > 0;
    }

    public async Task RevocarAsync(RegistroTokenRevocado registro, CancellationToken ct)
    {
        await using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync(ct);

        // Limpieza: un JWT vive a lo sumo MinutosExpiracion (60 min); una
        // revocacion dejada de mas de 24 h ya no puede afectar a nada.
        await using (var limpieza = connection.CreateCommand())
        {
            limpieza.CommandType = System.Data.CommandType.Text;
            limpieza.CommandText = """
                DELETE FROM token_revocado
                WHERE fecha_revocacion < SYSDATE - 1
                """;
            await limpieza.ExecuteNonQueryAsync(ct);
        }

        await using var command = connection.CreateCommand();
        command.CommandType = System.Data.CommandType.Text;
        command.CommandText = """
            INSERT INTO token_revocado (
                jti, identificacion, id_usuario_temporal, motivo
            ) VALUES (
                :p_jti, :p_identificacion, :p_id_usuario_temporal, :p_motivo
            )
            """;
        command.BindByName = true;
        command.Parameters.Add(new OracleParameter("p_jti", OracleDbType.Varchar2, registro.Jti, ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter("p_identificacion", OracleDbType.Varchar2, registro.Identificacion, ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter("p_id_usuario_temporal", OracleDbType.Decimal, (object?)registro.IdUsuarioTemporal ?? DBNull.Value, ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter("p_motivo", OracleDbType.Varchar2, registro.Motivo, ParameterDirection.Input));
        await command.ExecuteNonQueryAsync(ct);
    }
}