namespace SDPIS.Api.Repositories;

// HU-001: unica via de escritura de bitacora_sesion. La tabla es inmutable
// (trg_bitacora_sesion_inmutable): no hay update ni delete.
public interface IBitacoraSesionRepository
{
    Task RegistrarAsync(RegistroBitacoraSesion registro, CancellationToken ct);
}