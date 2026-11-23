using System.Data;
using System.Globalization;
using Oracle.ManagedDataAccess.Client;
using SDPIS.Api.Models.DTOs;
using SDPIS.Api.Services.GestionUsuarios;

namespace SDPIS.Api.Repositories;

public class AsignacionEncargadoRepository(IConfiguration configuration) : IAsignacionEncargadoRepository
{
    private static readonly CultureInfo Cultura = CultureInfo.InvariantCulture;
    private readonly string _connectionString = configuration.GetConnectionString("SDPIS")
        ?? throw new InvalidOperationException("No se encontro la cadena de conexion 'SDPIS'.");

    // -20029: la denuncia tiene el cierre en revision (HU-017) -> 422 con el
    // mensaje limpio. El resto de errores de negocio se tratan como hasta ahora.
    private static Exception MapearError(OracleException ex) => ex.Number == 20029
        ? new ReglaNegocioException(ResolucionCierreErrores.Limpiar(ex.Message))
        : new DenunciaBaseDatosException(ex.Message, ex);

    public async Task<List<DenunciaParaEncargadoDto>> ObtenerDenunciasBandejaAsync(long areaId, CancellationToken ct)
    {
        var denuncias = new List<DenunciaParaEncargadoDto>();

        await using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = """
            SELECT d.id_denuncia, d.consecutivo, d.fecha_creacion,
                   d.area_actual_id, a.nombre AS nombre_area_actual,
                   NVL((
                       SELECT ec.vinculado_comercio_electronico
                       FROM evaluacion_criticidad ec
                       WHERE ec.denuncia_id = d.id_denuncia
                         AND ec.vinculado_comercio_electronico = 'S'
                         AND ROWNUM = 1
                   ), 'N') AS vinculada_ce,
                   f.nombre AS nombre_encargado
            FROM denuncia d
            JOIN area a ON a.id_area = d.area_actual_id
            LEFT JOIN funcionario f ON d.funcionario_asignado_id = f.id_funcionario
            WHERE d.area_actual_id = :p_area_id
              AND d.estado IN ('PENDIENTE_ASIGNACION', 'EN_TIEMPO')
              AND EXISTS (
                  SELECT 1 FROM evaluacion_criticidad ec
                  WHERE ec.denuncia_id = d.id_denuncia
                    AND ec.nivel_criticidad IS NOT NULL
              )
            ORDER BY d.fecha_creacion ASC
            """;
        command.BindByName = true;
        command.Parameters.Add(new OracleParameter("p_area_id", OracleDbType.Decimal, areaId, ParameterDirection.Input));

        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var esComercioElectronico = reader.GetString(reader.GetOrdinal("vinculada_ce")) == "S";
            denuncias.Add(new DenunciaParaEncargadoDto(
                reader.GetInt64(reader.GetOrdinal("id_denuncia")),
                reader.GetString(reader.GetOrdinal("consecutivo")),
                reader.GetDateTime(reader.GetOrdinal("fecha_creacion")).ToString("dd MMM. yyyy", Cultura).ToLowerInvariant(),
                reader.GetInt64(reader.GetOrdinal("area_actual_id")),
                reader.GetString(reader.GetOrdinal("nombre_area_actual")),
                esComercioElectronico,
                reader.IsDBNull(reader.GetOrdinal("nombre_encargado"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("nombre_encargado")),
                PendienteReasignacionUvc: esComercioElectronico
            ));
        }

        return denuncias;
    }

    public async Task<List<FuncionarioEncargadoDto>> ObtenerEncargadosDisponiblesAsync(long areaId, CancellationToken ct)
    {
        var encargados = new List<FuncionarioEncargadoDto>();

        await using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        // Regla HU-012: el jefe de área nunca es funcionario encargado.
        // Regla HU-007: no disponible si existe un periodo de indisponibilidad vigente.
        command.CommandText = """
            SELECT f.id_funcionario, f.nombre, f.rol, f.especialidad,
                   (SELECT COUNT(d.id_denuncia)
                    FROM denuncia d
                    WHERE d.funcionario_asignado_id = f.id_funcionario
                      AND d.estado != 'FINALIZADA') AS carga_activa
            FROM funcionario f
            WHERE (f.area_id = :p_area_id
                   OR EXISTS (SELECT 1 FROM funcionario_area_adicional fa
                               WHERE fa.funcionario_id = f.id_funcionario
                                 AND fa.area_id = :p_area_id))
              AND f.rol != 'jefe_area'
              AND f.activo = 'S'
              AND NOT EXISTS (
                  SELECT 1
                  FROM periodo_indisponibilidad pi
                  WHERE pi.funcionario_id = f.id_funcionario
                    AND pi.fecha_inicio <= TRUNC(SYSDATE)
                    AND (pi.fecha_fin_estimada IS NULL OR pi.fecha_fin_estimada >= TRUNC(SYSDATE))
              )
            ORDER BY carga_activa ASC, f.id_funcionario ASC
            """;
        command.BindByName = true;
        command.Parameters.Add(new OracleParameter("p_area_id", OracleDbType.Decimal, areaId, ParameterDirection.Input));

        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            encargados.Add(new FuncionarioEncargadoDto(
                reader.GetInt64(reader.GetOrdinal("id_funcionario")),
                reader.GetString(reader.GetOrdinal("nombre")),
                reader.GetString(reader.GetOrdinal("rol")),
                Convert.ToInt32(reader.GetDecimal(reader.GetOrdinal("carga_activa"))),
                reader.IsDBNull(reader.GetOrdinal("especialidad"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("especialidad"))
            ));
        }

        return encargados;
    }

