using SDPIS.Api.Models.DTOs;
using SDPIS.Api.Repositories;
using SDPIS.Api.Services.Validation;

namespace SDPIS.Api.Services;

public class DisponibilidadService(IDisponibilidadRepository repository) : IDisponibilidadService
{
    private static readonly System.Globalization.CultureInfo Cultura = System.Globalization.CultureInfo.InvariantCulture;

    public async Task<PeriodoIndisponibilidadDto> RegistrarPeriodoAsync(
        RegistrarIndisponibilidadRequest request, CancellationToken ct)
    {
        var fechaInicio = DisponibilidadValidator.ValidarYNormalizar(request);
        var motivo = request.Motivo.Trim().ToLowerInvariant();
        var fechaFin = request.FechaFinEstimada is { } f
            ? DateOnly.ParseExact(f, "yyyy-MM-dd", Cultura)
            : (DateOnly?)null;
        var registradoPor = string.IsNullOrWhiteSpace(request.RegistradoPor)
            ? null
            : request.RegistradoPor.Trim();
        var periodosExistentes = await repository.ObtenerPeriodosAsync(request.FuncionarioId, ct);
        if (HayTraslape(periodosExistentes, fechaInicio, fechaFin, idPeriodoExcluir: null))
        {
            throw new DenunciaValidationException(new[] {"Ya existe un periodo de indisponibilidad registrado que se traslapa con estas fechas."});
        }
        var registro = await repository.InsertarPeriodoAsync(
            request.FuncionarioId, motivo, fechaInicio, fechaFin, registradoPor, ct);

        return new PeriodoIndisponibilidadDto(
            registro.IdPeriodo,
            registro.FuncionarioId,
            registro.Motivo,
            registro.FechaInicio.ToString("yyyy-MM-dd", Cultura),
            registro.FechaFinEstimada?.ToString("yyyy-MM-dd", Cultura),
            EsVigente(registro, DateOnly.FromDateTime(DateTime.Today)));
    }

    public async Task<PeriodoIndisponibilidadDto> ActualizarPeriodoAsync(
        ActualizarIndisponibilidadRequest request, CancellationToken ct)
    {
        var fechaInicio = DisponibilidadValidator.ValidarYNormalizarActualizacion(request);
        var motivo = request.Motivo.Trim().ToLowerInvariant();
        var fechaFin = request.FechaFinEstimada is { } f
            ? DateOnly.ParseExact(f, "yyyy-MM-dd", Cultura)
            : (DateOnly?)null;
        var registradoPor = string.IsNullOrWhiteSpace(request.RegistradoPor)
            ? null
            : request.RegistradoPor.Trim();

        // Regla de negocio HU-007: un periodo finalizado (fecha fin ya pasó) no
        // se edita; el historial es una bitácora. Solo son editables los estados
        // programado y vigente.
        var hoy = DateOnly.FromDateTime(DateTime.Today);
        var periodos = await repository.ObtenerPeriodosAsync(request.FuncionarioId, ct);
        var periodoActual = periodos.FirstOrDefault(p => p.IdPeriodo == request.IdPeriodo);

        if (periodoActual is null)
        {
            throw new DenunciaBaseDatosException(
                "El periodo de indisponibilidad indicado no existe.",
                new InvalidOperationException($"No se encontro el periodo {request.IdPeriodo}."));
        }

        if (periodoActual.FechaFinEstimada is { } fin && fin < hoy)
        {
            throw new DenunciaBaseDatosException(
                "No se puede editar un periodo de indisponibilidad finalizado.",
                new InvalidOperationException($"El periodo {request.IdPeriodo} ya finalizo."));
        }

        if (HayTraslape(periodos, fechaInicio, fechaFin, request.IdPeriodo))
        {
            throw new DenunciaValidationException(new[] {"Ya existe un periodo de indisponibilidad registrado que se traslapa con estas fechas."});
        }

        var registro = await repository.ActualizarPeriodoAsync(
            request.IdPeriodo, request.FuncionarioId, motivo, fechaInicio, fechaFin, registradoPor, ct);

        return new PeriodoIndisponibilidadDto(
            registro.IdPeriodo,
            registro.FuncionarioId,
            registro.Motivo,
            registro.FechaInicio.ToString("yyyy-MM-dd", Cultura),
            registro.FechaFinEstimada?.ToString("yyyy-MM-dd", Cultura),
            EsVigente(registro, DateOnly.FromDateTime(DateTime.Today)));
    }

    public async Task EliminarPeriodoAsync(long idPeriodo, long funcionarioId, CancellationToken ct)
    {
        // Regla de negocio HU-007: un periodo finalizado (fecha fin ya pasó) no
        // se elimina; el historial es una bitácora. Solo son editables/eliminables
        // los estados programado y vigente.
        var hoy = DateOnly.FromDateTime(DateTime.Today);
        var periodos = await repository.ObtenerPeriodosAsync(funcionarioId, ct);
        var periodoActual = periodos.FirstOrDefault(p => p.IdPeriodo == idPeriodo);

        if (periodoActual is null)
        {
            throw new DenunciaBaseDatosException(
                "El periodo de indisponibilidad indicado no existe.",
                new InvalidOperationException($"No se encontro el periodo {idPeriodo}."));
        }

        if (periodoActual.FechaFinEstimada is { } fin && fin < hoy)
        {
            throw new DenunciaBaseDatosException(
                "No se puede eliminar un periodo de indisponibilidad finalizado.",
                new InvalidOperationException($"El periodo {idPeriodo} ya finalizo."));
        }

        await repository.EliminarPeriodoAsync(idPeriodo, funcionarioId, ct);
    }

