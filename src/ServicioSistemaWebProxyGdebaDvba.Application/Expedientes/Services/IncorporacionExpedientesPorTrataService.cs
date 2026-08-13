using Microsoft.Extensions.Logging;
using ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Gdeba;
using ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Persistence;
using ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Models;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.Auditoria.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.Auditoria.Models;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.ControlCuotas.Models;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.Seguridad.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities;
using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;
using ServicioSistemaWebProxyGdebaDvba.Domain.ValueObjects;
using URF.Core.Abstractions;
using URF.Core.Abstractions.Trackable;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Services;

public sealed class IncorporacionExpedientesPorTrataService : IIncorporacionExpedientesPorTrataService
{
    private const string OperacionIncorporarExpedientesPorTrata = "IncorporarExpedientesPorTrata";
    private const string OperacionBuscarDatosExpedientePorCodigosTrata = "buscarDatosExpedientePorCodigosTrata";

    private readonly IExpedienteCacheReadStore _expedienteCacheReadStore;
    private readonly IGdebaExpedienteGateway _gdebaExpedienteGateway;
    private readonly IGdebaExecutionContext _gdebaExecutionContext;
    private readonly IAuditoriaService _auditoriaService;
    private readonly ICurrentApplicationAccessor _currentApplicationAccessor;
    private readonly ITrackableRepository<Expediente> _expedienteRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<IncorporacionExpedientesPorTrataService> _logger;

    public IncorporacionExpedientesPorTrataService(
        IExpedienteCacheReadStore expedienteCacheReadStore,
        IGdebaExpedienteGateway gdebaExpedienteGateway,
        IGdebaExecutionContext gdebaExecutionContext,
        IAuditoriaService auditoriaService,
        ICurrentApplicationAccessor currentApplicationAccessor,
        ITrackableRepository<Expediente> expedienteRepository,
        IUnitOfWork unitOfWork,
        ILogger<IncorporacionExpedientesPorTrataService> logger)
    {
        _expedienteCacheReadStore = expedienteCacheReadStore;
        _gdebaExpedienteGateway = gdebaExpedienteGateway;
        _gdebaExecutionContext = gdebaExecutionContext;
        _auditoriaService = auditoriaService;
        _currentApplicationAccessor = currentApplicationAccessor;
        _expedienteRepository = expedienteRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IncorporarExpedientesPorTrataResult> PrepararAsync(
        IncorporarExpedientesPorTrataRequest request,
        CancellationToken cancellationToken)
    {
        var codigoTrata = IncorporacionExpedientesPorTrataService.NormalizarRequerido(request.CodigoTrata, nameof(request.CodigoTrata));
        var estadoDestino = IncorporacionExpedientesPorTrataService.NormalizarRequerido(request.EstadoDestino, nameof(request.EstadoDestino));
        var recurso = $"{codigoTrata}|{estadoDestino}";
        var resolvedAt = DateTimeOffset.Now;
        var trata = await _expedienteCacheReadStore.BuscarTrataPorCodigoAsync(codigoTrata, codigoReparticion: null, cancellationToken);
        if (trata is null)
        {
            throw new InvalidOperationException($"La trata '{codigoTrata}' no esta habilitada localmente.");
        }

        var codigosReparticionHabilitados = await _expedienteCacheReadStore.CargarCodigosReparticionHabilitadosAsync(cancellationToken);
        if (codigosReparticionHabilitados.Count == 0)
        {
            throw new InvalidOperationException("No hay reparticiones habilitadas localmente para incorporar expedientes por trata.");
        }

        IReadOnlyCollection<GdebaExpedientePorTrataDto> datosGdeba;
        try
        {
            datosGdeba = await _gdebaExpedienteGateway.BuscarDatosExpedientePorCodigosTrataAsync(
                codigoTrata, estadoDestino, usuario: null,
                ContextoInvocacionGdeba.Crear(request.OrigenInvocacion), cancellationToken);
        }
        catch (GdebaOperationException ex)
        {
            await this.RegistrarFalloGdebaAsync(recurso, ex.Message, resolvedAt, cancellationToken);
            throw;
        }
        catch (OperationCanceledException)
        {
            await this.RegistrarFalloGdebaAsync(recurso, "La consulta fue cancelada.", resolvedAt, CancellationToken.None);
            throw;
        }
        catch (Exception ex)
        {
            await this.RegistrarFalloGdebaAsync(recurso, ex.Message, resolvedAt, cancellationToken);
            throw new GdebaOperationException(OperacionBuscarDatosExpedientePorCodigosTrata, $"No se pudo ejecutar la operacion GDEBA: {ex.Message}", innerException: ex);
        }

        var expedientesDetectados = new List<(NumeroGdebaCompleto Numero, GdebaExpedientePorTrataDto Datos)>();
        var descartados = 0;
        foreach (var datos in datosGdeba)
        {
            try
            {
                var numero = NumeroGdebaCompleto.Create(datos.NumeroExpediente);
                if (!codigosReparticionHabilitados.Contains(numero.Reparticion))
                {
                    descartados++;
                    continue;
                }

                expedientesDetectados.Add((numero, datos));
            }
            catch (ArgumentException)
            {
                descartados++;
            }
        }

        var expedientesLocales = (await _expedienteCacheReadStore.CargarExpedientesPorNumeroAsync(
            expedientesDetectados.Select(x => x.Numero.Valor), cancellationToken))
            .ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);
        var creados = 0;
        var actualizados = 0;
        var sinCambios = 0;
        var expedientesNuevosIds = new List<Guid>();

        foreach (var (numero, datos) in expedientesDetectados)
        {
            var expedienteEsNuevo = !expedientesLocales.TryGetValue(numero.Valor, out var expediente);
            expediente ??= new Expediente(numero.Valor);
            var datosCambiaron = expediente.AplicarDatosDescubiertosPorTrata(trata.Id, datos.Estado);
            var cacheDebeRegistrarse = expedienteEsNuevo || datosCambiaron || expediente.CacheControl is null;

            if (cacheDebeRegistrarse)
            {
                expediente.RegistrarRespuestaExpedienteCorrecta(resolvedAt, resolvedAt, IncorporacionExpedientesPorTrataService.CalcularVencimientoDiario(resolvedAt), estaCompleto: false);
                this.RegistrarCambiosExpediente(expediente, expedienteEsNuevo);
            }

            if (expedienteEsNuevo)
            {
                creados++;
                expedientesNuevosIds.Add(expediente.Id);
                expedientesLocales[numero.Valor] = expediente;
            }
            else if (datosCambiaron)
            {
                actualizados++;
            }
            else
            {
                sinCambios++;
            }
        }

        await _auditoriaService.RegistrarAsync(
            new RegistrarAuditoriaRequest(
                _currentApplicationAccessor.Current.ApplicationId,
                OperacionIncorporarExpedientesPorTrata,
                OperacionBuscarDatosExpedientePorCodigosTrata,
                recurso,
                _gdebaExecutionContext.Ambiente,
                FuenteRespuesta.Gdeba,
                Exitoso: true,
                $"Recibidos: {datosGdeba.Count}. Habilitados: {expedientesDetectados.Count}. Descartados: {descartados}. Creados: {creados}. Actualizados: {actualizados}. Sin cambios: {sinCambios}.",
                resolvedAt),
            cancellationToken);

        return new IncorporarExpedientesPorTrataResult(
            codigoTrata, trata.Id, estadoDestino, resolvedAt, datosGdeba.Count, expedientesDetectados.Count, descartados,
            creados, actualizados, sinCambios, expedientesNuevosIds);
    }

