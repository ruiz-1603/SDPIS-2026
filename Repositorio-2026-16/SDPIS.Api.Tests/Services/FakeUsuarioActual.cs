using SDPIS.Api.Services.Autorizacion;

namespace SDPIS.Api.Tests.Services;

// Fake de IUsuarioActual: simula al funcionario autenticado (en la app real
// su id sale del token JWT).
public class FakeUsuarioActual(long funcionarioId = 1) : IUsuarioActual
{
    public long FuncionarioId { get; } = funcionarioId;
}