       public async Task<List<FuncionarioDisponibilidadResumenDto>> ObtenerResumenFuncionariosAsync(long? areaId, CancellationToken ct)
    {
        var funcionarios = await repository.ObtenerFuncionariosAsync(areaId, ct);
        var periodos = await repository.ObtenerTodosLosPeriodosAsync(ct);
        var hoy = DateOnly.FromDateTime(DateTime.Today);

        // Carga activa: proviene de la asignacion de denuncias (HU-009/013).
        // Pendiente de integrar; se entrega null mientras esa fuente no exista.
        return funcionarios
            .Select(f =>
            {
                var delFuncionario = periodos.Where(p => p.FuncionarioId == f.IdFuncionario).ToList();
                var vigente = delFuncionario.FirstOrDefault(p => EsVigente(p, hoy));
                var tieneProximo = delFuncionario.Any(p => p.FechaInicio > hoy);   // <-- por funcionario, no global
                return new FuncionarioDisponibilidadResumenDto(
                    f.IdFuncionario,
                    f.Nombre,
                    f.Rol,
                    f.AreaNombre,
                    Disponible: vigente is null,
                    MotivoVigente: vigente?.Motivo,
                    FechaInicioVigente: vigente?.FechaInicio.ToString("yyyy-MM-dd", Cultura),
                    FechaFinVigente: vigente?.FechaFinEstimada?.ToString("yyyy-MM-dd", Cultura),
                    CargaActiva: null,
                    TieneProximo: tieneProximo);
            })
            .ToList();
    }

    public async Task<DisponibilidadFuncionarioDto> ObtenerDisponibilidadAsync(
        long funcionarioId, CancellationToken ct)
    {
        var info = await ValidarYUbicarFuncionarioAsync(funcionarioId, ct);

        var hoy = DateOnly.FromDateTime(DateTime.Today);
        var periodos = await repository.ObtenerPeriodosAsync(funcionarioId, ct);

        var historial = periodos
            .OrderByDescending(p => p.FechaInicio)
            .Select(p => new PeriodoIndisponibilidadHistorialDto(
                p.IdPeriodo,
                p.Motivo,
                p.FechaInicio.ToString("yyyy-MM-dd", Cultura),
                p.FechaFinEstimada?.ToString("yyyy-MM-dd", Cultura),
                Estado: CalcularEstado(p, hoy),
                p.RegistradoPor,
                p.FechaRegistro?.ToString("yyyy-MM-dd", Cultura)))
            .ToList();

        return new DisponibilidadFuncionarioDto(
            info.IdFuncionario,
            info.Nombre,
            info.Rol,
            info.AreaNombre,
            Disponible: !historial.Any(h => h.Estado == "vigente"),
            historial);
    }

    public async Task<bool> EsDisponibleAsync(long funcionarioId, DateOnly fechaCorte, CancellationToken ct)
    {
        await ValidarYUbicarFuncionarioAsync(funcionarioId, ct);

        var periodos = await repository.ObtenerPeriodosAsync(funcionarioId, ct);
        return !periodos.Any(p => EsVigente(p, fechaCorte));
    }

    private async Task<FuncionarioInfo> ValidarYUbicarFuncionarioAsync(long funcionarioId, CancellationToken ct)
    {
        if (funcionarioId <= 0)
        {
            throw new DenunciaBaseDatosException(
                "El funcionario indicado no existe.",
                new InvalidOperationException($"Funcionario {funcionarioId} no encontrado."));
        }

        var info = (await repository.ObtenerFuncionariosAsync(null, ct))
        .FirstOrDefault(f => f.IdFuncionario == funcionarioId);

        if (info is null)
        {
            throw new DenunciaBaseDatosException(
                "El funcionario indicado no existe.",
                new InvalidOperationException($"Funcionario {funcionarioId} no encontrado."));
        }

        return info;
    }

    // Regla de negocio: un periodo esta vigente si la fecha de corte cae
    // dentro de [fecha_inicio, fecha_fin_estimada]. Si fecha_fin_estimada
    // es NULL, la indisponibilidad es indefinida desde fecha_inicio.
    private static bool EsVigente(PeriodoIndisponibilidadRegistro periodo, DateOnly fechaCorte)
        => periodo.FechaInicio <= fechaCorte
           && (periodo.FechaFinEstimada is null || periodo.FechaFinEstimada >= fechaCorte);

    // Estado mostrado en el historial (mockup HU-007):
    // programado -> la fecha de inicio aun no llega.
    // vigente    -> la fecha de corte cae dentro del periodo.
    // finalizado -> el periodo termino (fecha fin ya paso).
    private static string CalcularEstado(PeriodoIndisponibilidadRegistro periodo, DateOnly hoy)
    {
        if (periodo.FechaInicio > hoy)
            return "programado";

        if (periodo.FechaFinEstimada is { } fin && fin < hoy)
            return "finalizado";

        return "vigente";
    }

    private static bool HayTraslape(
    IEnumerable<PeriodoIndisponibilidadRegistro> periodosExistentes,
    DateOnly nuevoInicio, DateOnly? nuevoFin, long? idPeriodoExcluir)
    {
        var finComparable = nuevoFin ?? DateOnly.MaxValue;
        return periodosExistentes.Any(p =>
            p.IdPeriodo != idPeriodoExcluir &&
            nuevoInicio <= (p.FechaFinEstimada ?? DateOnly.MaxValue) &&
            finComparable >= p.FechaInicio);
    }

}