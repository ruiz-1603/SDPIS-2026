namespace SDPIS.Api.Models.DTOs;

// HU-016: comentario manual en la bitacora (se guarda como tipo "comentario").
// TODO: reemplazar FuncionarioId simulado por el usuario autenticado via MSI
// cuando este disponible.
public record AgregarComentarioBitacoraRequest(
    long FuncionarioId,
    string Contenido
);
