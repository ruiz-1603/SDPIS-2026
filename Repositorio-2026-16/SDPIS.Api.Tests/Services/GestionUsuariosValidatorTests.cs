using SDPIS.Api.Models.DTOs;
using SDPIS.Api.Services.Autorizacion;
using SDPIS.Api.Services.Validation;

namespace SDPIS.Api.Tests.Services;

// HU-015: validacion de FORMA de las solicitudes de gestion de usuarios.
// GestionUsuariosValidator es logica pura (estatica): no necesita mocks ni BD.
public class GestionUsuariosValidatorTests
{
    private static CrearFuncionarioRequest CrearValido() => new(
        "Valeria Mora Chaves", "valeria.mora@sdpis.go.cr", Roles.Validador, 5, null);

    private static void AssertError(DenunciaValidationException excepcion, string fragmento)
        => Assert.Contains(excepcion.Errores,
            e => e.Contains(fragmento, StringComparison.OrdinalIgnoreCase));

    // =====================================================
    // ValidarCrear
    // =====================================================

    [Fact]
    public void ValidarCrear_SolicitudValida_NoLanzaExcepcion()
    {
        var excepcion = Record.Exception(() => GestionUsuariosValidator.ValidarCrear(CrearValido()));

        Assert.Null(excepcion);
    }

    [Fact]
    public void ValidarCrear_SolicitudNula_LanzaErrorDeSolicitudVacia()
    {
        var excepcion = Assert.Throws<DenunciaValidationException>(
            () => GestionUsuariosValidator.ValidarCrear(null));

        AssertError(excepcion, "vacia");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("12345")]
    [InlineData("!!!")]
    public void ValidarCrear_NombreSinLetras_LanzaErrorDeNombre(string nombre)
    {
        var request = CrearValido() with { Nombre = nombre };

        var excepcion = Assert.Throws<DenunciaValidationException>(
            () => GestionUsuariosValidator.ValidarCrear(request));

        AssertError(excepcion, "nombre");
    }

    [Fact]
    public void ValidarCrear_NombreMayorA150Caracteres_LanzaErrorDeNombre()
    {
        var request = CrearValido() with { Nombre = new string('a', 151) };

        var excepcion = Assert.Throws<DenunciaValidationException>(
            () => GestionUsuariosValidator.ValidarCrear(request));

        AssertError(excepcion, "150 caracteres");
    }

    [Theory]
    [InlineData("")]
    [InlineData("sin-arroba.go.cr")]
    [InlineData("usuario@sindominio")]
    [InlineData("con espacio@sdpis.go.cr")]
    public void ValidarCrear_CorreoInvalido_LanzaErrorDeCorreo(string correo)
    {
        var request = CrearValido() with { Correo = correo };

        var excepcion = Assert.Throws<DenunciaValidationException>(
            () => GestionUsuariosValidator.ValidarCrear(request));

        AssertError(excepcion, "correo");
    }

    [Fact]
    public void ValidarCrear_CorreoMayorA150Caracteres_LanzaErrorDeCorreo()
    {
        var request = CrearValido() with { Correo = new string('a', 150) + "@sdpis.go.cr" };

        var excepcion = Assert.Throws<DenunciaValidationException>(
            () => GestionUsuariosValidator.ValidarCrear(request));

        AssertError(excepcion, "correo");
    }

    [Theory]
    [InlineData("administrador")]
    [InlineData("")]
    [InlineData("JEFE_AREA")]
    public void ValidarCrear_RolFueraDelCatalogo_LanzaErrorDeRol(string rol)
    {
        var request = CrearValido() with { Rol = rol };

        var excepcion = Assert.Throws<DenunciaValidationException>(
            () => GestionUsuariosValidator.ValidarCrear(request));

        AssertError(excepcion, "rol");
    }

