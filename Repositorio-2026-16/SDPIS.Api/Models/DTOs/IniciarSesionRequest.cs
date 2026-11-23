namespace SDPIS.Api.Models.DTOs;

// HU-001: entrada de un inicio de sesion. CodigoUsuario es la cedula
// (o el codigo MSI) que identifica al funcionario; tambien se acepta
// el correo institucional como alternativa al codigo.
public record IniciarSesionRequest(
    string CodigoUsuario,
    string Clave
);