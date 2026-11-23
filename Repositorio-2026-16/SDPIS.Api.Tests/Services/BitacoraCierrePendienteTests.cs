using SDPIS.Api.Models.DTOs;
using SDPIS.Api.Repositories;
using SDPIS.Api.Services;
using SDPIS.Api.Services.GestionUsuarios;
using Xunit;

namespace SDPIS.Api.Tests.Services;

// HU-018: una vez enviada la solicitud de cierre, el funcionario responsable
// ya no puede agregar comentarios a la bitacora; la jefatura si.
public class BitacoraCierrePendienteTests
{
    private static BitacoraService CrearServicio(FakeBitacoraRepository repository)
        => new(repository, FakePermisosService.Desde(repository.FuncionarioARetornar));

    [Fact]
    public async Task AgregarComentarioAsync_FuncionarioConCierrePendiente_LanzaReglaNegocioYNoRegistra()
    {
        var repository = new FakeBitacoraRepository
        {
            FuncionarioARetornar = new FuncionarioAccesoInfo(100, "validador", AreaId: 5, Activo: true),
            DenunciaARetornar = new DenunciaAccesoInfo(9001, AreaActualId: 5, FuncionarioAsignadoId: 100,
                CierrePendiente: true),
        };
        var service = CrearServicio(repository);

        await Assert.ThrowsAsync<ReglaNegocioException>(() =>
            service.AgregarComentarioAsync(9001, new AgregarComentarioBitacoraRequest(100, "Otro avance"),
                CancellationToken.None));

        Assert.Empty(repository.RegistrosInsertados);
    }

    [Fact]
    public async Task AgregarComentarioAsync_JefaturaConCierrePendiente_RegistraElComentario()
    {
        var repository = new FakeBitacoraRepository
        {
            FuncionarioARetornar = new FuncionarioAccesoInfo(1, "jefe_area", AreaId: 5, Activo: true),
            DenunciaARetornar = new DenunciaAccesoInfo(9001, AreaActualId: 5, FuncionarioAsignadoId: 100,
                CierrePendiente: true),
        };
        var service = CrearServicio(repository);

        await service.AgregarComentarioAsync(9001, new AgregarComentarioBitacoraRequest(1, "Revisando la solicitud"),
            CancellationToken.None);

        var registro = Assert.Single(repository.RegistrosInsertados);
        Assert.Equal(1, registro.FuncionarioId);
    }

    [Fact]
    public async Task AgregarComentarioAsync_FuncionarioSinCierrePendiente_RegistraElComentario()
    {
        var repository = new FakeBitacoraRepository
        {
            FuncionarioARetornar = new FuncionarioAccesoInfo(100, "validador", AreaId: 5, Activo: true),
            DenunciaARetornar = new DenunciaAccesoInfo(9001, AreaActualId: 5, FuncionarioAsignadoId: 100),
        };
        var service = CrearServicio(repository);

        await service.AgregarComentarioAsync(9001, new AgregarComentarioBitacoraRequest(100, "Avance del caso"),
            CancellationToken.None);

        Assert.Single(repository.RegistrosInsertados);
    }
}
