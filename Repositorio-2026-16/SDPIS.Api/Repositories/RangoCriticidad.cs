namespace SDPIS.Api.Repositories;

// Representa un renglon del catalogo nivel_criticidad (tabla ya definida en el
// esquema, pensada para HU-013). Para HU-011 se siembra con valores fijos, sin
// pantalla de administracion (ver AC #9 de HU-011). PuntajeMin/PuntajeMax son
// inclusivos.
public record RangoCriticidad(string Nivel, decimal PuntajeMin, decimal PuntajeMax, int PlazoDias);
