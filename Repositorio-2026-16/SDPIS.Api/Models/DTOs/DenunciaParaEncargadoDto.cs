namespace SDPIS.Api.Models.DTOs;

// HU-012: denuncia visible en la bandeja de asignación de
// funcionario encargado / reasignación de área.
public record DenunciaParaEncargadoDto(
    long IdDenuncia,
    string Consecutivo,
    string FechaRecepcion,
    long AreaActualId,
    string NombreAreaActual,
    bool VinculadaComercioElectronico,
    string? NombreEncargadoActual,
    bool PendienteReasignacionUvc
);