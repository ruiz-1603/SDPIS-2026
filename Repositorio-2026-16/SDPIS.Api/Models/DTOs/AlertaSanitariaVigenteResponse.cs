namespace SDPIS.Api.Models.DTOs;

// Salida de la consulta automatica de alerta sanitaria vigente por producto
// (HU-010, subtarea backend #116), como apoyo informativo para que el
// funcionario validador registre el criterio de dano potencial a la salud.
public record AlertaSanitariaVigenteResponse(
    bool TieneAlertaVigente,
    List<string> NumerosAlerta
);