    [Theory]
    [InlineData(Roles.Validador)]
    [InlineData(Roles.JefeArea)]
    [InlineData(Roles.Superusuario)]
    public void ValidarCrear_TodosLosRolesDelCatalogo_SonAceptados(string rol)
    {
        var request = CrearValido() with { Rol = rol };

        var excepcion = Record.Exception(() => GestionUsuariosValidator.ValidarCrear(request));

        Assert.Null(excepcion);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ValidarCrear_AreaNoPositiva_LanzaErrorDeArea(long areaId)
    {
        var request = CrearValido() with { AreaId = areaId };

        var excepcion = Assert.Throws<DenunciaValidationException>(
            () => GestionUsuariosValidator.ValidarCrear(request));

        AssertError(excepcion, "area valida");
    }

    [Fact]
    public void ValidarCrear_EspecialidadMayorA100Caracteres_LanzaErrorDeEspecialidad()
    {
        var request = CrearValido() with { Especialidad = new string('e', 101) };

        var excepcion = Assert.Throws<DenunciaValidationException>(
            () => GestionUsuariosValidator.ValidarCrear(request));

        AssertError(excepcion, "especialidad");
    }

    [Fact]
    public void ValidarCrear_VariosCamposInvalidos_ReportaTodosLosErroresJuntos()
    {
        var request = new CrearFuncionarioRequest("", "mal", "otro", 0, null);

        var excepcion = Assert.Throws<DenunciaValidationException>(
            () => GestionUsuariosValidator.ValidarCrear(request));

        Assert.Equal(4, excepcion.Errores.Count);
    }

    // =====================================================
    // ValidarActualizar
    // =====================================================

    [Fact]
    public void ValidarActualizar_SoloCambiaElEstado_NoLanzaExcepcion()
    {
        var request = new ActualizarFuncionarioRequest(null, false, null);

        var excepcion = Record.Exception(() => GestionUsuariosValidator.ValidarActualizar(3, request));

        Assert.Null(excepcion);
    }

    [Fact]
    public void ValidarActualizar_EspecialidadVaciaParaQuitarla_NoLanzaExcepcion()
    {
        // "" = quitar la especialidad (distinto de null = sin cambio).
        var request = new ActualizarFuncionarioRequest(null, null, "");

        var excepcion = Record.Exception(() => GestionUsuariosValidator.ValidarActualizar(3, request));

        Assert.Null(excepcion);
    }

    [Fact]
    public void ValidarActualizar_IdNoPositivo_LanzaErrorDeFuncionario()
    {
        var request = new ActualizarFuncionarioRequest(null, true, null);

        var excepcion = Assert.Throws<DenunciaValidationException>(
            () => GestionUsuariosValidator.ValidarActualizar(0, request));

        AssertError(excepcion, "funcionario valido");
    }

    [Fact]
    public void ValidarActualizar_SolicitudNula_ExigeAlMenosUnCambio()
    {
        var excepcion = Assert.Throws<DenunciaValidationException>(
            () => GestionUsuariosValidator.ValidarActualizar(3, null));

        AssertError(excepcion, "al menos un cambio");
    }

    [Fact]
    public void ValidarActualizar_SinNingunCambio_ExigeAlMenosUnCambio()
    {
        var request = new ActualizarFuncionarioRequest(null, null, null);

        var excepcion = Assert.Throws<DenunciaValidationException>(
            () => GestionUsuariosValidator.ValidarActualizar(3, request));

        AssertError(excepcion, "al menos un cambio");
    }

    [Theory]
    [InlineData(Roles.Validador)]
    [InlineData(Roles.JefeArea)]
    public void ValidarActualizar_RolFuncionarioOJefe_EsAceptado(string rol)
    {
        var request = new ActualizarFuncionarioRequest(rol, null, null);

        var excepcion = Record.Exception(() => GestionUsuariosValidator.ValidarActualizar(3, request));

        Assert.Null(excepcion);
    }

    [Fact]
    public void ValidarActualizar_RolSuperusuario_SeRechazaPorEstaVia()
    {
        // Un superusuario no se crea ni se asigna con "actualizar".
        var request = new ActualizarFuncionarioRequest(Roles.Superusuario, null, null);

        var excepcion = Assert.Throws<DenunciaValidationException>(
            () => GestionUsuariosValidator.ValidarActualizar(3, request));

        AssertError(excepcion, "solo se permite el rol");
    }

    [Fact]
    public void ValidarActualizar_CorreoInvalido_LanzaErrorDeCorreo()
    {
        var request = new ActualizarFuncionarioRequest(null, null, null, Correo: "sin-arroba");

        var excepcion = Assert.Throws<DenunciaValidationException>(
            () => GestionUsuariosValidator.ValidarActualizar(3, request));

        AssertError(excepcion, "correo");
    }

    [Fact]
    public void ValidarActualizar_NombreSinLetras_LanzaErrorDeNombre()
    {
        var request = new ActualizarFuncionarioRequest(null, null, null, Nombre: "1234");

        var excepcion = Assert.Throws<DenunciaValidationException>(
            () => GestionUsuariosValidator.ValidarActualizar(3, request));

        AssertError(excepcion, "nombre");
    }

    // =====================================================
    // ValidarPermiso
    // =====================================================

    [Theory]
    [InlineData("OTORGAR")]
    [InlineData("REVOCAR")]
    [InlineData("otorgar")]
    [InlineData(" revocar ")]
    public void ValidarPermiso_AccionValida_NoLanzaExcepcion(string accion)
    {
        var request = new GestionarPermisoRequest(Permisos.VerFuncionariosArea, accion);

        var excepcion = Record.Exception(() => GestionUsuariosValidator.ValidarPermiso(3, request));

        Assert.Null(excepcion);
    }

    [Theory]
    [InlineData(Permisos.AdministrarSistema)]
    [InlineData(Permisos.DelegarPermisosArea)]
    [InlineData(Permisos.AprobarCierre)]
    [InlineData("PERMISO_INVENTADO")]
    public void ValidarPermiso_PermisoNoDelegable_LanzaErrorDePermiso(string codigo)
    {
        var request = new GestionarPermisoRequest(codigo, "OTORGAR");

        var excepcion = Assert.Throws<DenunciaValidationException>(
            () => GestionUsuariosValidator.ValidarPermiso(3, request));

        AssertError(excepcion, "no es delegable");
    }

    [Fact]
    public void ValidarPermiso_CodigoEnBlanco_LanzaErrorDePermisoObligatorio()
    {
        var request = new GestionarPermisoRequest("   ", "OTORGAR");

        var excepcion = Assert.Throws<DenunciaValidationException>(
            () => GestionUsuariosValidator.ValidarPermiso(3, request));

        AssertError(excepcion, "Debe indicar el permiso");
    }

    [Theory]
    [InlineData("BORRAR")]
    [InlineData("")]
    public void ValidarPermiso_AccionInvalida_LanzaErrorDeAccion(string accion)
    {
        var request = new GestionarPermisoRequest(Permisos.VerFuncionariosArea, accion);

        var excepcion = Assert.Throws<DenunciaValidationException>(
            () => GestionUsuariosValidator.ValidarPermiso(3, request));

        AssertError(excepcion, "OTORGAR o REVOCAR");
    }

    [Fact]
    public void ValidarPermiso_IdNoPositivo_LanzaErrorDeFuncionario()
    {
        var request = new GestionarPermisoRequest(Permisos.VerFuncionariosArea, "OTORGAR");

        var excepcion = Assert.Throws<DenunciaValidationException>(
            () => GestionUsuariosValidator.ValidarPermiso(0, request));

        AssertError(excepcion, "funcionario valido");
    }

    // =====================================================
    // ValidarMaxJefes
    // =====================================================

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(GestionUsuariosValidator.MaxJefesTope)]
    public void ValidarMaxJefes_CupoDentroDelRango_NoLanzaExcepcion(int maxJefes)
    {
        var excepcion = Record.Exception(
            () => GestionUsuariosValidator.ValidarMaxJefes(5, new ConfigurarMaxJefesRequest(maxJefes)));

        Assert.Null(excepcion);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(GestionUsuariosValidator.MaxJefesTope + 1)]
    public void ValidarMaxJefes_CupoFueraDelRango_LanzaErrorDeCupo(int maxJefes)
    {
        var excepcion = Assert.Throws<DenunciaValidationException>(
            () => GestionUsuariosValidator.ValidarMaxJefes(5, new ConfigurarMaxJefesRequest(maxJefes)));

        AssertError(excepcion, "cupo de jefes");
    }

