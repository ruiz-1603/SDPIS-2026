using System.Data;
using System.Globalization;
using Oracle.ManagedDataAccess.Client;
using SDPIS.Api.Models.DTOs;

namespace SDPIS.Api.Repositories;

public class SolicitudCierreRepository(IConfiguration configuration) : ISolicitudCierreRepository
{
    private static readonly CultureInfo Cultura = CultureInfo.InvariantCulture;
    private readonly string _connectionString = configuration.GetConnectionString("SDPIS")
        ?? throw new InvalidOperationException("No se encontro la cadena de conexion 'SDPIS'.");

    private const string SelectSolicitud = """
        SELECT s.id_solicitud_cierre, s.denuncia_id, d.consecutivo,
               s.solicitado_por_id, f.nombre AS solicitado_por_nombre,
               s.area_id, a.nombre AS area_nombre,
               s.resolucion_final, s.observaciones_finales, s.acciones_realizadas,
               s.fecha_solicitud, s.estado
        FROM solicitud_cierre s
        JOIN denuncia d ON d.id_denuncia = s.denuncia_id
        JOIN funcionario f ON f.id_funcionario = s.solicitado_por_id
        JOIN area a ON a.id_area = s.area_id
        """;

    public async Task<DenunciaCierreInfo?> ObtenerDenunciaAsync(long denunciaId, CancellationToken ct)
    {
        await using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = """
            SELECT id_denuncia, estado, area_actual_id, funcionario_asignado_id,
                   carpeta_confidencial_url, carpeta_no_confidencial_url
            FROM denuncia
            WHERE id_denuncia = :p_denuncia_id
            """;
        command.BindByName = true;
        command.Parameters.Add(new OracleParameter("p_denuncia_id", OracleDbType.Decimal, denunciaId, ParameterDirection.Input));

        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
            return null;

        var asignado = reader.GetOrdinal("funcionario_asignado_id");
        var confidencial = reader.GetOrdinal("carpeta_confidencial_url");
        var noConfidencial = reader.GetOrdinal("carpeta_no_confidencial_url");
        return new DenunciaCierreInfo(
            reader.GetInt64(reader.GetOrdinal("id_denuncia")),
            reader.GetString(reader.GetOrdinal("estado")),
            reader.GetInt64(reader.GetOrdinal("area_actual_id")),
            reader.IsDBNull(asignado) ? null : reader.GetInt64(asignado),
            reader.IsDBNull(confidencial) ? null : reader.GetString(confidencial),
            reader.IsDBNull(noConfidencial) ? null : reader.GetString(noConfidencial));
    }

    public async Task<long> SolicitarAsync(long denunciaId, long funcionarioId,
        SolicitarCierreRequest request, CancellationToken ct)
    {
        await using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.StoredProcedure;
        command.CommandText = "sp_solicitar_cierre_denuncia";
        command.BindByName = true;
        command.Parameters.Add(new OracleParameter("p_denuncia_id", OracleDbType.Decimal, denunciaId, ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter("p_funcionario_id", OracleDbType.Decimal, funcionarioId, ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter("p_resolucion_final", OracleDbType.Varchar2, request.ResolucionFinal, ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter("p_observaciones_finales", OracleDbType.Varchar2, request.ObservacionesFinales, ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter("p_acciones_realizadas", OracleDbType.Varchar2, request.AccionesRealizadas, ParameterDirection.Input));
        var idSolicitud = new OracleParameter("p_id_solicitud", OracleDbType.Decimal, ParameterDirection.Output);
        command.Parameters.Add(idSolicitud);

        try
        {
            await command.ExecuteNonQueryAsync(ct);
        }
        catch (OracleException ex) when (ex.Number is >= 20000 and <= 20999)
        {
            throw new DenunciaBaseDatosException(ex.Message, ex);
        }
        // ORA-00001 sobre UQ_SOLICITUD_CIERRE_PENDIENTE: otra solicitud de la
        // misma denuncia gano la carrera.
        catch (OracleException ex) when (ex.Number == 1)
        {
            throw new DenunciaBaseDatosException(
                "La denuncia ya tiene una solicitud de cierre pendiente de revisión por la jefatura.", ex);
        }

        return Convert.ToInt64(idSolicitud.Value.ToString(), Cultura);
    }

    public async Task<SolicitudCierreDto?> ObtenerAsync(long solicitudId, CancellationToken ct)
    {
        await using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = SelectSolicitud + """

            WHERE s.id_solicitud_cierre = :p_solicitud_id
            """;
        command.BindByName = true;
        command.Parameters.Add(new OracleParameter("p_solicitud_id", OracleDbType.Decimal, solicitudId, ParameterDirection.Input));

        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Mapear(reader) : null;
    }

    public async Task<SolicitudCierreDto?> ObtenerPendienteAsync(long denunciaId, CancellationToken ct)
    {
        await using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = SelectSolicitud + """

            WHERE s.denuncia_id = :p_denuncia_id
              AND s.estado = 'PENDIENTE'
            """;
        command.BindByName = true;
        command.Parameters.Add(new OracleParameter("p_denuncia_id", OracleDbType.Decimal, denunciaId, ParameterDirection.Input));

        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Mapear(reader) : null;
    }

    public async Task<List<SolicitudCierreDto>> ListarPendientesAsync(long? areaId, CancellationToken ct)
    {
        var solicitudes = new List<SolicitudCierreDto>();

        await using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = SelectSolicitud + """

            WHERE s.estado = 'PENDIENTE'
              AND (:p_area_id IS NULL OR s.area_id = :p_area_id)
            ORDER BY s.fecha_solicitud ASC, s.id_solicitud_cierre ASC
            """;
        command.BindByName = true;
        command.Parameters.Add(new OracleParameter("p_area_id", OracleDbType.Decimal, (object?)areaId ?? DBNull.Value, ParameterDirection.Input));

        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            solicitudes.Add(Mapear(reader));

        return solicitudes;
    }

    private static SolicitudCierreDto Mapear(OracleDataReader reader) => new(
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
        reader.GetDateTime(reader.GetOrdinal("fecha_solicitud")).ToString("yyyy-MM-dd'T'HH:mm:ss.fff", Cultura),
        reader.GetString(reader.GetOrdinal("estado")));
}
