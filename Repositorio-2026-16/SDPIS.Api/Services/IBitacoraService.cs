using SDPIS.Api.Models.DTOs;

namespace SDPIS.Api.Services;

// HU-016: servicio interno de logueo reutilizable por todas las HUs.
public interface IBitacoraService
{
    // Registro desde codigo confiable del backend (sin chequeo de permisos).
    // funcionarioId null = accion automatica del sistema.
    Task RegistrarAsync(long denunciaId, long? funcionarioId, long? areaOrigenId,
        string tipoRegistro, string contenido, CancellationToken ct);

    // Consulta cronologica paginada, restringida segun el rol/area del funcionario.
    Task<BitacoraPaginaDto> ConsultarAsync(long denunciaId, long funcionarioId,
        string? tipoRegistro, int pagina, int tamano, CancellationToken ct);

    // Comentario manual: se guarda siempre como tipo "comentario".
    Task<BitacoraEntradaDto> AgregarComentarioAsync(long denunciaId,
        AgregarComentarioBitacoraRequest request, CancellationToken ct);

    // Panel de busqueda de Bitacora, filtrado segun el alcance de
    // visibilidad del rol de quien consulta (ver implementacion).
    Task<List<DenunciaBitacoraListadoDto>> ListarDenunciasVisiblesAsync(
        long funcionarioId, CancellationToken ct);
}
