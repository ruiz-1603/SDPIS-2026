using System.Data;
using Oracle.ManagedDataAccess.Client;
using SDPIS.Api.Models.DTOs;

namespace SDPIS.Api.Repositories;

// HU-014: acceso a datos de los plazos por nivel de criticidad
// (tabla nivel_criticidad). Las reglas de negocio viven en
// PlazosAtencionService. Los metodos con (connection, transaction)
// participan de la transaccion del servicio.
public interface IPlazosAtencionRepository
{
    Task<OracleConnection> AbrirConexionAsync(CancellationToken ct);
    Task<List<PlazoCriticidadDto>> ListarAsync(CancellationToken ct);

    // Bloquea (FOR UPDATE) todas las filas para validar el orden entre
    // niveles sin que otra solicitud lo altere a la vez.
    Task<List<PlazoCriticidadDto>> BloquearTodosAsync(OracleConnection c, OracleTransaction t, CancellationToken ct);
    Task ActualizarPlazoAsync(OracleConnection c, OracleTransaction t, string nivel, int plazoDias, CancellationToken ct);
    Task RegistrarBitacoraAsync(OracleConnection c, OracleTransaction t, RegistroBitacoraAdministrativa registro, CancellationToken ct);
}

public class PlazosAtencionRepository(IConfiguration configuration) : IPlazosAtencionRepository
{
    private readonly string _connectionString = configuration.GetConnectionString("SDPIS")
        ?? throw new InvalidOperationException("No se encontro la cadena de conexion 'SDPIS'.");

    public async Task<OracleConnection> AbrirConexionAsync(CancellationToken ct)
    {
        var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync(ct);
        return connection;
    }

    public async Task<List<PlazoCriticidadDto>> ListarAsync(CancellationToken ct)
    {
        await using var connection = await AbrirConexionAsync(ct);
        await using var command = Comando(connection, null, ConsultaBase);
        return await LeerAsync(command, ct);
    }

    public async Task<List<PlazoCriticidadDto>> BloquearTodosAsync(OracleConnection c, OracleTransaction t, CancellationToken ct)
    {
        await using var command = Comando(c, t, ConsultaBase + " FOR UPDATE");
        return await LeerAsync(command, ct);
    }

    public async Task ActualizarPlazoAsync(OracleConnection c, OracleTransaction t, string nivel, int plazoDias, CancellationToken ct)
    {
        await using var command = Comando(c, t,
            "UPDATE nivel_criticidad SET plazo_dias = :p_plazo WHERE nivel = :p_nivel");
        command.Parameters.Add(new OracleParameter("p_plazo", OracleDbType.Decimal, plazoDias, ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter("p_nivel", OracleDbType.Varchar2, nivel, ParameterDirection.Input));
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task RegistrarBitacoraAsync(OracleConnection c, OracleTransaction t, RegistroBitacoraAdministrativa r, CancellationToken ct)
    {
        await using var command = Comando(c, t, """
            INSERT INTO bitacora_administrativa
                   (realizado_por_id, funcionario_afectado_id, area_id, categoria,
                    detalle, valor_anterior, valor_nuevo)
            VALUES (:p_realizado_por, :p_afectado, :p_area_id, :p_categoria,
                    :p_detalle, :p_anterior, :p_nuevo)
            """);
        command.Parameters.Add(new OracleParameter("p_realizado_por", OracleDbType.Decimal, (object?)r.RealizadoPorId ?? DBNull.Value, ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter("p_afectado", OracleDbType.Decimal, (object?)r.FuncionarioAfectadoId ?? DBNull.Value, ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter("p_area_id", OracleDbType.Decimal, (object?)r.AreaId ?? DBNull.Value, ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter("p_categoria", OracleDbType.Varchar2, r.Categoria, ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter("p_detalle", OracleDbType.Varchar2, Recortar(r.Detalle, 1000), ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter("p_anterior", OracleDbType.Varchar2, (object?)Recortar(r.ValorAnterior, 500) ?? DBNull.Value, ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter("p_nuevo", OracleDbType.Varchar2, (object?)Recortar(r.ValorNuevo, 500) ?? DBNull.Value, ParameterDirection.Input));
        await command.ExecuteNonQueryAsync(ct);
    }

    private const string ConsultaBase = """
        SELECT nivel, puntaje_min, puntaje_max, plazo_dias
          FROM nivel_criticidad
         ORDER BY puntaje_min DESC
        """;

    private static async Task<List<PlazoCriticidadDto>> LeerAsync(OracleCommand command, CancellationToken ct)
    {
        var lista = new List<PlazoCriticidadDto>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            lista.Add(new PlazoCriticidadDto(
                reader.GetString(reader.GetOrdinal("nivel")),
                reader.GetDecimal(reader.GetOrdinal("puntaje_min")),
                reader.GetDecimal(reader.GetOrdinal("puntaje_max")),
                reader.GetInt32(reader.GetOrdinal("plazo_dias"))));
        }
        return lista;
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

    private static string? Recortar(string? texto, int maximo)
        => texto is null || texto.Length <= maximo ? texto : texto[..maximo];
}