    private void RegistrarCambiosExpediente(Expediente expediente, bool esNuevo)
    {
        if (esNuevo)
        {
            _expedienteRepository.Insert(expediente);
        }
        else
        {
            _expedienteRepository.Update(expediente);
        }

        _expedienteRepository.ApplyChanges(expediente);
    }

    private async Task RegistrarFalloGdebaAsync(string recurso, string mensaje, DateTimeOffset fecha, CancellationToken cancellationToken)
    {
        await _auditoriaService.RegistrarAsync(
            new RegistrarAuditoriaRequest(
                _currentApplicationAccessor.Current.ApplicationId,
                OperacionIncorporarExpedientesPorTrata,
                OperacionBuscarDatosExpedientePorCodigosTrata,
                recurso,
                _gdebaExecutionContext.Ambiente,
                FuenteRespuesta.Gdeba,
                Exitoso: false,
                mensaje,
                fecha),
            cancellationToken);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo registrar la auditoria del fallo GDEBA para {Recurso}.", recurso);
        }
    }

    private static string NormalizarRequerido(string? valor, string parameterName)
    {
        return string.IsNullOrWhiteSpace(valor)
            ? throw new ArgumentException("El valor es requerido.", parameterName)
            : valor.Trim();
    }

    private static DateTimeOffset CalcularVencimientoDiario(DateTimeOffset fechaConsulta)
    {
        return fechaConsulta.AddDays(1);
    }
}
