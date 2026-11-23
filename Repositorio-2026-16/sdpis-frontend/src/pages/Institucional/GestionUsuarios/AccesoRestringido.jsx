import './GestionUsuariosPage.css';

// HU-015: se muestra cuando el usuario llega a una vista para la que no
// tiene permiso (p. ej. tras una revocacion o con un enlace guardado).
export function AccesoRestringido({ onVolver }) {
    return (
        <section className="page-gestion-usuarios">
            <div className="gu-restringido" role="alert">
                <span className="gu-restringido__icono" aria-hidden="true">⚿</span>
                <h2>Acceso restringido</h2>
                <p>
                    Su usuario no cuenta con los permisos necesarios para esta sección.
                    Si considera que debería tener acceso, solicítelo a la jefatura de su área.
                </p>
                {onVolver && (
                    <button type="button" className="gu-btn gu-btn--secundario" onClick={onVolver}>
                        Volver al inicio
                    </button>
                )}
            </div>
        </section>
    );
}
