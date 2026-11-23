using SDPIS.Api.Repositories;
using SDPIS.Api.Services.Autorizacion;

namespace SDPIS.Api.Tests.Services;

// HU-015: lo que permite cada rol, lo que se puede delegar y el alcance de
// denuncias. CatalogoPermisos es logica pura: no necesita mocks ni base de datos.
public class CatalogoPermisosTests
{
    [Fact]
    public void Construir_Validador_NoPuedeAbrirRolesYPermisos()
    {
        var info = new FuncionarioPermisosInfo(1, Roles.Validador, true, 2, false, []);

        var contexto = CatalogoPermisos.Construir(info);

        Assert.False(contexto.Tiene(Permisos.VerFuncionariosArea));
        Assert.False(contexto.Tiene(Permisos.VerListaAreas));
        Assert.False(contexto.Tiene(Permisos.VerBitacoraAdministrativa));
        Assert.Equal(AlcanceDenuncias.Propias, contexto.Alcance);
    }

    [Fact]
    public void Construir_ValidadorConPermisoDelegado_ConservaRolYGanaElPermiso()
    {
        var info = new FuncionarioPermisosInfo(
            1, Roles.Validador, true, 2, false, [Permisos.VerFuncionariosArea]);

        var contexto = CatalogoPermisos.Construir(info);

        Assert.True(contexto.EsRol(Roles.Validador));
        Assert.True(contexto.Tiene(Permisos.VerFuncionariosArea));
    }

    [Fact]
    public void Construir_FuncionarioInactivo_NoTienePermisos()
    {
        var info = new FuncionarioPermisosInfo(
            1, Roles.JefeArea, false, 2, true, [Permisos.VerFuncionariosArea]);

        var contexto = CatalogoPermisos.Construir(info);

        Assert.Empty(contexto.Efectivos);
        Assert.Equal(AlcanceDenuncias.Ninguna, contexto.Alcance);
    }

    [Theory]
    [InlineData(Permisos.AdministrarSistema)]
    [InlineData(Permisos.DelegarPermisosArea)]
    [InlineData(Permisos.AprobarCierre)]
    public void Delegables_PermisosProhibidos_NoEstanEnElCatalogo(string codigo)
    {
        Assert.DoesNotContain(codigo, CatalogoPermisos.Delegables.Keys);
    }
}