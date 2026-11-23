using SDPIS.Api.Models.DTOs;
using SDPIS.Api.Repositories;
using SDPIS.Api.Services.Autorizacion;
using SDPIS.Api.Services.GestionUsuarios;

namespace SDPIS.Api.Tests.Services;

// Fake de IVisibilidadDenunciasService. Por defecto la denuncia 9001 esta en
// el area 5 y asignada al funcionario 100, y el acceso siempre se permite
// (la regla real de visibilidad se prueba en su propio servicio). Cambie
// DenunciaARetornar para simular otra denuncia.
public class FakeVisibilidadDenunciasService : IVisibilidadDenunciasService
{
    public DenunciaAlcanceRegistro DenunciaARetornar { get; set; } = new(9001, 5, 100);

    public Task<AlcanceResuelto> ResolverAlcanceAsync(long? areaSolicitada, CancellationToken ct)
        => Task.FromResult(new AlcanceResuelto(AlcanceDenuncias.Todas, areaSolicitada, null));

    public Task<List<DenunciaVisibleDto>> ListarAsync(
        long? areaSolicitada, string? filtro, CancellationToken ct, string? criterios = null)
        => Task.FromResult(new List<DenunciaVisibleDto>());

    public Task<long> ResolverAreaOperativaAsync(long? areaSolicitada, string permisoRequerido, CancellationToken ct)
        => Task.FromResult(areaSolicitada ?? DenunciaARetornar.AreaActualId);

    public Task<DenunciaAlcanceRegistro> VerificarAccesoDenunciaAsync(long denunciaId, CancellationToken ct)
        => Task.FromResult(DenunciaARetornar);
}