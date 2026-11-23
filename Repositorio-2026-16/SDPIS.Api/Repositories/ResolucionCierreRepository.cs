using System.Data;
using System.Globalization;
using Oracle.ManagedDataAccess.Client;
using SDPIS.Api.Models.DTOs;

namespace SDPIS.Api.Repositories;

public class ResolucionCierreRepository(IConfiguration configuration) : IResolucionCierreRepository
{
    private static readonly CultureInfo Cultura = CultureInfo.InvariantCulture;
    private const string FormatoFecha = "yyyy-MM-dd'T'HH:mm:ss.fff";
    private readonly string _connectionString = configuration.GetConnectionString("SDPIS")
        ?? throw new InvalidOperationException("No se encontro la cadena de conexion 'SDPIS'.");

    public async Task<SolicitudResolucionInfo?> ObtenerSolicitudAsync(long solicitudId, CancellationToken ct)
    {
        await using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = """
            SELECT s.id_solicitud_cierre, s.denuncia_id, s.area_id, s.estado,
                   d.estado AS estado_denuncia
            FROM solicitud_cierre s
            JOIN denuncia d ON d.id_denuncia = s.denuncia_id
            WHERE s.id_solicitud_cierre = :p_solicitud_id
            """;
        command.BindByName = true;
        command.Parameters.Add(new OracleParameter("p_solicitud_id", OracleDbType.Decimal, solicitudId, ParameterDirection.Input));

        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
            return null;

        return new SolicitudResolucionInfo(
            reader.GetInt64(reader.GetOrdinal("id_solicitud_cierre")),
            reader.GetInt64(reader.GetOrdinal("denuncia_id")),
            reader.GetInt64(reader.GetOrdinal("area_id")),
            reader.GetString(reader.GetOrdinal("estado")),
            reader.GetString(reader.GetOrdinal("estado_denuncia")));
    }

    public async Task<string> ResolverAsync(long solicitudId, long jefeId, string decision, string? observaciones,
        CancellationToken ct)
    {
        await using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.StoredProcedure;
        command.CommandText = "sp_resolver_solicitud_cierre";
        command.BindByName = true;
        command.Parameters.Add(new OracleParameter("p_solicitud_id", OracleDbType.Decimal, solicitudId, ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter("p_jefe_id", OracleDbType.Decimal, jefeId, ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter("p_decision", OracleDbType.Varchar2, decision, ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter("p_observaciones", OracleDbType.Varchar2,
            (object?)observaciones ?? DBNull.Value, ParameterDirection.Input));
        var estadoFinal = new OracleParameter("p_estado_final", OracleDbType.Varchar2, 20, null, ParameterDirection.Output);
        command.Parameters.Add(estadoFinal);

        try
        {
            await command.ExecuteNonQueryAsync(ct);
        }
        catch (OracleException ex) when (ex.Number is >= 20000 and <= 20999)
        {
            throw ResolucionCierreErrores.Mapear(ex.Number, ex.Message, ex);
        }

        return estadoFinal.Value?.ToString()
            ?? throw new InvalidOperationException("El procedimiento no devolvio el estado final de la denuncia.");
    }

    public async Task<DecisionCierreDto?> ObtenerDecisionAsync(long solicitudId, CancellationToken ct)
    {
        await using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = """
            SELECT dc.id_decision_cierre, dc.decision, dc.decidido_por_id,
                   f.nombre AS decidido_por_nombre, dc.observaciones_jefatura, dc.fecha_decision
            FROM decision_cierre dc
            JOIN funcionario f ON f.id_funcionario = dc.decidido_por_id
            WHERE dc.solicitud_cierre_id = :p_solicitud_id
            """;
        command.BindByName = true;
        command.Parameters.Add(new OracleParameter("p_solicitud_id", OracleDbType.Decimal, solicitudId, ParameterDirection.Input));

        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? MapearDecision(reader) : null;
    }

    public async Task<List<HistorialSolicitudCierreDto>> ListarHistorialAsync(long denunciaId, CancellationToken ct)
    {
        var historial = new List<HistorialSolicitudCierreDto>();

        await using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = """
            SELECT s.id_solicitud_cierre, s.denuncia_id, d.consecutivo,
                   s.solicitado_por_id, f.nombre AS solicitado_por_nombre,
                   s.area_id, a.nombre AS area_nombre,
                   s.resolucion_final, s.observaciones_finales, s.acciones_realizadas,
                   s.fecha_solicitud, s.estado, s.estado_previo,
                   dc.id_decision_cierre, dc.decision, dc.decidido_por_id,
                   fj.nombre AS decidido_por_nombre, dc.observaciones_jefatura, dc.fecha_decision
            FROM solicitud_cierre s
            JOIN denuncia d ON d.id_denuncia = s.denuncia_id
            JOIN funcionario f ON f.id_funcionario = s.solicitado_por_id
            JOIN area a ON a.id_area = s.area_id
            LEFT JOIN decision_cierre dc ON dc.solicitud_cierre_id = s.id_solicitud_cierre
            LEFT JOIN funcionario fj ON fj.id_funcionario = dc.decidido_por_id
            WHERE s.denuncia_id = :p_denuncia_id
            ORDER BY s.fecha_solicitud DESC, s.id_solicitud_cierre DESC
            """;
        command.BindByName = true;
        command.Parameters.Add(new OracleParameter("p_denuncia_id", OracleDbType.Decimal, denunciaId, ParameterDirection.Input));

        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var solicitud = new SolicitudCierreDto(
                reader.GetInt64(reader.GetOrdinal("id_solicitud_cierre")),
                reader.GetInt64(reader.GetOrdinal("denuncia_id")),
                reader.GetString(reader.GetOrdinal("consecutivo")),
                reader.GetInt64(reader.GetOrdinal("solicitado_por_id")),
                reader.GetString(reader.GetOrdinal("solicitado_por_nombre")),
                reader.GetInt64(reader.GetOrdinal("area_id")),
                reader.GetString(reader.GetOrdinal("area_nombre")),
                reader.GetString(reader.GetOrdinal("resolucion_final")),
                reader.GetString(reader.GetOrdinal("observaciones_finales")),
                reader.GetString(reader.GetOrdinal("acciones_realizadas")),
                reader.GetDateTime(reader.GetOrdinal("fecha_solicitud")).ToString(FormatoFecha, Cultura),
                reader.GetString(reader.GetOrdinal("estado")));

            var decision = reader.IsDBNull(reader.GetOrdinal("id_decision_cierre")) ? null : MapearDecision(reader);
            historial.Add(new HistorialSolicitudCierreDto(solicitud, LeerTexto(reader, "estado_previo"), decision));
        }

        return historial;
    }

    private static DecisionCierreDto MapearDecision(OracleDataReader reader) => new(
        reader.GetInt64(reader.GetOrdinal("id_decision_cierre")),
        reader.GetString(reader.GetOrdinal("decision")),
        reader.GetInt64(reader.GetOrdinal("decidido_por_id")),
        reader.GetString(reader.GetOrdinal("decidido_por_nombre")),
        LeerTexto(reader, "observaciones_jefatura"),
        reader.GetDateTime(reader.GetOrdinal("fecha_decision")).ToString(FormatoFecha, Cultura));

    private static string? LeerTexto(OracleDataReader reader, string columna)
    {
        var ordinal = reader.GetOrdinal(columna);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }
}
