namespace SDPIS.Api.Repositories;

// HU-028: unica via de acceso a token_revocado. Revocar es insercion de solo
// el claim JTI (identificador del token); consultar es de solo lectura. La
// interfaz no expone actualizaciones ni borrados por fila.
public interface ITokenRevocadoRepository
{
    Task<bool> EstaRevocadoAsync(string jti, CancellationToken ct);
    Task RevocarAsync(RegistroTokenRevocado registro, CancellationToken ct);
}