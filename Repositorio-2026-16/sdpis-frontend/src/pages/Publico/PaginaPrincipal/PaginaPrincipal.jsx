import { BarraSuperiorPublica } from '../../../components/comunes/BarraSuperiorPublica';
import { AvisoToast } from '../../../components/comunes/AvisoToast';
import { PiePaginaPublico } from '../../../components/comunes/PiePaginaPublico';
import { useAvisoToast } from '../../../hooks/useAvisoToast';
import { HeroPrincipal } from './HeroPrincipal';
import { SeccionComoFunciona } from './SeccionComoFunciona';
import { SeccionSobreElSistema } from './SeccionSobreElSistema';
import { SeccionExperienciaDenunciante } from './SeccionExperienciaDenunciante';
import { SeccionAccesoInstitucional } from './SeccionAccesoInstitucional';
import { SeccionSeguimiento } from './SeccionSeguimiento';
import { SeccionLlamadoFinal } from './SeccionLlamadoFinal';

// onIrADenuncia: () => void — navega al formulario público de denuncia (HU-002).
// onIrALogin: () => void — navega a la pantalla de inicio de sesión (HU-001) ahora
// que la autenticación real está implementada en el backend; reemplaza el antiguo
// modal simulado.
export function PaginaPrincipal({ onIrADenuncia, onIrALogin }) {
  const { aviso, mostrarAviso } = useAvisoToast();

  function manejarFuncionPendiente() {
    mostrarAviso('Esta función se conectará al backend en un sprint posterior.', 'aviso');
  }

  return (
    <>
      <BarraSuperiorPublica
        onIngresar={onIrALogin}
        onDenunciar={onIrADenuncia}
      />

      <main>
        <HeroPrincipal onDenunciar={onIrADenuncia} onConsultarCaso={manejarFuncionPendiente} />

        <div className="main-publico">
          <SeccionComoFunciona />
          <SeccionSobreElSistema />
          <SeccionExperienciaDenunciante />
          <SeccionAccesoInstitucional onIngresar={onIrALogin} />
          <SeccionSeguimiento />
          <SeccionLlamadoFinal onDenunciar={onIrADenuncia} />
        </div>
      </main>

      <PiePaginaPublico />

      <AvisoToast aviso={aviso} />
    </>
  );
}