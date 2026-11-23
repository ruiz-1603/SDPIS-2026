using SDPIS.Api.Models.DTOs;
using SDPIS.Api.Repositories;
using SDPIS.Api.Services.Autorizacion;

namespace SDPIS.Api.Tests.Services;

// Fake de IPermisosService: calcula los permisos con la logica REAL de
// CatalogoPermisos a partir de los datos del funcionario (rol, estado, area),
// asi que un fake "jefe_area" se comporta como un jefe de verdad.
// Devuelve el mismo contexto sin importar el id que se le pida: cada prueba
// simula a un solo usuario autenticado.
public class FakePermisosService(FuncionarioPermisosInfo info) : IPermisosService
{
    private readonly ContextoPermisos _contexto = CatalogoPermisos.Construir(info);

    // Atajo para las pruebas de Bitacora, que ya describen al funcionario con
    // FuncionarioAccesoInfo. Sin funcionario (pruebas que no consultan
    // permisos) devuelve un jefe de area cualquiera.
    public static FakePermisosService Desde(FuncionarioAccesoInfo? funcionario)
        => new(funcionario is null
            ? new FuncionarioPermisosInfo(1, Roles.JefeArea, true, 5, false, [])
            : new FuncionarioPermisosInfo(
                funcionario.IdFuncionario, funcionario.Rol, funcionario.Activo, funcionario.AreaId, false, []));

    public Task<ContextoPermisos?> ObtenerContextoAsync(long funcionarioId, CancellationToken ct)
        => Task.FromResult<ContextoPermisos?>(_contexto);

    public Task<bool> TieneAsync(long funcionarioId, string codigo, CancellationToken ct)
        => Task.FromResult(_contexto.Tiene(codigo));

    public Task<PermisosSesionDto> ObtenerSesionAsync(long funcionarioId, CancellationToken ct)
        => Task.FromResult(new PermisosSesionDto(
            _contexto.Efectivos.OrderBy(p => p, StringComparer.Ordinal).ToList(), _contexto.Alcance));

    public void Invalidar(long funcionarioId) { }
}