    [Fact]
    public void ValidarMaxJefes_AreaNoPositiva_LanzaErrorDeArea()
    {
        var excepcion = Assert.Throws<DenunciaValidationException>(
            () => GestionUsuariosValidator.ValidarMaxJefes(0, new ConfigurarMaxJefesRequest(2)));

        AssertError(excepcion, "area valida");
    }

    // =====================================================
    // ValidarFiltroDenuncias
    // =====================================================

    [Theory]
    [InlineData("TODAS")]
    [InlineData("POR_ASIGNAR")]
    [InlineData("ASIGNADAS")]
    public void ValidarFiltroDenuncias_FiltroValido_NoLanzaExcepcion(string filtro)
    {
        var excepcion = Record.Exception(() => GestionUsuariosValidator.ValidarFiltroDenuncias(filtro));

        Assert.Null(excepcion);
    }

    [Theory]
    [InlineData("todas")]
    [InlineData("")]
    [InlineData("OTRO")]
    public void ValidarFiltroDenuncias_FiltroInvalido_LanzaErrorDeFiltro(string filtro)
    {
        // La comparacion es exacta: "todas" en minuscula no es valido.
        var excepcion = Assert.Throws<DenunciaValidationException>(
            () => GestionUsuariosValidator.ValidarFiltroDenuncias(filtro));

        AssertError(excepcion, "filtro");
    }

