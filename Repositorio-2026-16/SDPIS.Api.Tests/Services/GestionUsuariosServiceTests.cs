using Microsoft.Extensions.Configuration;
using Moq;
using SDPIS.Api.Models.DTOs;
using SDPIS.Api.Repositories;
using SDPIS.Api.Services.Autorizacion;
using SDPIS.Api.Services.GestionUsuarios;
using SDPIS.Api.Services.Validation;

namespace SDPIS.Api.Tests.Services;

// HU-015: LECTURAS de GestionUsuariosService (quien puede ver que).
//   * Superusuario: todas las areas.
//   * Jefe de area (o delegado): solo su area.
//   * Validador: solo lo propio, salvo que se le delegue un permiso.
// El repositorio se simula con Moq; los permisos se calculan con la logica
// real de CatalogoPermisos a traves de FakePermisosService.
public class GestionUsuariosServiceTests
{
    private readonly Mock<IGestionUsuariosRepository> _repositorioMock = new();
    private FiltroDenuncias? _filtroDenuncias;
    private FiltroBitacora? _filtroBitacora;

    // ---------- Actores (el usuario autenticado de cada prueba) ----------

    private static FuncionarioPermisosInfo Superusuario()
        => new(1, Roles.Superusuario, true, 1, false, []);

    private static FuncionarioPermisosInfo Jefe(long areaId = 5)
        => new(2, Roles.JefeArea, true, areaId, true, []);

    private static FuncionarioPermisosInfo Validador(long id = 3, long areaId = 5, params string[] delegados)
        => new(id, Roles.Validador, true, areaId, false, delegados);

    private GestionUsuariosService CrearServicio(FuncionarioPermisosInfo actor)
        => new(
            _repositorioMock.Object,
            new FakePermisosService(actor),
            new FakeUsuarioActual(actor.FuncionarioId),
            new ConfigurationBuilder().Build());

    private static FuncionarioRegistro Registro(long id, string rol, long areaId)
        => new(id, "Funcionario de prueba", rol, "prueba@sdpis.go.cr", true, areaId, null, false);

    // =====================================================
    // ListarPermisosDelegables
    // =====================================================

    [Fact]
    public void ListarPermisosDelegables_DevuelveTodoElCatalogoOrdenadoPorGrupoYCodigo()
    {
        var resultado = CrearServicio(Superusuario()).ListarPermisosDelegables();

        Assert.Equal(CatalogoPermisos.Delegables.Count, resultado.Count);
        var codigosEsperados = resultado
            .OrderBy(p => p.Grupo, StringComparer.Ordinal)
            .ThenBy(p => p.Codigo, StringComparer.Ordinal)
            .Select(p => p.Codigo);
        Assert.Equal(codigosEsperados, resultado.Select(p => p.Codigo));
    }

    [Fact]
    public void ListarPermisosDelegables_NoIncluyeLosPermisosProhibidos()
    {
        var codigos = CrearServicio(Superusuario()).ListarPermisosDelegables().Select(p => p.Codigo);

        Assert.DoesNotContain(Permisos.AdministrarSistema, codigos);
        Assert.DoesNotContain(Permisos.DelegarPermisosArea, codigos);
        Assert.DoesNotContain(Permisos.AprobarCierre, codigos);
    }

    // =====================================================
    // ListarFuncionariosAsync
    // =====================================================

