using System.Data;
using Oracle.ManagedDataAccess.Client;
using SDPIS.Api.Services.Criticidad;

namespace SDPIS.Api.Repositories;

public class EvaluacionCriticidadRepository(IConfiguration configuration) : IEvaluacionCriticidadRepository
{
    private readonly string _connectionString = configuration.GetConnectionString("SDPIS")
        ?? throw new InvalidOperationException("No se encontro la cadena de conexion 'SDPIS'.");

    public async Task<IReadOnlyList<RangoCriticidad>> ObtenerRangosAsync(CancellationToken ct)
    {
        var rangos = new List<RangoCriticidad>();

        await using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = """
            SELECT nivel, puntaje_min, puntaje_max, plazo_dias
            FROM nivel_criticidad
            ORDER BY puntaje_min
            """;

        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            rangos.Add(new RangoCriticidad(
                reader.GetString(reader.GetOrdinal("nivel")),
                reader.GetDecimal(reader.GetOrdinal("puntaje_min")),
                reader.GetDecimal(reader.GetOrdinal("puntaje_max")),
                reader.GetInt32(reader.GetOrdinal("plazo_dias"))));
        }

        return rangos;
    }

    public async Task RegistrarEvaluacionAsync(long denunciaId, ResultadoCalculoCriticidad resultado, CancellationToken ct)
    {
        await using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.StoredProcedure;
        command.CommandText = "sp_registrar_evaluacion_criticidad";
        command.BindByName = true;

        command.Parameters.Add(new OracleParameter("p_denuncia_id", OracleDbType.Decimal, denunciaId, ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter("p_nivel_criticidad", OracleDbType.Varchar2, resultado.Nivel, ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter("p_plazo_dias", OracleDbType.Decimal, resultado.PlazoDias, ParameterDirection.Input));

        try
        {
            await command.ExecuteNonQueryAsync(ct);
        }
        catch (OracleException ex) when (ex.Number is >= 20000 and <= 20999)
        {
            throw new DenunciaBaseDatosException(ex.Message, ex);
        }
    }

    public Task LimpiarEvaluacionAsync(long denunciaId, CancellationToken ct)
        => EjecutarSpConDenunciaIdAsync("sp_limpiar_evaluacion_criticidad", denunciaId, ct);

    public Task RegistrarNotificacionUvcAsync(long denunciaId, CancellationToken ct)
        => EjecutarSpConDenunciaIdAsync("sp_notificar_uvc_criticidad_alta", denunciaId, ct);

    public Task RegistrarPendienteReasignacionUvcAsync(long denunciaId, CancellationToken ct)
        => EjecutarSpConDenunciaIdAsync("sp_marcar_pendiente_reasignacion_uvc", denunciaId, ct);

    private async Task EjecutarSpConDenunciaIdAsync(string nombreSp, long denunciaId, CancellationToken ct)
    {
        await using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.StoredProcedure;
        command.CommandText = nombreSp;
        command.BindByName = true;
        command.Parameters.Add(new OracleParameter("p_denuncia_id", OracleDbType.Decimal, denunciaId, ParameterDirection.Input));

        try
        {
            await command.ExecuteNonQueryAsync(ct);
        }
        catch (OracleException ex) when (ex.Number is >= 20000 and <= 20999)
        {
            throw new DenunciaBaseDatosException(ex.Message, ex);
        }
    }
}
