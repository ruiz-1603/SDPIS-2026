import { useEnPantalla } from '../../../hooks/useEnPantalla';

// onIngresar: () => void — navega a la pantalla de inicio de sesión (HU-001).
// Seccion del portal público que orienta a funcionarios del Ministerio a
// ingresar al portal institucional.
export function SeccionAccesoInstitucional({ onIngresar }) {
  const [referencia, visible] = useEnPantalla();

  return (
    <section ref={referencia} className={`pp-reveal pp-acceso-institucional${visible ? ' pp-reveal--visible' : ''}`}>
      <div className="pp-acceso-institucional__tarjeta">
        <h2>Función institucional</h2>
        <p>
          El personal del Ministerio de Salud que gestiona el SDPIS inicia sesión con
          su usuario o correo institucional para administrar las denuncias.
        </p>
        <button type="button" className="btn-primary" onClick={onIngresar}>
          Ingresar como funcionario
        </button>
      </div>
    </section>
  );
}