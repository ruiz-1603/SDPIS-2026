namespace SDPIS.Api.Models.DTOs;

// Entrada para registrar los criterios tecnicos de evaluacion de una denuncia (HU-010).
// FuncionarioValidadorId es quien realiza la evaluacion; hoy viene del usuario
// simulado del frontend, igual que RegistradoPor en RegistrarIndisponibilidadRequest.
// DanoPotencialSalud y RequiereCriterioEspecializado deben coincidir con un
// valor estandarizado de criterio_valor (Bajo/Medio/Alto y No/Si respectivamente);
// la base de datos rechaza cualquier otro valor via la FK de respuesta_criterio.
public record RegistrarCriteriosTecnicosRequest(
    long FuncionarioValidadorId,
    string VinculadoComercioElectronico,
    string DanoPotencialSalud,
    string RequiereCriterioEspecializado,
    string? ObservacionesTecnicas
);
