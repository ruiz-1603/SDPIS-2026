using SDPIS.Api.Repositories;

namespace SDPIS.Api.Services.Criticidad;

// Calculo de criticidad (HU-011, subtarea #118). Puro: no toca base de datos,
// recibe los rangos ya leidos del catalogo nivel_criticidad por el repositorio.
// Regla confirmada en el issue #62: puntaje = suma de "Dano potencial a la
// salud" + "Criterio tecnico especializado UVC" (maximo 6) -- >=5 ALTO,
// >=3 MEDIO, resto BAJO. Esta clase no conoce esos numeros: solo aplica los
// rangos que recibe, para no duplicar los umbrales en dos lugares.
public static class CriticidadCalculator
{
    public static ResultadoCalculoCriticidad Calcular(decimal puntajeTotal, IReadOnlyList<RangoCriticidad> rangos)
    {
        var rango = rangos.FirstOrDefault(r => puntajeTotal >= r.PuntajeMin && puntajeTotal <= r.PuntajeMax);

        if (rango is null)
        {
            throw new InvalidOperationException(
                $"No hay un rango de criticidad configurado que cubra el puntaje {puntajeTotal}.");
        }

        return new ResultadoCalculoCriticidad(rango.Nivel, rango.PlazoDias);
    }
}
