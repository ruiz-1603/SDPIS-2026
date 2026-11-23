namespace SDPIS.Api.Models.DTOs;

// Detalle de disponibilidad de un funcionario.
// No existe campo manual S/N: Disponible se deriva de comparar la
// fecha actual contra los periodos de indisponibilidad registrados.
// Incluye el historial completo de periodos con su estado calculado.
public record DisponibilidadFuncionarioDto(
    long FuncionarioId,
    string Nombre,
    string? Rol,
    string AreaNombre,
    bool Disponible,
    List<PeriodoIndisponibilidadHistorialDto> Historial
);