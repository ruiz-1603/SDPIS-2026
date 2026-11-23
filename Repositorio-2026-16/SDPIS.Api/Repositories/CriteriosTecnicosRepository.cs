using System.Data;
using Oracle.ManagedDataAccess.Client;
using SDPIS.Api.Models.DTOs;

namespace SDPIS.Api.Repositories;

public class CriteriosTecnicosRepository(IConfiguration configuration) : ICriteriosTecnicosRepository
{
    private readonly string _connectionString = configuration.GetConnectionString("SDPIS")
        ?? throw new InvalidOperationException("No se encontro la cadena de conexion 'SDPIS'.");

    public async Task<CriteriosTecnicosResultado> RegistrarAsync(
        long denunciaId, RegistrarCriteriosTecnicosRequest request, CancellationToken ct)
    {
        await using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.StoredProcedure;
        command.CommandText = "sp_registrar_criterios_tecnicos";
        command.BindByName = true;

        command.Parameters.Add(new OracleParameter("p_denuncia_id", OracleDbType.Decimal, denunciaId, ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter("p_funcionario_validador_id", OracleDbType.Decimal, request.FuncionarioValidadorId, ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter("p_vinculado_comercio_electronico", OracleDbType.Char, request.VinculadoComercioElectronico, ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter("p_dano_potencial_salud", OracleDbType.Varchar2, request.DanoPotencialSalud, ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter("p_requiere_criterio_especializado", OracleDbType.Varchar2, request.RequiereCriterioEspecializado, ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter("p_observaciones_tecnicas", OracleDbType.Varchar2, request.ObservacionesTecnicas, ParameterDirection.Input));
        var pIdEvaluacion = new OracleParameter("p_id_evaluacion", OracleDbType.Decimal, ParameterDirection.Output);
        var pPuntajeTotal = new OracleParameter("p_puntaje_total", OracleDbType.Decimal, ParameterDirection.Output);
        command.Parameters.Add(pIdEvaluacion);
        command.Parameters.Add(pPuntajeTotal);

        try
        {
            await command.ExecuteNonQueryAsync(ct);
        }
        catch (OracleException ex) when (ex.Number is >= 20000 and <= 20999)
        {
            throw new DenunciaBaseDatosException(ex.Message, ex);
        }

        return LeerResultado(pIdEvaluacion, pPuntajeTotal);
    }

    public async Task<CriteriosTecnicosResultado> ActualizarCriterioAsync(
        long denunciaId, string criterioClave, ActualizarCriterioTecnicoRequest request, CancellationToken ct)
    {
        await using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.StoredProcedure;
        command.CommandText = "sp_actualizar_criterio_tecnico";
        command.BindByName = true;

        command.Parameters.Add(new OracleParameter("p_denuncia_id", OracleDbType.Decimal, denunciaId, ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter("p_funcionario_id", OracleDbType.Decimal, request.FuncionarioId, ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter("p_criterio_clave", OracleDbType.Varchar2, criterioClave, ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter("p_valor_seleccionado", OracleDbType.Varchar2, request.ValorSeleccionado, ParameterDirection.Input));
        // NULL cuando el criterio no tenia valor previo ("Agregar"): el SP solo
        // exige justificacion cuando si habia un valor anterior que se reemplaza.
        command.Parameters.Add(new OracleParameter("p_justificacion", OracleDbType.Varchar2, (object?)request.Justificacion ?? DBNull.Value, ParameterDirection.Input));
        var pIdEvaluacion = new OracleParameter("p_id_evaluacion", OracleDbType.Decimal, ParameterDirection.Output);
        var pPuntajeTotal = new OracleParameter("p_puntaje_total", OracleDbType.Decimal, ParameterDirection.Output);
        command.Parameters.Add(pIdEvaluacion);
        command.Parameters.Add(pPuntajeTotal);

        try
        {
            await command.ExecuteNonQueryAsync(ct);
        }
        catch (OracleException ex) when (ex.Number is >= 20000 and <= 20999)
        {
            throw new DenunciaBaseDatosException(ex.Message, ex);
        }

        return LeerResultado(pIdEvaluacion, pPuntajeTotal);
    }

    public async Task<long> ActualizarMetadataAsync(
        long denunciaId, ActualizarMetadataCriteriosTecnicosRequest request, CancellationToken ct)
    {
        await using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.StoredProcedure;
        command.CommandText = "sp_actualizar_metadata_criterios_tecnicos";
        command.BindByName = true;

        command.Parameters.Add(new OracleParameter("p_denuncia_id", OracleDbType.Decimal, denunciaId, ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter("p_funcionario_id", OracleDbType.Decimal, request.FuncionarioId, ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter("p_vinculado_comercio_electronico", OracleDbType.Char, request.VinculadoComercioElectronico, ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter("p_observaciones_tecnicas", OracleDbType.Varchar2, request.ObservacionesTecnicas, ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter("p_justificacion", OracleDbType.Varchar2, request.Justificacion, ParameterDirection.Input));
        var pIdEvaluacion = new OracleParameter("p_id_evaluacion", OracleDbType.Decimal, ParameterDirection.Output);
        command.Parameters.Add(pIdEvaluacion);

        try
        {
            await command.ExecuteNonQueryAsync(ct);
        }
        catch (OracleException ex) when (ex.Number is >= 20000 and <= 20999)
        {
            throw new DenunciaBaseDatosException(ex.Message, ex);
        }

        return long.Parse(pIdEvaluacion.Value!.ToString()!, System.Globalization.CultureInfo.InvariantCulture);
    }

    public async Task<CriteriosTecnicosDto?> ObtenerAsync(long denunciaId, CancellationToken ct)
    {
        await using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = """
            SELECT e.id_evaluacion, e.denuncia_id, e.funcionario_validador_id, e.fecha_evaluacion,
                   e.puntaje_total, e.vinculado_comercio_electronico, e.observaciones_tecnicas,
                   e.nivel_criticidad, e.fecha_limite,
                   rd.valor_seleccionado AS dano_potencial_salud,
                   re.valor_seleccionado AS requiere_criterio_especializado
            FROM evaluacion_criticidad e
            LEFT JOIN respuesta_criterio rd
                   ON rd.id_evaluacion = e.id_evaluacion
                  AND rd.criterio_id = (SELECT id_criterio FROM criterio WHERE nombre = 'Dano potencial a la salud')
            LEFT JOIN respuesta_criterio re
                   ON re.id_evaluacion = e.id_evaluacion
                  AND re.criterio_id = (SELECT id_criterio FROM criterio WHERE nombre = 'Criterio tecnico especializado UVC')
            WHERE e.denuncia_id = :p_denuncia_id
              AND e.id_evaluacion = (
                    SELECT MAX(e2.id_evaluacion) FROM evaluacion_criticidad e2 WHERE e2.denuncia_id = e.denuncia_id
              )
            """;
        command.BindByName = true;
        command.Parameters.Add(new OracleParameter("p_denuncia_id", OracleDbType.Decimal, denunciaId, ParameterDirection.Input));

        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
            return null;

        return new CriteriosTecnicosDto(
            reader.GetInt64(reader.GetOrdinal("id_evaluacion")),
            reader.GetInt64(reader.GetOrdinal("denuncia_id")),
            reader.GetInt64(reader.GetOrdinal("funcionario_validador_id")),
            reader.GetDateTime(reader.GetOrdinal("fecha_evaluacion")),
            reader.IsDBNull(reader.GetOrdinal("puntaje_total")) ? null : reader.GetDecimal(reader.GetOrdinal("puntaje_total")),
            reader.GetString(reader.GetOrdinal("vinculado_comercio_electronico")),
            reader.IsDBNull(reader.GetOrdinal("dano_potencial_salud")) ? null : reader.GetString(reader.GetOrdinal("dano_potencial_salud")),
            reader.IsDBNull(reader.GetOrdinal("requiere_criterio_especializado")) ? null : reader.GetString(reader.GetOrdinal("requiere_criterio_especializado")),
            reader.IsDBNull(reader.GetOrdinal("observaciones_tecnicas")) ? null : reader.GetString(reader.GetOrdinal("observaciones_tecnicas")),
            reader.IsDBNull(reader.GetOrdinal("nivel_criticidad")) ? null : reader.GetString(reader.GetOrdinal("nivel_criticidad")),
            reader.IsDBNull(reader.GetOrdinal("fecha_limite")) ? null : reader.GetDateTime(reader.GetOrdinal("fecha_limite"))
        );
    }

    public async Task<AlertaSanitariaVigenteResponse> ObtenerAlertaSanitariaVigenteAsync(long denunciaId, CancellationToken ct)
    {
        var numerosAlerta = new List<string>();

        await using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = """
            SELECT DISTINCT a.numero_alerta
            FROM detalle_producto dp
            JOIN alerta_sanitaria_producto asp ON asp.producto_id = dp.producto_id AND asp.activa = 'S'
            JOIN alerta_sanitaria a ON a.id_alerta = asp.alerta_id
            WHERE dp.denuncia_id = :p_denuncia_id
            """;
        command.BindByName = true;
        command.Parameters.Add(new OracleParameter("p_denuncia_id", OracleDbType.Decimal, denunciaId, ParameterDirection.Input));

        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var ordinal = reader.GetOrdinal("numero_alerta");
            if (!reader.IsDBNull(ordinal))
                numerosAlerta.Add(reader.GetString(ordinal));
        }

        return new AlertaSanitariaVigenteResponse(numerosAlerta.Count > 0, numerosAlerta);
    }

    // Tres lecturas sobre la misma conexion (encabezado, productos, motivos)
    // en vez de un solo JOIN: una denuncia tiene N productos y cada producto
    // N motivos, y aplanarlo todo duplicaria la descripcion del hecho
    // (hasta 4000 caracteres) por cada combinacion producto/motivo.
    public async Task<DetalleDenunciaDto?> ObtenerDetalleDenunciaAsync(long denunciaId, CancellationToken ct)
    {
        await using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync(ct);

        long idDenuncia;
        string consecutivo;
        DateTime fechaCreacion;
        string estado;
        DetalleDenunciaHechoDto? hecho = null;
        DetalleDenunciaUbicacionDto? ubicacion = null;

        await using (var command = connection.CreateCommand())
        {
            command.CommandType = CommandType.Text;
            command.CommandText = """
                SELECT d.id_denuncia, d.consecutivo, d.fecha_creacion, d.estado,
                       h.denuncia_id AS hecho_denuncia_id,
                       h.nombre_establecimiento, h.descripcion_hecho,
                       h.denunciado_otra_institucion, h.detalle_otra_institucion,
                       h.denunciado_previamente_minsalud, h.detalle_previo_minsalud,
                       u.denuncia_id AS ubicacion_denuncia_id,
                       p.nombre AS provincia, c.nombre AS canton, di.nombre AS distrito,
                       u.direccion_exacta
                FROM denuncia d
                LEFT JOIN denuncia_hecho h ON h.denuncia_id = d.id_denuncia
                LEFT JOIN denuncia_ubicacion u ON u.denuncia_id = d.id_denuncia
                LEFT JOIN canton c ON c.id_canton = u.canton_id
                LEFT JOIN provincia p ON p.id_provincia = c.provincia_id
                LEFT JOIN distrito di ON di.id_distrito = u.distrito_id
                WHERE d.id_denuncia = :p_denuncia_id
                """;
            command.BindByName = true;
            command.Parameters.Add(new OracleParameter("p_denuncia_id", OracleDbType.Decimal, denunciaId, ParameterDirection.Input));

            await using var reader = await command.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct))
                return null;

            idDenuncia = Convert.ToInt64(reader["id_denuncia"], System.Globalization.CultureInfo.InvariantCulture);
            consecutivo = LeerTexto(reader, "consecutivo") ?? string.Empty;
            fechaCreacion = reader.GetDateTime(reader.GetOrdinal("fecha_creacion"));
            estado = LeerTexto(reader, "estado") ?? string.Empty;

            if (!reader.IsDBNull(reader.GetOrdinal("hecho_denuncia_id")))
            {
                hecho = new DetalleDenunciaHechoDto(
                    LeerTexto(reader, "nombre_establecimiento") ?? string.Empty,
                    LeerTexto(reader, "descripcion_hecho") ?? string.Empty,
                    LeerTexto(reader, "denunciado_otra_institucion"),
                    LeerTexto(reader, "detalle_otra_institucion"),
                    LeerTexto(reader, "denunciado_previamente_minsalud"),
                    LeerTexto(reader, "detalle_previo_minsalud"));
            }

            if (!reader.IsDBNull(reader.GetOrdinal("ubicacion_denuncia_id")))
            {
                ubicacion = new DetalleDenunciaUbicacionDto(
                    LeerTexto(reader, "provincia") ?? string.Empty,
                    LeerTexto(reader, "canton") ?? string.Empty,
                    LeerTexto(reader, "distrito"),
                    LeerTexto(reader, "direccion_exacta"));
            }
        }

        var motivosPorProducto = new Dictionary<long, List<string>>();
        await using (var command = connection.CreateCommand())
        {
            command.CommandType = CommandType.Text;
            command.CommandText = """
                SELECT dpm.id_detalle_producto, m.nombre
                FROM detalle_producto dp
                JOIN detalle_producto_motivo dpm ON dpm.id_detalle_producto = dp.id_detalle_producto
                JOIN motivo_denuncia m ON m.id_motivo = dpm.id_motivo
                WHERE dp.denuncia_id = :p_denuncia_id
                ORDER BY dpm.id_detalle_producto, m.nombre
                """;
            command.BindByName = true;
            command.Parameters.Add(new OracleParameter("p_denuncia_id", OracleDbType.Decimal, denunciaId, ParameterDirection.Input));

            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var idDetalle = Convert.ToInt64(reader["id_detalle_producto"], System.Globalization.CultureInfo.InvariantCulture);
                if (!motivosPorProducto.TryGetValue(idDetalle, out var motivos))
                {
                    motivos = [];
                    motivosPorProducto[idDetalle] = motivos;
                }
                motivos.Add(LeerTexto(reader, "nombre") ?? string.Empty);
            }
        }

        var productos = new List<DetalleDenunciaProductoDto>();
        await using (var command = connection.CreateCommand())
        {
            command.CommandType = CommandType.Text;
            command.CommandText = """
                SELECT dp.id_detalle_producto, dp.nombre_producto_texto, tp.nombre AS tipo_producto,
                       dp.descripcion_producto, dp.registro_sanitario_ingresado, dp.marca_ingresada,
                       dpc.numero_lote, dpc.fabricante_ingresado, dpc.pais_origen,
                       dpc.presentacion, dpc.fecha_compra
                FROM detalle_producto dp
                LEFT JOIN tipo_producto tp ON tp.id_tipo_producto = dp.tipo_producto_id
                LEFT JOIN detalle_producto_compra dpc ON dpc.id_detalle_producto = dp.id_detalle_producto
                WHERE dp.denuncia_id = :p_denuncia_id
                ORDER BY dp.id_detalle_producto
                """;
            command.BindByName = true;
            command.Parameters.Add(new OracleParameter("p_denuncia_id", OracleDbType.Decimal, denunciaId, ParameterDirection.Input));

            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var idDetalle = Convert.ToInt64(reader["id_detalle_producto"], System.Globalization.CultureInfo.InvariantCulture);
                var ordinalFechaCompra = reader.GetOrdinal("fecha_compra");

                productos.Add(new DetalleDenunciaProductoDto(
                    idDetalle,
                    LeerTexto(reader, "nombre_producto_texto") ?? string.Empty,
                    LeerTexto(reader, "tipo_producto"),
                    LeerTexto(reader, "descripcion_producto") ?? string.Empty,
                    LeerTexto(reader, "registro_sanitario_ingresado"),
                    LeerTexto(reader, "marca_ingresada"),
                    LeerTexto(reader, "numero_lote"),
                    LeerTexto(reader, "fabricante_ingresado"),
                    LeerTexto(reader, "pais_origen"),
                    LeerTexto(reader, "presentacion"),
                    reader.IsDBNull(ordinalFechaCompra) ? null : reader.GetDateTime(ordinalFechaCompra),
                    motivosPorProducto.GetValueOrDefault(idDetalle) ?? []));
            }
        }

        return new DetalleDenunciaDto(idDenuncia, consecutivo, fechaCreacion, estado, hecho, ubicacion, productos);
    }

    private static string? LeerTexto(System.Data.Common.DbDataReader reader, string columna)
    {
        var ordinal = reader.GetOrdinal(columna);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static CriteriosTecnicosResultado LeerResultado(OracleParameter pIdEvaluacion, OracleParameter pPuntajeTotal)
    {
        var idEvaluacion = long.Parse(pIdEvaluacion.Value!.ToString()!, System.Globalization.CultureInfo.InvariantCulture);
        var puntajeTotal = decimal.Parse(pPuntajeTotal.Value!.ToString()!, System.Globalization.CultureInfo.InvariantCulture);
        return new CriteriosTecnicosResultado(idEvaluacion, puntajeTotal);
    }
}