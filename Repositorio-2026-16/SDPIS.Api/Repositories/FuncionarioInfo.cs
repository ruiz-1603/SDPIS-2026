namespace SDPIS.Api.Repositories;

// Funcionario activo junto con el nombre de su area, para la lista (mockup HU-007).
public record FuncionarioInfo(
    long IdFuncionario,
    string Nombre,
    string? Rol,
    string AreaNombre
);