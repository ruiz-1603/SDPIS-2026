namespace SDPIS.Api.Models.DTOs;

// HU-012: funcionario elegible como encargado de denuncias.
// Se excluyen los jefes de área (regla HU-012) y los funcionarios
// con indisponibilidad vigente (integración con HU-007).
public record FuncionarioEncargadoDto(
    long IdFuncionario,
    string Nombre,
    string Rol,
    int CargaActiva,
    string? Especialidad
);