    public async Task<List<AreaReasignacionDto>> ObtenerAreasDestinoAsync(CancellationToken ct)
    {
        var areas = new List<AreaReasignacionDto>();

        await using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = """
            SELECT id_area, nombre, tipo
            FROM area
            ORDER BY tipo, nombre
            """;

        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            areas.Add(new AreaReasignacionDto(
                reader.GetInt64(reader.GetOrdinal("id_area")),
                reader.GetString(reader.GetOrdinal("nombre")),
                reader.GetString(reader.GetOrdinal("tipo"))
            ));
        }

        return areas;
    }

    public async Task<List<HistorialAsignacionDto>> ObtenerHistorialAsync(long denunciaId, CancellationToken ct)
    {
        var historial = new List<HistorialAsignacionDto>();

        await using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = """
            SELECT id_historial, denuncia_id, area_anterior_id, area_nueva_id,
                   funcionario_anterior_id, funcionario_nuevo_id, fecha, motivo, numero_oficio
            FROM historial_asignacion
            WHERE denuncia_id = :p_denuncia_id
            ORDER BY fecha DESC, id_historial DESC
            """;
        command.BindByName = true;
        command.Parameters.Add(new OracleParameter("p_denuncia_id", OracleDbType.Decimal, denunciaId, ParameterDirection.Input));

        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            historial.Add(new HistorialAsignacionDto(
                reader.GetInt64(reader.GetOrdinal("id_historial")),
                reader.GetInt64(reader.GetOrdinal("denuncia_id")),
                reader.IsDBNull(reader.GetOrdinal("area_anterior_id"))
                    ? null
                    : reader.GetInt64(reader.GetOrdinal("area_anterior_id")),
                reader.GetInt64(reader.GetOrdinal("area_nueva_id")),
                reader.IsDBNull(reader.GetOrdinal("funcionario_anterior_id"))
                    ? null
                    : reader.GetInt64(reader.GetOrdinal("funcionario_anterior_id")),
                reader.IsDBNull(reader.GetOrdinal("funcionario_nuevo_id"))
                    ? null
                    : reader.GetInt64(reader.GetOrdinal("funcionario_nuevo_id")),
                reader.GetDateTime(reader.GetOrdinal("fecha")).ToString("dd MMM. yyyy HH:mm", Cultura).ToLowerInvariant(),
                reader.IsDBNull(reader.GetOrdinal("motivo"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("motivo")),
                reader.IsDBNull(reader.GetOrdinal("numero_oficio"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("numero_oficio"))
            ));
        }

        return historial;
    }

    public async Task AsignarEncargadoAsync(long denunciaId, long encargadoId, long jefeAreaId, string? motivo, CancellationToken ct)
    {
        await using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.StoredProcedure;
        command.CommandText = "sp_asignar_funcionario_encargado";
        command.BindByName = true;
        command.Parameters.Add(new OracleParameter("p_denuncia_id", OracleDbType.Decimal, denunciaId, ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter("p_encargado_id", OracleDbType.Decimal, encargadoId, ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter("p_realizado_por", OracleDbType.Decimal, jefeAreaId, ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter("p_motivo", OracleDbType.Varchar2, motivo ?? (object)DBNull.Value, ParameterDirection.Input));

        try
        {
            await command.ExecuteNonQueryAsync(ct);
        }
        catch (OracleException ex) when (ex.Number is >= 20000 and <= 20999)
        {
            throw MapearError(ex);
        }
    }

    public async Task ReasignarAreaAsync(long denunciaId, long areaNuevaId, long jefeAreaId, string? motivo, CancellationToken ct)
    {
        await using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.StoredProcedure;
        command.CommandText = "sp_reasignar_denuncia_area";
        command.BindByName = true;
        command.Parameters.Add(new OracleParameter("p_denuncia_id", OracleDbType.Decimal, denunciaId, ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter("p_area_nueva_id", OracleDbType.Decimal, areaNuevaId, ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter("p_realizado_por", OracleDbType.Decimal, jefeAreaId, ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter("p_motivo", OracleDbType.Varchar2, motivo ?? (object)DBNull.Value, ParameterDirection.Input));

        try
        {
            await command.ExecuteNonQueryAsync(ct);
        }
        catch (OracleException ex) when (ex.Number is >= 20000 and <= 20999)
        {
            throw MapearError(ex);
        }
    }

    public async Task ReasignarAUvcAsync(long denunciaId, long jefeAreaId, string motivo, string? numeroOficio, CancellationToken ct)
    {
        await using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.StoredProcedure;
        command.CommandText = "sp_reasignar_denuncia_area";
        command.BindByName = true;
        command.Parameters.Add(new OracleParameter("p_denuncia_id", OracleDbType.Decimal, denunciaId, ParameterDirection.Input));
        // Area NULL => el SP resuelve el área UVC automáticamente (AC #5).
        command.Parameters.Add(new OracleParameter("p_area_nueva_id", OracleDbType.Decimal, DBNull.Value, ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter("p_realizado_por", OracleDbType.Decimal, jefeAreaId, ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter("p_motivo", OracleDbType.Varchar2, motivo, ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter("p_numero_oficio", OracleDbType.Varchar2, numeroOficio ?? (object)DBNull.Value, ParameterDirection.Input));

        try
        {
            await command.ExecuteNonQueryAsync(ct);
        }
        catch (OracleException ex) when (ex.Number is >= 20000 and <= 20999)
        {
            throw MapearError(ex);
        }
    }

    public async Task<int> GenerarAlertasVencimientoAsync(long areaId, CancellationToken ct)
    {
        await using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.StoredProcedure;
        command.CommandText = "sp_generar_alerta_vencimiento_asignacion";
        command.BindByName = true;
        command.Parameters.Add(new OracleParameter("p_area_id", OracleDbType.Decimal, areaId, ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter("p_generadas", OracleDbType.Decimal, ParameterDirection.Output));

        try
        {
            await command.ExecuteNonQueryAsync(ct);
            var valor = command.Parameters["p_generadas"].Value;
            // OracleDecimal no implementa IConvertible; se convierte via ToString().
            return valor is DBNull || valor == null
                ? 0
                : Convert.ToInt32(valor.ToString(), System.Globalization.CultureInfo.InvariantCulture);
        }
        catch (OracleException ex) when (ex.Number is >= 20000 and <= 20999)
        {
            throw MapearError(ex);
        }
    }

    public async Task ConfirmarAtencionUvcAsync(long denunciaId, long superusuarioId, string? motivo, CancellationToken ct)
    {
        await using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.StoredProcedure;
        command.CommandText = "sp_confirmar_atencion_uvc";
        command.BindByName = true;
        command.Parameters.Add(new OracleParameter("p_denuncia_id", OracleDbType.Decimal, denunciaId, ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter("p_superusuario_id", OracleDbType.Decimal, superusuarioId, ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter("p_motivo", OracleDbType.Varchar2, motivo ?? (object)DBNull.Value, ParameterDirection.Input));

        try
        {
            await command.ExecuteNonQueryAsync(ct);
        }
        catch (OracleException ex) when (ex.Number is >= 20000 and <= 20999)
        {
            throw MapearError(ex);
        }
    }

    public async Task DevolverAreaRectoraAsync(long denunciaId, long areaRectoraId, long superusuarioId, string? motivo, CancellationToken ct)
    {
        await using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.StoredProcedure;
        command.CommandText = "sp_devolver_area_rectora";
        command.BindByName = true;
        command.Parameters.Add(new OracleParameter("p_denuncia_id", OracleDbType.Decimal, denunciaId, ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter("p_area_rectora_id", OracleDbType.Decimal, areaRectoraId, ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter("p_superusuario_id", OracleDbType.Decimal, superusuarioId, ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter("p_motivo", OracleDbType.Varchar2, motivo ?? (object)DBNull.Value, ParameterDirection.Input));

        try
        {
            await command.ExecuteNonQueryAsync(ct);
        }
        catch (OracleException ex) when (ex.Number is >= 20000 and <= 20999)
        {
            throw MapearError(ex);
        }
    }
}