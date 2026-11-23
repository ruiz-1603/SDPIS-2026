using Oracle.ManagedDataAccess.Client;

namespace SDPIS.Api.Repositories;

public class BitacoraSesionRepository(IConfiguration configuration) : IBitacoraSesionRepository
{
    private readonly string _connectionString = configuration.GetConnectionString("SDPIS")
        ?? throw new InvalidOperationException("No se encontro la cadena de conexion 'SDPIS'.");

    public async Task RegistrarAsync(RegistroBitacoraSesion registro, CancellationToken ct)
    {
        await using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        command.CommandType = System.Data.CommandType.Text;
        command.CommandText = """
            INSERT INTO bitacora_sesion (
                codigo_usuario, id_usuario_temporal, resultado, motivo,
                direccion_ip, agente_usuario
            ) VALUES (
                :p_codigo_usuario, :p_id_usuario_temporal, :p_resultado, :p_motivo,
                :p_ip, :p_agente
            )
            """;
        command.BindByName = true;
        command.Parameters.Add(new OracleParameter("p_codigo_usuario", OracleDbType.Varchar2, registro.CodigoUsuario, System.Data.ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter("p_id_usuario_temporal", OracleDbType.Decimal, (object?)registro.IdUsuarioTemporal ?? DBNull.Value, System.Data.ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter("p_resultado", OracleDbType.Varchar2, registro.Resultado, System.Data.ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter("p_motivo", OracleDbType.Varchar2, (object?)registro.Motivo ?? DBNull.Value, System.Data.ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter("p_ip", OracleDbType.Varchar2, (object?)registro.DireccionIp ?? DBNull.Value, System.Data.ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter("p_agente", OracleDbType.Varchar2, (object?)registro.AgenteUsuario ?? DBNull.Value, System.Data.ParameterDirection.Input));
        await command.ExecuteNonQueryAsync(ct);
    }
}