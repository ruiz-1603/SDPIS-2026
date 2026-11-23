namespace SDPIS.Api.Repositories;

// HU-016: tipos de registro de la bitacora (CHECK de bitacora.tipo_registro en
// 01_esquema.sql). Agrupan las acciones para el filtro de la pantalla; los
// procedimientos de 02_procedimientos.sql usan los mismos valores.
public static class TiposBitacora
{
    public const string RegistroDenuncia = "registro_denuncia";
    public const string Asignacion = "asignacion";
    public const string Traslado = "traslado";
    public const string EvaluacionTecnica = "evaluacion_tecnica";
    public const string Criticidad = "criticidad";
    public const string Notificacion = "notificacion";
    public const string Comentario = "comentario";
    public const string SolicitudCierre = "solicitud_cierre";
    // Reservado para los archivos adjuntos (aun no se registran).
    public const string Adjunto = "adjunto";

    public static readonly IReadOnlySet<string> Todos = new HashSet<string>(StringComparer.Ordinal)
    {
        RegistroDenuncia, Asignacion, Traslado, EvaluacionTecnica, Criticidad,
        Notificacion, Comentario, SolicitudCierre, Adjunto
    };
}

// HU-016: datos para escribir una entrada via sp_insertar_bitacora.
public record RegistroBitacora(
    long DenunciaId,
    long? FuncionarioId,
    long? AreaOrigenId,
    string TipoRegistro,
    string Contenido
);

// Datos del funcionario necesarios para decidir el acceso a la bitacora.
public record FuncionarioAccesoInfo(long IdFuncionario, string? Rol, long AreaId, bool Activo);

// Datos de la denuncia necesarios para decidir el acceso a la bitacora.
// CierrePendiente (HU-018): tiene una solicitud de cierre esperando a la jefatura.
public record DenunciaAccesoInfo(long IdDenuncia, long? AreaActualId, long? FuncionarioAsignadoId,
    bool CierrePendiente = false);
