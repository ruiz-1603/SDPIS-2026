namespace SDPIS.Api.Models.DTOs;

// Resumen de un funcionario para la lista:
// disponibilidad calculada, motivo vigente (si aplica) y carga activa.
// CargaActiva proviene de la asignación de denuncias (HU-009/012); hasta
// que esa fuente exista viene null y el frontend lo muestra en blanco.
public record FuncionarioDisponibilidadResumenDto(
    long FuncionarioId,
    string Nombre,
    string? Rol,
    string AreaNombre,
    bool Disponible,
    string? MotivoVigente,
    string? FechaInicioVigente,
    string? FechaFinVigente,
    int? CargaActiva,
    bool TieneProximo
);