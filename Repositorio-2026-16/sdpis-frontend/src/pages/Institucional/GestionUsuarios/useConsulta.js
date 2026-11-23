import { useEffect, useState } from 'react';

// Ejecuta una consulta al backend cada vez que cambian sus claves.
//
// Cumple la regla react-hooks/set-state-in-effect: el efecto NO cambia el
// estado de forma sincronica (nada de setCargando(true) / setError(null) al
// inicio); solo guarda el resultado cuando la promesa responde. "cargando" se
// deriva comparando la clave de la ultima respuesta con la clave actual.
//
//   consultar: () => Promise
//   claves:    valores de los que depende la consulta (como las deps de useEffect)
//   opciones.mantenerAnterior: mientras recarga, sigue mostrando los datos
//                              previos (evita parpadeos al refrescar).
export function useConsulta(consultar, claves, { mantenerAnterior = false } = {}) {
    const clave = JSON.stringify(claves);
    const [estado, setEstado] = useState({ clave: null, datos: null, error: null });

    useEffect(() => {
        let vigente = true;
        consultar()
            .then((datos) => { if (vigente) setEstado({ clave, datos, error: null }); })
            .catch((e) => { if (vigente) setEstado({ clave, datos: null, error: e.message }); });
        return () => { vigente = false; };
        // La consulta depende solo de "clave" (las claves serializadas).
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [clave]);

    const alDia = estado.clave === clave;
    return {
        datos: alDia || mantenerAnterior ? estado.datos : null,
        error: alDia ? estado.error : null,
        cargando: !alDia,
    };
}