    // =====================================================
    // ValidarConsultaBitacora
    // =====================================================

    [Fact]
    public void ValidarConsultaBitacora_ParametrosPorDefecto_NoLanzaExcepcion()
    {
        var excepcion = Record.Exception(
            () => GestionUsuariosValidator.ValidarConsultaBitacora(null, 1, 20));

        Assert.Null(excepcion);
    }

    [Theory]
    [InlineData("FUNCIONARIOS")]
    [InlineData("PERMISOS")]
    [InlineData("CUPOS")]
    [InlineData("CONSULTA")]
    [InlineData("CRITERIOS")]
    [InlineData("PLAZOS")]
    [InlineData("GESTION_TERRITORIAL")]
    [InlineData("SESION")]
    public void ValidarConsultaBitacora_CategoriaDelCatalogo_NoLanzaExcepcion(string categoria)
    {
        var excepcion = Record.Exception(
            () => GestionUsuariosValidator.ValidarConsultaBitacora(categoria, 1, 20));

        Assert.Null(excepcion);
    }

    [Fact]
    public void ValidarConsultaBitacora_CategoriaInexistente_LanzaErrorDeCategoria()
    {
        var excepcion = Assert.Throws<DenunciaValidationException>(
            () => GestionUsuariosValidator.ValidarConsultaBitacora("INVENTADA", 1, 20));

        AssertError(excepcion, "categoria");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ValidarConsultaBitacora_PaginaMenorA1_LanzaErrorDePagina(int pagina)
    {
        var excepcion = Assert.Throws<DenunciaValidationException>(
            () => GestionUsuariosValidator.ValidarConsultaBitacora(null, pagina, 20));

        AssertError(excepcion, "pagina debe ser");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(GestionUsuariosValidator.TamanoPaginaMaximo + 1)]
    public void ValidarConsultaBitacora_TamanoFueraDelRango_LanzaErrorDeTamano(int tamano)
    {
        var excepcion = Assert.Throws<DenunciaValidationException>(
            () => GestionUsuariosValidator.ValidarConsultaBitacora(null, 1, tamano));

        AssertError(excepcion, "tamano de pagina");
    }

    [Fact]
    public void ValidarConsultaBitacora_TamanoIgualAlMaximo_NoLanzaExcepcion()
    {
        var excepcion = Record.Exception(() => GestionUsuariosValidator.ValidarConsultaBitacora(
            null, 1, GestionUsuariosValidator.TamanoPaginaMaximo));

        Assert.Null(excepcion);
    }

    [Fact]
    public void ValidarConsultaBitacora_FechaInicialPosteriorALaFinal_LanzaErrorDeFechas()
    {
        var desde = new DateTime(2026, 10, 10);
        var hasta = new DateTime(2026, 10, 1);

        var excepcion = Assert.Throws<DenunciaValidationException>(
            () => GestionUsuariosValidator.ValidarConsultaBitacora(null, 1, 20, desde, hasta));

        AssertError(excepcion, "fecha inicial");
    }

    [Fact]
    public void ValidarConsultaBitacora_MismoDiaConDistintaHora_NoLanzaExcepcion()
    {
        // Solo se compara la fecha, no la hora.
        var desde = new DateTime(2026, 10, 5, 18, 0, 0);
        var hasta = new DateTime(2026, 10, 5, 8, 0, 0);

        var excepcion = Record.Exception(
            () => GestionUsuariosValidator.ValidarConsultaBitacora(null, 1, 20, desde, hasta));

        Assert.Null(excepcion);
    }

    [Fact]
    public void ValidarConsultaBitacora_TextoMayorA100Caracteres_LanzaErrorDeBusqueda()
    {
        var excepcion = Assert.Throws<DenunciaValidationException>(
            () => GestionUsuariosValidator.ValidarConsultaBitacora(
                null, 1, 20, texto: new string('x', 101)));

        AssertError(excepcion, "busqueda");
    }

    [Fact]
    public void ValidarConsultaBitacora_TextoDe100Caracteres_NoLanzaExcepcion()
    {
        var excepcion = Record.Exception(
            () => GestionUsuariosValidator.ValidarConsultaBitacora(
                null, 1, 20, texto: new string('x', 100)));

        Assert.Null(excepcion);
    }

    [Fact]
    public void ValidarConsultaBitacora_PaginaYTamanoInvalidos_ReportaAmbosErrores()
    {
        var excepcion = Assert.Throws<DenunciaValidationException>(
            () => GestionUsuariosValidator.ValidarConsultaBitacora(null, 0, 0));

        Assert.Equal(2, excepcion.Errores.Count);
    }

    // =====================================================
    // ValidarRolFiltro
    // =====================================================

    [Fact]
    public void ValidarRolFiltro_SinRol_NoLanzaExcepcion()
    {
        var excepcion = Record.Exception(() => GestionUsuariosValidator.ValidarRolFiltro(null));

        Assert.Null(excepcion);
    }

    [Theory]
    [InlineData(Roles.Validador)]
    [InlineData(Roles.JefeArea)]
    [InlineData(Roles.Superusuario)]
    public void ValidarRolFiltro_RolDelCatalogo_NoLanzaExcepcion(string rol)
    {
        var excepcion = Record.Exception(() => GestionUsuariosValidator.ValidarRolFiltro(rol));

        Assert.Null(excepcion);
    }

    [Fact]
    public void ValidarRolFiltro_RolInexistente_LanzaErrorDeRol()
    {
        var excepcion = Assert.Throws<DenunciaValidationException>(
            () => GestionUsuariosValidator.ValidarRolFiltro("administrador"));

        AssertError(excepcion, "rol indicado");
    }

    // =====================================================
    // ValidarCredencialTemporal (TEMPORAL: se elimina al integrar MSI)
    // =====================================================

    private static CrearCredencialTemporalRequest CredencialValida() => new(
        "1-0234-0567", "Valeria", "Mora", null, "Segura#Clave2026");

    [Fact]
    public void ValidarCredencialTemporal_SolicitudValida_NoLanzaExcepcion()
    {
        var excepcion = Record.Exception(
            () => GestionUsuariosValidator.ValidarCredencialTemporal(3, CredencialValida()));

        Assert.Null(excepcion);
    }

    [Theory]
    [InlineData("corta1A#")]               // menos de 12 caracteres
    [InlineData("sinmayuscula#2026")]
    [InlineData("SINMINUSCULA#2026")]
    [InlineData("SinNumeros#Clave")]
    [InlineData("SinSimbolo2026Abc")]
    public void ValidarCredencialTemporal_ClaveDebil_LanzaErrorDeClave(string clave)
    {
        var request = CredencialValida() with { Clave = clave };

        var excepcion = Assert.Throws<DenunciaValidationException>(
            () => GestionUsuariosValidator.ValidarCredencialTemporal(3, request));

        AssertError(excepcion, "clave temporal");
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("con espacios")]
    [InlineData("")]
    public void ValidarCredencialTemporal_IdentificacionInvalida_LanzaErrorDeIdentificacion(string identificacion)
    {
        var request = CredencialValida() with { Identificacion = identificacion };

        var excepcion = Assert.Throws<DenunciaValidationException>(
            () => GestionUsuariosValidator.ValidarCredencialTemporal(3, request));

        AssertError(excepcion, "identificacion");
    }

    [Fact]
    public void ValidarCredencialTemporal_IdDeFuncionarioNoPositivo_LanzaErrorDeFuncionario()
    {
        var excepcion = Assert.Throws<DenunciaValidationException>(
            () => GestionUsuariosValidator.ValidarCredencialTemporal(0, CredencialValida()));

        AssertError(excepcion, "funcionario valido");
    }
}