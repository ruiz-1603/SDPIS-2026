using System.Data;
using System.Globalization;
using Oracle.ManagedDataAccess.Client;

namespace SDPIS.Api.Repositories;

// Datos crudos para calcular permisos efectivos (HU-015).
public record FuncionarioPermisosInfo(
    long FuncionarioId,
    string? Rol,
    bool Activo,
    long AreaId,
    bool PermiteAsignar,
    IReadOnlyList<string> Delegados);

public interface IPermisosRepository
{
    // Una sola consulta: rol, estado, area, permite_asignar y delegados.
    Task<FuncionarioPermisosInfo?> ObtenerAsync(long funcionarioId, CancellationToken ct);
}

public class PermisosRepository(IConfiguration configuration) : IPermisosRepository
{
    private readonly string _connectionString = configuration.GetConnectionString("SDPIS")
        ?? throw new InvalidOperationException("No se encontro la cadena de conexion 'SDPIS'.");

    public async Task<FuncionarioPermisosInfo?> ObtenerAsync(long funcionarioId, CancellationToken ct)
    {
        await using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.BindByName = true;
        command.CommandText = """
            SELECT f.rol,
                   NVL(f.activo, 'S')          AS activo,
                   f.area_id,
                   NVL(f.permite_asignar, 'N') AS permite_asignar,
                   (SELECT LISTAGG(fp.codigo_permiso, ',') WITHIN GROUP (ORDER BY fp.codigo_permiso)
                      FROM funcionario_permiso fp
                     WHERE fp.funcionario_id = f.id_funcionario) AS delegados
              FROM funcionario f
             WHERE f.id_funcionario = :p_id
            """;
        command.Parameters.Add(new OracleParameter("p_id", OracleDbType.Decimal, funcionarioId, ParameterDirection.Input));

        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
            return null;

        var delegados = reader.IsDBNull(reader.GetOrdinal("delegados"))
            ? Array.Empty<string>()
            : reader.GetString(reader.GetOrdinal("delegados"))
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return new FuncionarioPermisosInfo(
            funcionarioId,
            reader.IsDBNull(reader.GetOrdinal("rol")) ? null : reader.GetString(reader.GetOrdinal("rol")),
            reader.GetString(reader.GetOrdinal("activo")) == "S",
            Convert.ToInt64(reader.GetValue(reader.GetOrdinal("area_id")), CultureInfo.InvariantCulture),
            reader.GetString(reader.GetOrdinal("permite_asignar")) == "S",
            delegados);
    }
}
