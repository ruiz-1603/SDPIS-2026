namespace SDPIS.Api.Services.Criticidad;

// Resultado de aplicar CriticidadCalculator sobre un puntaje total (HU-011).
// PlazoDias es el insumo para la definicion automatica de plazos de atencion
// (AC #10): quien consuma este resultado decide como aplicarlo a la denuncia.
public record ResultadoCalculoCriticidad(string Nivel, int PlazoDias);
