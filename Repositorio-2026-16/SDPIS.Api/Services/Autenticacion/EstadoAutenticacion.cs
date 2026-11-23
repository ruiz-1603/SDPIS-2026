namespace SDPIS.Api.Services.Autenticacion;

// Resultado de validar credenciales contra el origen (local o MSI).
public enum EstadoAutenticacion
{
    Exitosa,
    CredencialesInvalidas,
    CuentaDeshabilitada,
    CuentaBloqueada,
    RolInvalido,
    RequiereCambioClave,
    RequiereSegundoFactor,
    Error
}