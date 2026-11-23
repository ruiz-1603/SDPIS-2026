using Oracle.ManagedDataAccess.Client;
using SDPIS.Api.Models.DTOs;

namespace SDPIS.Api.Repositories;

// HU-016: unica via de escritura de la bitacora es sp_insertar_bitacora.
// La tabla es inmutable (trigger trg_bitacora_inmutable): no hay update ni delete.
public interface IBitacoraRepository
{
    // Abre su propia conexion/transaccion y devuelve la entrada creada.
    Task<BitacoraEntradaDto> RegistrarAsync(RegistroBitacora registro, CancellationToken ct);

    // Para quien ya tiene una transaccion abierta (la bitacora debe confirmarse
    // o revertirse junto con la accion que registra).
    Task RegistrarAsync(OracleConnection connection, OracleTransaction transaction,
        RegistroBitacora registro, CancellationToken ct);

    // Orden cronologico ascendente (fecha, id_bitacora). tipoRegistro null = todos.
    Task<(List<BitacoraEntradaDto> Items, int Total)> ListarPorDenunciaAsync(
        long denunciaId, string? tipoRegistro, int pagina, int tamano, CancellationToken ct);

    Task<FuncionarioAccesoInfo?> ObtenerFuncionarioAsync(long funcionarioId, CancellationToken ct);

    Task<DenunciaAccesoInfo?> ObtenerDenunciaAsync(long denunciaId, CancellationToken ct);

    // Panel de busqueda de Bitacora (bug reportado: las denuncias con
    // encargado ya asignado desaparecian del listado). areaId y
    // funcionarioAsignadoId son excluyentes segun el rol de quien consulta:
    // ambos null = todas las denuncias (superusuario).
    Task<List<DenunciaBitacoraListadoDto>> ListarDenunciasVisiblesAsync(
        long? areaId, long? funcionarioAsignadoId, CancellationToken ct);
}
