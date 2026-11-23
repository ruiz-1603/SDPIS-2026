namespace SDPIS.Api.Models.DTOs;

// HU-014: plazo maximo de atencion (en dias) por nivel de criticidad.
// El plazo corre desde la notificacion al funcionario encargado.
public record PlazoCriticidadDto(
    string Nivel,            // ALTO | MEDIO | BAJO
    decimal PuntajeMin,
    decimal PuntajeMax,
    int PlazoDias);

public record ActualizarPlazoRequest(int PlazoDias);