    [Fact]
    public async Task ListarFuncionariosAsync_SuperusuarioSinFiltros_ConsultaTodasLasAreasYRegistraLaConsulta()
    {
        var esperados = new List<FuncionarioGestionDto>();
        _repositorioMock
            .Setup(r => r.ListarFuncionariosAsync(null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(esperados);

        var resultado = await CrearServicio(Superusuario())
            .ListarFuncionariosAsync(null, null, CancellationToken.None);

        Assert.Same(esperados, resultado);
        _repositorioMock.Verify(r => r.RegistrarConsultaDiariaAsync(
            It.Is<RegistroBitacoraAdministrativa>(b => b.RealizadoPorId == 1 && b.Categoria == "CONSULTA"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListarFuncionariosAsync_SuperusuarioPideListaDeJefes_FiltraPorAreaYRol()
    {
        _repositorioMock
            .Setup(r => r.ListarFuncionariosAsync(7, Roles.JefeArea, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FuncionarioGestionDto>());

        await CrearServicio(Superusuario())
            .ListarFuncionariosAsync(7, Roles.JefeArea, CancellationToken.None);

        _repositorioMock.Verify(r => r.ListarFuncionariosAsync(7, Roles.JefeArea, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListarFuncionariosAsync_JefeSinIndicarArea_SoloVeSuPropiaArea()
    {
        _repositorioMock
            .Setup(r => r.ListarFuncionariosAsync(5, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FuncionarioGestionDto>());

        await CrearServicio(Jefe(areaId: 5))
            .ListarFuncionariosAsync(null, null, CancellationToken.None);

        _repositorioMock.Verify(r => r.ListarFuncionariosAsync(5, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListarFuncionariosAsync_JefeIndicaSuPropiaArea_EsAceptado()
    {
        _repositorioMock
            .Setup(r => r.ListarFuncionariosAsync(5, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FuncionarioGestionDto>());

        var excepcion = await Record.ExceptionAsync(() => CrearServicio(Jefe(areaId: 5))
            .ListarFuncionariosAsync(5, null, CancellationToken.None));

        Assert.Null(excepcion);
    }

    [Fact]
    public async Task ListarFuncionariosAsync_JefePideOtraArea_LanzaPermisoDenegadoSinConsultarLaBase()
    {
        var excepcion = await Assert.ThrowsAsync<PermisoDenegadoException>(() => CrearServicio(Jefe(areaId: 5))
            .ListarFuncionariosAsync(9, null, CancellationToken.None));

        Assert.Contains("propia area", excepcion.Message);
        _repositorioMock.Verify(r => r.ListarFuncionariosAsync(
            It.IsAny<long?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ListarFuncionariosAsync_ValidadorSinPermiso_LanzaPermisoDenegado()
    {
        await Assert.ThrowsAsync<PermisoDenegadoException>(() => CrearServicio(Validador())
            .ListarFuncionariosAsync(null, null, CancellationToken.None));

        _repositorioMock.Verify(r => r.ListarFuncionariosAsync(
            It.IsAny<long?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ListarFuncionariosAsync_ValidadorConPermisoDelegado_VeSoloSuArea()
    {
        // Permiso delegado por el jefe: VER_FUNCIONARIOS_AREA.
        var validadorConPermiso = Validador(3, 5, Permisos.VerFuncionariosArea);
        _repositorioMock
            .Setup(r => r.ListarFuncionariosAsync(5, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FuncionarioGestionDto>());

        await CrearServicio(validadorConPermiso).ListarFuncionariosAsync(null, null, CancellationToken.None);

        _repositorioMock.Verify(r => r.ListarFuncionariosAsync(5, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListarFuncionariosAsync_RolConEspacios_SeNormalizaAntesDeConsultar()
    {
        _repositorioMock
            .Setup(r => r.ListarFuncionariosAsync(null, Roles.JefeArea, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FuncionarioGestionDto>());

        await CrearServicio(Superusuario())
            .ListarFuncionariosAsync(null, " jefe_area ", CancellationToken.None);

        _repositorioMock.Verify(r => r.ListarFuncionariosAsync(null, Roles.JefeArea, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListarFuncionariosAsync_RolInexistente_LanzaValidacionAntesDeConsultarPermisos()
    {
        await Assert.ThrowsAsync<DenunciaValidationException>(() => CrearServicio(Superusuario())
            .ListarFuncionariosAsync(null, "administrador", CancellationToken.None));

        _repositorioMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ListarFuncionariosAsync_ActorInactivo_LanzaPermisoDenegado()
    {
        var superusuarioInactivo = new FuncionarioPermisosInfo(1, Roles.Superusuario, false, 1, false, []);

        var excepcion = await Assert.ThrowsAsync<PermisoDenegadoException>(() => CrearServicio(superusuarioInactivo)
            .ListarFuncionariosAsync(null, null, CancellationToken.None));

        Assert.Contains("inactivo", excepcion.Message);
    }

    // =====================================================
    // ListarDenunciasFuncionarioAsync
    // =====================================================

    private void PrepararDenuncias()
        => _repositorioMock
            .Setup(r => r.ListarDenunciasAsync(It.IsAny<FiltroDenuncias>(), It.IsAny<CancellationToken>()))
            .Callback<FiltroDenuncias, CancellationToken>((filtro, _) => _filtroDenuncias = filtro)
            .ReturnsAsync(new List<DenunciaVisibleDto>());

    private void PrepararObjetivo(FuncionarioRegistro? objetivo, long id)
        => _repositorioMock
            .Setup(r => r.ObtenerFuncionarioAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(objetivo);

    [Fact]
    public async Task ListarDenunciasFuncionarioAsync_FuncionarioInexistente_LanzaRecursoNoEncontrado()
    {
        PrepararObjetivo(null, 99);

        await Assert.ThrowsAsync<RecursoNoEncontradoException>(() => CrearServicio(Superusuario())
            .ListarDenunciasFuncionarioAsync(99, CancellationToken.None));
    }

    [Fact]
    public async Task ListarDenunciasFuncionarioAsync_ValidadorConsultaSusPropias_FiltraPorSuId()
    {
        PrepararObjetivo(Registro(3, Roles.Validador, 5), 3);
        PrepararDenuncias();

        await CrearServicio(Validador(3, 5)).ListarDenunciasFuncionarioAsync(3, CancellationToken.None);

        Assert.Equal(new FiltroDenuncias(null, 3, "TODAS"), _filtroDenuncias);
    }

    [Fact]
    public async Task ListarDenunciasFuncionarioAsync_ValidadorConsultaAOtroFuncionario_LanzaPermisoDenegado()
    {
        PrepararObjetivo(Registro(4, Roles.Validador, 5), 4);
        PrepararDenuncias();

        await Assert.ThrowsAsync<PermisoDenegadoException>(() => CrearServicio(Validador(3, 5))
            .ListarDenunciasFuncionarioAsync(4, CancellationToken.None));

        _repositorioMock.Verify(r => r.ListarDenunciasAsync(
            It.IsAny<FiltroDenuncias>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ListarDenunciasFuncionarioAsync_JefeConsultaValidadorDeSuArea_FiltraPorElValidador()
    {
        PrepararObjetivo(Registro(3, Roles.Validador, 5), 3);
        PrepararDenuncias();

        await CrearServicio(Jefe(areaId: 5)).ListarDenunciasFuncionarioAsync(3, CancellationToken.None);

        // Acotado a su area: el validador podria tener denuncias de otras areas.
        Assert.Equal(new FiltroDenuncias(5, 3, "TODAS"), _filtroDenuncias);
    }

    [Fact]
    public async Task ListarDenunciasFuncionarioAsync_JefeConsultaValidadorConSuAreaComoAdicional_VeSoloLasDeSuArea()
    {
        // Area base 9; el superusuario le otorgo el area 5 (la del jefe).
        PrepararObjetivo(Registro(3, Roles.Validador, 9), 3);
        _repositorioMock
            .Setup(r => r.FuncionarioPerteneceAAreaAsync(3, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        PrepararDenuncias();

        await CrearServicio(Jefe(areaId: 5)).ListarDenunciasFuncionarioAsync(3, CancellationToken.None);

        Assert.Equal(new FiltroDenuncias(5, 3, "TODAS"), _filtroDenuncias);
    }

    [Fact]
    public async Task ListarDenunciasFuncionarioAsync_JefeConsultaValidadorDeOtraArea_LanzaPermisoDenegado()
    {
        PrepararObjetivo(Registro(3, Roles.Validador, 9), 3);
        PrepararDenuncias();

        await Assert.ThrowsAsync<PermisoDenegadoException>(() => CrearServicio(Jefe(areaId: 5))
            .ListarDenunciasFuncionarioAsync(3, CancellationToken.None));
    }

    [Fact]
    public async Task ListarDenunciasFuncionarioAsync_SuperusuarioConsultaCualquierArea_EsAceptado()
    {
        PrepararObjetivo(Registro(3, Roles.Validador, 9), 3);
        PrepararDenuncias();

        var excepcion = await Record.ExceptionAsync(() => CrearServicio(Superusuario())
            .ListarDenunciasFuncionarioAsync(3, CancellationToken.None));

        Assert.Null(excepcion);
        Assert.Equal(new FiltroDenuncias(null, 3, "TODAS"), _filtroDenuncias);
    }

    [Fact]
    public async Task ListarDenunciasFuncionarioAsync_ObjetivoEsJefe_DevuelveLasDenunciasPorAsignarDeSuArea()
    {
        // Un jefe nunca tiene denuncias asignadas: se muestran las de su area por asignar.
        PrepararObjetivo(Registro(2, Roles.JefeArea, 5), 2);
        PrepararDenuncias();

        await CrearServicio(Superusuario()).ListarDenunciasFuncionarioAsync(2, CancellationToken.None);

        Assert.Equal(new FiltroDenuncias(5, null, "POR_ASIGNAR"), _filtroDenuncias);
    }

    // =====================================================
    // GestionarAreaAdicionalAsync (reglas previas a la transaccion)
    // =====================================================

    [Fact]
    public async Task GestionarAreaAdicionalAsync_JefeDeArea_LanzaPermisoDenegado()
    {
        // Solo el superusuario otorga areas adicionales; el jefe no puede, ni en su area.
        await Assert.ThrowsAsync<PermisoDenegadoException>(() => CrearServicio(Jefe(areaId: 5))
            .GestionarAreaAdicionalAsync(3, new GestionarAreaAdicionalRequest(7, "OTORGAR"), CancellationToken.None));

        _repositorioMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GestionarAreaAdicionalAsync_ValidadorConPermisosDelegados_LanzaPermisoDenegado()
    {
        await Assert.ThrowsAsync<PermisoDenegadoException>(() => CrearServicio(
                Validador(3, 5, Permisos.AsignarDenuncias, Permisos.VerTodasAreas))
            .GestionarAreaAdicionalAsync(4, new GestionarAreaAdicionalRequest(7, "OTORGAR"), CancellationToken.None));

        _repositorioMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GestionarAreaAdicionalAsync_SuperusuarioSobreSiMismo_LanzaReglaNegocio()
    {
        await Assert.ThrowsAsync<ReglaNegocioException>(() => CrearServicio(Superusuario())
            .GestionarAreaAdicionalAsync(1, new GestionarAreaAdicionalRequest(7, "OTORGAR"), CancellationToken.None));

        _repositorioMock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(0, "OTORGAR")]
    [InlineData(7, "OTRA")]
    [InlineData(7, "")]
    public async Task GestionarAreaAdicionalAsync_SolicitudInvalida_LanzaValidacion(long areaId, string accion)
    {
        await Assert.ThrowsAsync<DenunciaValidationException>(() => CrearServicio(Superusuario())
            .GestionarAreaAdicionalAsync(3, new GestionarAreaAdicionalRequest(areaId, accion), CancellationToken.None));

        _repositorioMock.VerifyNoOtherCalls();
    }

    // =====================================================
    // ListarAreasAsync
    // =====================================================

    [Fact]
    public async Task ListarAreasAsync_Superusuario_DevuelveLasAreasYRegistraLaConsulta()
    {
        var esperadas = new List<AreaGestionDto>();
        _repositorioMock
            .Setup(r => r.ListarAreasAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(esperadas);

        var resultado = await CrearServicio(Superusuario()).ListarAreasAsync(CancellationToken.None);

        Assert.Same(esperadas, resultado);
        _repositorioMock.Verify(r => r.RegistrarConsultaDiariaAsync(
            It.Is<RegistroBitacoraAdministrativa>(b =>
                b.RealizadoPorId == 1 && b.AreaId == null && b.Categoria == "CONSULTA"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListarAreasAsync_JefeDeArea_LanzaPermisoDenegado()
    {
        // La lista de areas es del superusuario, no del jefe.
        await Assert.ThrowsAsync<PermisoDenegadoException>(() => CrearServicio(Jefe())
            .ListarAreasAsync(CancellationToken.None));

        _repositorioMock.Verify(r => r.ListarAreasAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ListarAreasAsync_ValidadorConPermisoDelegado_EsAceptado()
    {
        _repositorioMock
            .Setup(r => r.ListarAreasAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AreaGestionDto>());

        var excepcion = await Record.ExceptionAsync(() => CrearServicio(Validador(3, 5, Permisos.VerListaAreas))
            .ListarAreasAsync(CancellationToken.None));

        Assert.Null(excepcion);
    }

    // =====================================================
    // ListarBitacoraAsync
    // =====================================================

    private static readonly DateTime FechaEvento = new(2026, 10, 5, 8, 0, 0);

    private static ConsultaBitacora Consulta(
        long? areaId = null, string? categoria = null, DateTime? desde = null, DateTime? hasta = null,
        string? texto = null, bool incluirConsultas = false, int pagina = 1, int tamano = 20)
        => new(areaId, categoria, desde, hasta, texto, incluirConsultas, pagina, tamano);

    private static EventoBitacoraRegistro EventoAdmin()
        => new("ADMIN", 10, FechaEvento, "PERMISOS", 2, "Jefe de prueba", 3, "Valeria Mora", 5, "ARS Cartago",
            "Otorgado el permiso VER_FUNCIONARIOS_AREA a Valeria Mora.",
            "VER_FUNCIONARIOS_AREA=NO", "VER_FUNCIONARIOS_AREA=SI", null, null, null);

    private static EventoBitacoraRegistro EventoSesion(
        string resultado, string? nombre = "Valeria Mora", string? codigoUsuario = null, string? ip = null)
        => new("SESION", 1, FechaEvento, "SESION", 3, nombre, null, null, 5, "ARS Cartago",
            "detalle crudo", null, null, resultado, codigoUsuario, ip);

    private void PrepararBitacora(params EventoBitacoraRegistro[] eventos)
        => _repositorioMock
            .Setup(r => r.ListarBitacoraAsync(It.IsAny<FiltroBitacora>(), It.IsAny<CancellationToken>()))
            .Callback<FiltroBitacora, CancellationToken>((filtro, _) => _filtroBitacora = filtro)
            .ReturnsAsync((eventos.ToList(), eventos.Length));

    [Fact]
    public async Task ListarBitacoraAsync_Superusuario_UsaElAreaSolicitadaYNormalizaLosFiltros()
    {
        PrepararBitacora();
        var consulta = Consulta(
            areaId: 7,
            categoria: " permisos ",
            desde: new DateTime(2026, 10, 1, 14, 30, 0),
            hasta: new DateTime(2026, 10, 5, 9, 0, 0),
            texto: "  Valeria  ");

        await CrearServicio(Superusuario()).ListarBitacoraAsync(consulta, CancellationToken.None);

        Assert.NotNull(_filtroBitacora);
        Assert.Equal(7, _filtroBitacora.AreaId);
        Assert.Equal("PERMISOS", _filtroBitacora.Categoria);
        Assert.Equal(new DateTime(2026, 10, 1), _filtroBitacora.Desde);
        // "Hasta" incluye todo ese dia, por eso el repositorio recibe el dia siguiente.
        Assert.Equal(new DateTime(2026, 10, 6), _filtroBitacora.HastaExclusivo);
        Assert.Equal("Valeria", _filtroBitacora.Texto);
    }

    [Fact]
    public async Task ListarBitacoraAsync_Jefe_IgnoraElAreaSolicitadaYUsaLaSuya()
    {
        PrepararBitacora();

        await CrearServicio(Jefe(areaId: 5))
            .ListarBitacoraAsync(Consulta(areaId: 99), CancellationToken.None);

        Assert.NotNull(_filtroBitacora);
        Assert.Equal(5, _filtroBitacora.AreaId);
    }

    [Fact]
    public async Task ListarBitacoraAsync_ValidadorSinPermiso_LanzaPermisoDenegado()
    {
        await Assert.ThrowsAsync<PermisoDenegadoException>(() => CrearServicio(Validador())
            .ListarBitacoraAsync(Consulta(), CancellationToken.None));

        _repositorioMock.Verify(r => r.ListarBitacoraAsync(
            It.IsAny<FiltroBitacora>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ListarBitacoraAsync_PaginaInvalida_LanzaValidacion()
    {
        await Assert.ThrowsAsync<DenunciaValidationException>(() => CrearServicio(Superusuario())
            .ListarBitacoraAsync(Consulta(pagina: 0), CancellationToken.None));
    }

    [Fact]
    public async Task ListarBitacoraAsync_CategoriaInexistente_LanzaValidacion()
    {
        await Assert.ThrowsAsync<DenunciaValidationException>(() => CrearServicio(Superusuario())
            .ListarBitacoraAsync(Consulta(categoria: "INVENTADA"), CancellationToken.None));
    }

    [Fact]
    public async Task ListarBitacoraAsync_CategoriaYTextoEnBlanco_SePasanComoNull()
    {
        PrepararBitacora();

        await CrearServicio(Superusuario())
            .ListarBitacoraAsync(Consulta(categoria: "   ", texto: "   "), CancellationToken.None);

        Assert.NotNull(_filtroBitacora);
        Assert.Null(_filtroBitacora.Categoria);
        Assert.Null(_filtroBitacora.Texto);
    }

    [Fact]
    public async Task ListarBitacoraAsync_DevuelveLaPaginaConTotalPaginaYTamano()
    {
        PrepararBitacora(EventoAdmin());

        var resultado = await CrearServicio(Superusuario())
            .ListarBitacoraAsync(Consulta(pagina: 2, tamano: 10), CancellationToken.None);

        Assert.Equal(1, resultado.Total);
        Assert.Equal(2, resultado.Pagina);
        Assert.Equal(10, resultado.Tamano);
        Assert.Single(resultado.Items);
    }

    [Fact]
    public async Task ListarBitacoraAsync_EventoAdministrativo_SeDevuelveSinTraducirNiAgregarTipo()
    {
        PrepararBitacora(EventoAdmin());

        var resultado = await CrearServicio(Superusuario())
            .ListarBitacoraAsync(Consulta(), CancellationToken.None);

        var item = Assert.Single(resultado.Items);
        Assert.Equal("ADMIN", item.Origen);
        Assert.Equal("Otorgado el permiso VER_FUNCIONARIOS_AREA a Valeria Mora.", item.Detalle);
        Assert.Equal("VER_FUNCIONARIOS_AREA=NO", item.ValorAnterior);
        Assert.Equal("VER_FUNCIONARIOS_AREA=SI", item.ValorNuevo);
        Assert.Equal(3, item.FuncionarioAfectadoId);
        Assert.Null(item.TipoEvento);
        Assert.Null(item.DetalleTecnico);
    }

    [Theory]
    [InlineData("EXITO", "INICIO_SESION")]
    [InlineData("CIERRE_MANUAL", "CIERRE_SESION")]
    [InlineData("CIERRE_INACTIVIDAD", "CIERRE_SESION")]
    [InlineData("FALLO", "INTENTO_FALLIDO")]
    [InlineData("DESHABILITADO", "INTENTO_FALLIDO")]
    [InlineData("ROL_INVALIDO", "INTENTO_FALLIDO")]
    [InlineData("OTRO", "SESION")]
    public async Task ListarBitacoraAsync_EventoDeSesion_SeClasificaPorTipoDeEvento(string resultadoSesion, string tipoEsperado)
    {
        PrepararBitacora(EventoSesion(resultadoSesion));

        var resultado = await CrearServicio(Superusuario())
            .ListarBitacoraAsync(Consulta(), CancellationToken.None);

        var item = Assert.Single(resultado.Items);
        Assert.Equal(tipoEsperado, item.TipoEvento);
    }

    [Fact]
    public async Task ListarBitacoraAsync_EventoDeSesion_ReemplazaElDetalleCrudoPorUnTextoEntendible()
    {
        PrepararBitacora(EventoSesion("EXITO"));

        var resultado = await CrearServicio(Superusuario())
            .ListarBitacoraAsync(Consulta(), CancellationToken.None);

        var item = Assert.Single(resultado.Items);
        Assert.Equal("Inició sesión en el sistema.", item.Detalle);
        Assert.NotEqual("detalle crudo", item.Detalle);
    }

    [Fact]
    public async Task ListarBitacoraAsync_EventoDeSesion_NoRepiteAlFuncionarioAfectado()
    {
        PrepararBitacora(EventoSesion("EXITO"));

        var resultado = await CrearServicio(Superusuario())
            .ListarBitacoraAsync(Consulta(), CancellationToken.None);

        var item = Assert.Single(resultado.Items);
        Assert.Equal(3, item.RealizadoPorId);
        Assert.Null(item.FuncionarioAfectadoId);
        Assert.Null(item.FuncionarioAfectadoNombre);
    }

    [Fact]
    public async Task ListarBitacoraAsync_FalloConUsuarioNoRegistrado_LoIndicaEnElDetalle()
    {
        PrepararBitacora(EventoSesion("FALLO", nombre: null, codigoUsuario: "jperez"));

        var resultado = await CrearServicio(Superusuario())
            .ListarBitacoraAsync(Consulta(), CancellationToken.None);

        var item = Assert.Single(resultado.Items);
        Assert.Contains("no registrado", item.Detalle);
    }

    [Fact]
    public async Task ListarBitacoraAsync_FalloConUsuarioRegistrado_IndicaUsuarioOClaveIncorrectos()
    {
        PrepararBitacora(EventoSesion("FALLO", nombre: "Valeria Mora"));

        var resultado = await CrearServicio(Superusuario())
            .ListarBitacoraAsync(Consulta(), CancellationToken.None);

        var item = Assert.Single(resultado.Items);
        Assert.Contains("usuario o clave incorrectos", item.Detalle);
    }

    [Fact]
    public async Task ListarBitacoraAsync_Superusuario_RecibeElDetalleTecnicoDeLaSesion()
    {
        PrepararBitacora(EventoSesion("FALLO", nombre: null, codigoUsuario: "jperez", ip: "10.0.0.8"));

        var resultado = await CrearServicio(Superusuario())
            .ListarBitacoraAsync(Consulta(), CancellationToken.None);

        var item = Assert.Single(resultado.Items);
        Assert.NotNull(item.DetalleTecnico);
        Assert.Contains("Usuario digitado: jperez", item.DetalleTecnico);
        Assert.Contains("Dirección IP: 10.0.0.8", item.DetalleTecnico);
    }

    [Fact]
    public async Task ListarBitacoraAsync_Jefe_NoRecibeElDetalleTecnicoDeLaSesion()
    {
        PrepararBitacora(EventoSesion("FALLO", nombre: null, codigoUsuario: "jperez", ip: "10.0.0.8"));

        var resultado = await CrearServicio(Jefe())
            .ListarBitacoraAsync(Consulta(), CancellationToken.None);

        var item = Assert.Single(resultado.Items);
        Assert.Null(item.DetalleTecnico);
    }

    [Theory]
    [InlineData("::1")]
    [InlineData("127.0.0.1")]
    [InlineData("::ffff:127.0.0.1")]
    public async Task ListarBitacoraAsync_IpLocal_SeMuestraComoAccesoLocal(string ip)
    {
        PrepararBitacora(EventoSesion("EXITO", ip: ip));

        var resultado = await CrearServicio(Superusuario())
            .ListarBitacoraAsync(Consulta(), CancellationToken.None);

        var item = Assert.Single(resultado.Items);
        Assert.Equal("Equipo: el mismo servidor (acceso local)", item.DetalleTecnico);
    }
}