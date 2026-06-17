using ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Gdeba;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.ControlCuotas.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.ControlCuotas.Models;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities;
using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;
using URF.Core.Abstractions;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Transversales.ControlCuotas.Services;

internal sealed class ConsultaCuotasGdeba : IConsultaCuotasGdeba
{
    private readonly IRepository<OperacionGdeba> _operacionRepository;
    private readonly IRepository<InvocacionGdeba> _invocacionRepository;
    private readonly IGdebaExecutionContext _executionContext;

    public ConsultaCuotasGdeba(
        IRepository<OperacionGdeba> operacionRepository,
        IRepository<InvocacionGdeba> invocacionRepository,
        IGdebaExecutionContext executionContext)
    {
        _operacionRepository = operacionRepository;
        _invocacionRepository = invocacionRepository;
        _executionContext = executionContext;
    }

    public async Task<ConsultaCuotasGdebaResult> ConsultarCuotasAsync(DateOnly fecha, CancellationToken cancellationToken)
    {
        var inicio = ConsultaCuotasGdeba.CrearInicioDelDia(fecha);
        var fin = inicio.AddDays(1);
        var ambiente = _executionContext.Ambiente;

        var operaciones = (await _operacionRepository.Query()
                .Where(x => x.Activa)
                .OrderBy(x => x.Servicio)
                .ThenBy(x => x.Metodo)
                .SelectAsync(cancellationToken))
            .ToArray();

        var invocaciones = (await _invocacionRepository.Query()
                .Where(x =>
                    x.Ambiente == ambiente &&
                    x.ServidorRespondio &&
                    x.Fecha >= inicio &&
                    x.Fecha < fin)
                .SelectAsync(cancellationToken))
            .ToArray();

        var consumosPorOperacion = invocaciones
            .GroupBy(x => x.OperacionId)
            .ToDictionary(x => x.Key, x => x.ToArray());

        var filas = operaciones
            .Select(operacion =>
                ConsultaCuotasGdeba.CrearFila(
                    operacion,
                    consumosPorOperacion.GetValueOrDefault(operacion.Id) ??
                    Array.Empty<InvocacionGdeba>()))
            .ToArray();

        var totalHistorico = await _invocacionRepository.Query()
            .Where(x => x.Ambiente == ambiente && x.ServidorRespondio)
            .CountAsync(cancellationToken);

        return new ConsultaCuotasGdebaResult(
            fecha,
            ambiente,
            filas,
            ConsultaCuotasGdeba.CrearTotales(filas),
            totalHistorico);
    }

    private static ConsumoCuotaOperacionGdebaDto CrearFila(
        OperacionGdeba operacion,
        IReadOnlyCollection<InvocacionGdeba> invocaciones)
    {
        var interactiva = ConsultaCuotasGdeba.Contar(invocaciones, OrigenInvocacionGdeba.Interactiva);
        var refrescoManual = ConsultaCuotasGdeba.Contar(invocaciones, OrigenInvocacionGdeba.RefrescoManual);
        var workerProgramado = ConsultaCuotasGdeba.Contar(invocaciones, OrigenInvocacionGdeba.WorkerProgramado);
        var mensajeria = ConsultaCuotasGdeba.Contar(invocaciones, OrigenInvocacionGdeba.Mensajeria);
        var administrativo = ConsultaCuotasGdeba.Contar(invocaciones, OrigenInvocacionGdeba.Administrativo);
        var noDeterminado = ConsultaCuotasGdeba.Contar(invocaciones, OrigenInvocacionGdeba.NoDeterminado);
        var total = invocaciones.Count;
        var porcentaje = ConsultaCuotasGdeba.CalcularPorcentaje(total, operacion.LimiteDiario);

        return new ConsumoCuotaOperacionGdebaDto(
            operacion.Servicio,
            operacion.Metodo,
            interactiva,
            refrescoManual,
            workerProgramado,
            mensajeria,
            administrativo,
            noDeterminado,
            total,
            operacion.LimiteDiario,
            porcentaje,
            ConsultaCuotasGdeba.ResolverEstado(
                total,
                operacion.LimiteDiario,
                porcentaje,
                operacion.UmbralAdvertenciaPorcentaje,
                operacion.UmbralCriticoPorcentaje));
    }

    private static TotalesConsumoCuotasGdebaDto CrearTotales(
        IReadOnlyCollection<ConsumoCuotaOperacionGdebaDto> filas)
    {
        int? limiteTotal = filas.Any(x => x.LimiteDiario.HasValue)
            ? filas.Sum(x => x.LimiteDiario ?? 0)
            : null;
        var total = filas.Sum(x => x.Total);

        return new TotalesConsumoCuotasGdebaDto(
            filas.Sum(x => x.Interactiva),
            filas.Sum(x => x.RefrescoManual),
            filas.Sum(x => x.WorkerProgramado),
            filas.Sum(x => x.Mensajeria),
            filas.Sum(x => x.Administrativo),
            filas.Sum(x => x.NoDeterminado),
            total,
            limiteTotal,
            ConsultaCuotasGdeba.CalcularPorcentaje(total, limiteTotal));
    }

    private static int Contar(
        IEnumerable<InvocacionGdeba> invocaciones,
        OrigenInvocacionGdeba origen)
    {
        return invocaciones.Count(x => x.Origen == origen);
    }

    private static decimal? CalcularPorcentaje(int total, int? limiteDiario)
    {
        return limiteDiario is > 0
            ? Math.Round(total * 100m / limiteDiario.Value, 2)
            : null;
    }

    private static string ResolverEstado(
        int total,
        int? limiteDiario,
        decimal? porcentaje,
        decimal umbralAdvertencia,
        decimal umbralCritico)
    {
        if (limiteDiario is null or <= 0 || porcentaje is null)
        {
            return "SinLimiteConfigurado";
        }

        if (total > limiteDiario.Value)
        {
            return "Superada";
        }

        if (porcentaje >= umbralCritico)
        {
            return "Critica";
        }

        return porcentaje >= umbralAdvertencia
            ? "Advertencia"
            : "Normal";
    }

    private static DateTimeOffset CrearInicioDelDia(DateOnly fecha)
    {
        return new DateTimeOffset(
            fecha.ToDateTime(TimeOnly.MinValue),
            DateTimeOffset.Now.Offset);
    }
}
