using Microsoft.AspNetCore.Mvc;
using SDPIS.Api.Controllers;
using SDPIS.Api.Models.DTOs;
using SDPIS.Api.Repositories;
using SDPIS.Api.Services;
using SDPIS.Api.Services.Validation;
using SDPIS.Api.Tests.Services;
using Xunit;

namespace SDPIS.Api.Tests.Controllers;

// Bug reportado (HU-016, punto 2/3): el buscador "Ir a denuncia por N.º (id)"
// se oculta en el frontend para el rol funcionario (validador), pero esa
// restriccion es solo de UI -- nada impide que alguien reactive el campo
// (DevTools) o llame el endpoint directamente (Postman/curl) con el id de
// una denuncia ajena. Estas pruebas confirman que el rechazo no depende de
// que la pantalla esconda el campo: llaman el mismo metodo del controller
// que ASP.NET invoca para GET /api/denuncias/{id}/bitacora, sin pasar por
// ninguna pantalla, y verifican que el acceso se deniega igual.
//
// Complementan (no reemplazan) BitacoraServiceTests.
// ConsultarAsync_ConValidadorNoAsignado_LanzaExcepcionAccesoDenegado, que ya
// prueba la misma regla un nivel mas abajo (BitacoraService). Aqui se sube
// un nivel, hasta el controller, para dejar constancia de que no hay una
// ruta alterna sin proteger entre el HTTP publico y esa regla.
public class BitacoraControllerTests
{
    // HU-015: el controller ya no recibe el funcionarioId por parametro: lo toma
    // del token (IUsuarioActual). Aqui FakeUsuarioActual(100) simula esa sesion.
    [Fact]
    public async Task Consultar_ConFuncionarioQuePideUnaDenunciaAjena_LanzaExcepcionAccesoDenegado()
    {
        // Denuncia 9001 esta asignada al funcionario 101; el funcionario 100
        // (validador, "funcionario" en el lenguaje del reporte de bugs)
        // intenta consultarla igual, como si hubiera forzado el campo oculto.
        var repository = new FakeBitacoraRepository
        {
            FuncionarioARetornar = new FuncionarioAccesoInfo(100, "validador", AreaId: 5, Activo: true),
            DenunciaARetornar = new DenunciaAccesoInfo(9001, AreaActualId: 5, FuncionarioAsignadoId: 101),
        };
        var controller = new BitacoraController(
            new BitacoraService(repository, FakePermisosService.Desde(repository.FuncionarioARetornar)),
            new FakeUsuarioActual(100));

        // BitacoraAccesoDenegadoException, lanzada aqui, la traduce
        // ManejadorDeErroresMiddleware a HTTP 403 cuando la peticion llega
        // por la app real (ver Middlewares/ManejadorDeErroresMiddleware.cs).
        await Assert.ThrowsAsync<BitacoraAccesoDenegadoException>(() =>
            controller.Consultar(denunciaId: 9001, tipo: null, ct: CancellationToken.None));
    }

    // Contraparte positiva: el mismo funcionario SI puede consultar, por el
    // mismo camino, la denuncia que si tiene asignada -- confirma que la
    // regla filtra por asignacion y no bloquea a ciegas al rol validador.
    [Fact]
    public async Task Consultar_ConFuncionarioQuePideSuPropiaDenuncia_DevuelveLaPagina()
    {
        var repository = new FakeBitacoraRepository
        {
            FuncionarioARetornar = new FuncionarioAccesoInfo(100, "validador", AreaId: 5, Activo: true),
            DenunciaARetornar = new DenunciaAccesoInfo(9001, AreaActualId: 5, FuncionarioAsignadoId: 100),
        };
        var controller = new BitacoraController(
            new BitacoraService(repository, FakePermisosService.Desde(repository.FuncionarioARetornar)),
            new FakeUsuarioActual(100));

        var resultado = await controller.Consultar(denunciaId: 9001, tipo: null, ct: CancellationToken.None);

        // El action method devuelve Ok(resultado) explicito: en ActionResult<T>
        // eso deja el valor en .Result (como OkObjectResult), no en .Value
        // (que solo se llena con la conversion implicita T -> ActionResult<T>).
        var ok = Assert.IsType<OkObjectResult>(resultado.Result);
        Assert.IsType<BitacoraPaginaDto>(ok.Value);
    }
}