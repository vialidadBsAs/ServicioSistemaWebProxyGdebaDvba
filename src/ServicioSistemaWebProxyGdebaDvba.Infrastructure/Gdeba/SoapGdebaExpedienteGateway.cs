using System.Globalization;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Gdeba;
using ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Models;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.ControlCuotas.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.ControlCuotas.Models;
using ServicioSistemaWebProxyGdebaDvba.Domain.ValueObjects;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Gdeba;

public sealed class SoapGdebaExpedienteGateway : IGdebaExpedienteGateway
{
    private const string ServicioConsultaExpediente = "ws_gdeba_consultaExpediente";

    private readonly HttpClient _httpClient;
    private readonly IGdebaJwtTokenProvider _tokenProvider;
    private readonly IRegistroInvocacionesGdeba _registroInvocaciones;
    private readonly IOptions<GdebaOptions> _options;
    private readonly ILogger<SoapGdebaExpedienteGateway> _logger;

    public SoapGdebaExpedienteGateway(
        HttpClient httpClient,
        IGdebaJwtTokenProvider tokenProvider,
        IRegistroInvocacionesGdeba registroInvocaciones,
        IOptions<GdebaOptions> options,
        ILogger<SoapGdebaExpedienteGateway> logger)
    {
        _httpClient = httpClient;
        _tokenProvider = tokenProvider;
        _registroInvocaciones = registroInvocaciones;
        _options = options;
        _logger = logger;
    }

    public Task<ExpedienteGdebaDto?> BuscarExpedienteAsync(
        NumeroGdebaCompleto numeroGdebaCompleto,
        ContextoInvocacionGdeba contextoInvocacion,
        CancellationToken cancellationToken)
    {
        return this.BuscarExpedienteDesdeDetalleAsync(
            numeroGdebaCompleto,
            contextoInvocacion,
            cancellationToken);
    }

    private async Task<ExpedienteGdebaDto?> BuscarExpedienteDesdeDetalleAsync(
        NumeroGdebaCompleto numeroGdebaCompleto,
        ContextoInvocacionGdeba contextoInvocacion,
        CancellationToken cancellationToken)
    {
        var detalle = await ConsultarExpedienteDetalladoAsync(
            numeroGdebaCompleto,
            contextoInvocacion,
            cancellationToken);
        return detalle is null
            ? null
            : new ExpedienteGdebaDto(
                detalle.NumeroGdebaCompleto,
                detalle.CodigoTrata,
                detalle.DescripcionTrata,
                detalle.Estado);
    }

    public async Task<GdebaExpedienteDetalladoDto?> ConsultarExpedienteDetalladoAsync(
        NumeroGdebaCompleto numeroGdebaCompleto,
        ContextoInvocacionGdeba contextoInvocacion,
        CancellationToken cancellationToken)
    {
        var contractOptions = this.ResolveSoapContractOptions();
        var serviceContractOptions = SoapGdebaExpedienteGateway.ResolveConsultaExpedienteServiceContractOptions(contractOptions);
        var serviceOptions = this.ResolveConsultaExpedienteServiceOptions();
        var operationName = "consultarExpedienteDetallado";
        var envelope = SoapGdebaExpedienteGateway.BuildEnvelope(
            contractOptions,
            serviceContractOptions,
            operationName,
            numeroGdebaCompleto);

        var document = await this.SendSoapAsync(
            serviceOptions,
            SoapGdebaExpedienteGateway.ResolveSoapOperationContractOptions(serviceContractOptions, operationName),
            operationName,
            envelope,
            contextoInvocacion,
            cancellationToken);
        var response = SoapGdebaExpedienteGateway.FindFirstElement(document, "response");
        if (response is null)
        {
            return null;
        }

        var relaciones = new List<GdebaRelacionExpedienteDto>();
        relaciones.AddRange(SoapGdebaExpedienteGateway.MapRelaciones(response, "listaExpedientesAsociados", "Asociado"));
        relaciones.AddRange(SoapGdebaExpedienteGateway.MapRelaciones(response, "listaExpedientesAsociadosFusion", "Fusion"));
        relaciones.AddRange(SoapGdebaExpedienteGateway.MapRelaciones(response, "listaExpedientesAsociadosTC", "TramitacionConjunta"));

        return new GdebaExpedienteDetalladoDto(
            SoapGdebaExpedienteGateway.GetValue(response, "numeroExpediente") ?? numeroGdebaCompleto.Valor,
            SoapGdebaExpedienteGateway.GetValue(response, "codigotrata") ?? SoapGdebaExpedienteGateway.GetValue(response, "codigoTrata"),
            SoapGdebaExpedienteGateway.GetValue(response, "descripcionTrata"),
            SoapGdebaExpedienteGateway.GetValue(response, "estado"),
            SoapGdebaExpedienteGateway.GetValue(response, "sistemaOrigen"),
            SoapGdebaExpedienteGateway.GetValue(response, "descripcionTramite"),
            SoapGdebaExpedienteGateway.ParseDate(SoapGdebaExpedienteGateway.GetValue(response, "fechaCaratulacion")),
            SoapGdebaExpedienteGateway.GetValue(response, "usuarioCaratulador"),
            SoapGdebaExpedienteGateway.GetValue(response, "usuarioDestino"),
            SoapGdebaExpedienteGateway.MapDocumentosDetalle(response),
            SoapGdebaExpedienteGateway.GetRepeatedValues(response, "archivosAdjuntos"),
            relaciones);
    }

    public async Task<GdebaHistorialExpedienteDto?> BuscarHistorialPasesExpedienteAsync(
        NumeroGdebaCompleto numeroGdebaCompleto,
        ContextoInvocacionGdeba contextoInvocacion,
        CancellationToken cancellationToken)
    {
        var contractOptions = this.ResolveSoapContractOptions();
        var serviceContractOptions = SoapGdebaExpedienteGateway.ResolveConsultaExpedienteServiceContractOptions(contractOptions);
        var serviceOptions = this.ResolveConsultaExpedienteServiceOptions();
        var operationName = "buscarHistorialPasesExpediente";
        var envelope = SoapGdebaExpedienteGateway.BuildEnvelope(
            contractOptions,
            serviceContractOptions,
            operationName,
            numeroGdebaCompleto);

        var document = await this.SendSoapAsync(
            serviceOptions,
            SoapGdebaExpedienteGateway.ResolveSoapOperationContractOptions(serviceContractOptions, operationName),
            operationName,
            envelope,
            contextoInvocacion,
            cancellationToken);
        var response = SoapGdebaExpedienteGateway.FindFirstElement(document, "response");
        if (response is null)
        {
            return null;
        }

        var relaciones = new List<GdebaRelacionExpedienteDto>();
        relaciones.AddRange(SoapGdebaExpedienteGateway.MapRelacionesHistorial(response, "expedientesAsociados", "Asociado"));
        relaciones.AddRange(SoapGdebaExpedienteGateway.MapRelacionesHistorial(response, "expedientesFusionAsociados", "Fusion"));
        relaciones.AddRange(SoapGdebaExpedienteGateway.MapRelacionesHistorial(response, "expedientesVinculados", "TramitacionConjunta"));

        return new GdebaHistorialExpedienteDto(
            SoapGdebaExpedienteGateway.MapDocumentosHistorial(response),
            SoapGdebaExpedienteGateway.MapMovimientosHistorial(response),
            relaciones);
    }

    private async Task<XDocument> SendSoapAsync(
        SoapServiceOptions serviceOptions,
        GdebaSoapOperationContractOptions? operationContractOptions,
        string operationName,
        string envelope,
        ContextoInvocacionGdeba contextoInvocacion,
        CancellationToken cancellationToken)
    {
        var token = await _tokenProvider.ObtenerTokenAsync(cancellationToken);
        var endpoint = SoapGdebaExpedienteGateway.ResolveSoapEndpoint(serviceOptions);
        _logger.LogInformation(
            "Invocando operacion SOAP GDEBA {Operacion} en {Endpoint}.",
            operationName,
            endpoint);
        _logger.LogInformation(
            "Envelope SOAP GDEBA para {Operacion}: {Envelope}",
            operationName,
            envelope);

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (!string.IsNullOrWhiteSpace(operationContractOptions?.SoapAction))
        {
            request.Headers.Add("SOAPAction", operationContractOptions.SoapAction);
        }

        request.Content = new StringContent(envelope, Encoding.UTF8);
        request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/xml;charset='UTF-8'");

        var stopwatch = Stopwatch.StartNew();
        HttpResponseMessage? response = null;
        int? statusCode = null;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
            statusCode = (int)response.StatusCode;
            using (response)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);

                XDocument? document = null;
                if (!string.IsNullOrWhiteSpace(content))
                {
                    try
                    {
                        document = XDocument.Parse(content, LoadOptions.PreserveWhitespace);
                    }
                    catch (Exception ex) when (ex is System.Xml.XmlException or ArgumentException)
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            throw new GdebaOperationException(operationName, "GDEBA devolvio una respuesta XML invalida.", (int)response.StatusCode, innerException: ex);
                        }
                    }
                }

                if (!response.IsSuccessStatusCode)
                {
                    var fault = document is null ? null : SoapGdebaExpedienteGateway.FindSoapFault(document);
                    throw new GdebaOperationException(operationName, fault?.Message ?? $"GDEBA devolvio el error HTTP {(int)response.StatusCode}.", (int)response.StatusCode, fault?.Code);
                }

                if (document is null)
                {
                    throw new GdebaOperationException(operationName, "GDEBA devolvio una respuesta vacia.", (int)response.StatusCode);
                }

                SoapGdebaExpedienteGateway.ThrowIfSoapFault(document, operationName);
                stopwatch.Stop();
                await _registroInvocaciones.AgregarInvocacionAsync(
                    ServicioConsultaExpediente,
                    operationName,
                    contextoInvocacion,
                    servidorRespondio: true,
                    exitosa: true,
                    statusCode,
                    stopwatch.ElapsedMilliseconds,
                    CancellationToken.None);
                return document;
            }
        }
        catch
        {
            stopwatch.Stop();
            await _registroInvocaciones.AgregarInvocacionAsync(
                ServicioConsultaExpediente,
                operationName,
                contextoInvocacion,
                servidorRespondio: response is not null,
                exitosa: false,
                statusCode,
                stopwatch.ElapsedMilliseconds,
                CancellationToken.None);
            throw;
        }
    }

    private GdebaSoapContractsOptions ResolveSoapContractOptions()
    {
        var contractOptions = _options.Value.SoapContracts;
        if (string.IsNullOrWhiteSpace(contractOptions.EnvelopeNamespace))
        {
            throw new InvalidOperationException("No esta configurado el namespace SOAP Envelope.");
        }

        return contractOptions;
    }

    private static GdebaSoapServiceContractOptions ResolveConsultaExpedienteServiceContractOptions(
        GdebaSoapContractsOptions contractOptions)
    {
        if (!contractOptions.Services.TryGetValue(
                GdebaSoapServiceNames.ConsultaExpediente,
                out var serviceContractOptions) ||
            string.IsNullOrWhiteSpace(serviceContractOptions.Namespace))
        {
            throw new InvalidOperationException(
                $"No esta configurado el namespace XML del contrato SOAP '{GdebaSoapServiceNames.ConsultaExpediente}'.");
        }

        return serviceContractOptions;
    }

    private static GdebaSoapOperationContractOptions? ResolveSoapOperationContractOptions(
        GdebaSoapServiceContractOptions serviceContractOptions,
        string operationName)
    {
        return serviceContractOptions.Operations.TryGetValue(operationName, out var operationContractOptions)
            ? operationContractOptions
            : null;
    }

    private SoapServiceOptions ResolveConsultaExpedienteServiceOptions()
    {
        var soapOptions = this.ResolveEnvironmentOptions().Soap;
        if (soapOptions.Services.TryGetValue(
                GdebaSoapServiceNames.ConsultaExpediente,
                out var configuredService))
        {
            SoapGdebaExpedienteGateway.ValidateSoapServiceOptions(configuredService, GdebaSoapServiceNames.ConsultaExpediente);
            return configuredService;
        }

        throw new InvalidOperationException(
            $"No esta configurado el endpoint SOAP del servicio '{GdebaSoapServiceNames.ConsultaExpediente}'.");
    }

    private static Uri ResolveSoapEndpoint(SoapServiceOptions serviceOptions)
    {
        var wsdl = serviceOptions.Wsdl;
        if (string.IsNullOrWhiteSpace(wsdl))
        {
            throw new InvalidOperationException("No esta configurado el WSDL del servicio SOAP.");
        }

        return new Uri(wsdl.Trim(), UriKind.Absolute);
    }

    private static void ValidateSoapServiceOptions(SoapServiceOptions serviceOptions, string serviceName)
    {
        if (string.IsNullOrWhiteSpace(serviceOptions.Wsdl))
        {
            throw new InvalidOperationException($"No esta configurado el WSDL del servicio SOAP '{serviceName}'.");
        }
    }

    private GdebaEnvironmentOptions ResolveEnvironmentOptions()
    {
        var options = _options.Value;
        var environmentName = string.IsNullOrWhiteSpace(options.CurrentEnvironment)
            ? GdebaEnvironmentNames.Hml
            : options.CurrentEnvironment.Trim();

        return options.Environments.TryGetValue(environmentName, out var environmentOptions)
            ? environmentOptions
            : throw new InvalidOperationException($"No existe configuracion GDEBA para el ambiente '{environmentName}'.");
    }

    private static string BuildEnvelope(
        GdebaSoapContractsOptions contractOptions,
        GdebaSoapServiceContractOptions serviceContractOptions,
        string operationName,
        NumeroGdebaCompleto numeroGdebaCompleto)
    {
        return $$"""
            <Envelope xmlns="{{contractOptions.EnvelopeNamespace}}">
                <Body>
                    <{{operationName}} xmlns="{{serviceContractOptions.Namespace}}">
                        <numeroExpediente xmlns="">{{SoapGdebaExpedienteGateway.EscapeXml(numeroGdebaCompleto.Valor)}}</numeroExpediente>
                    </{{operationName}}>
                </Body>
            </Envelope>
            """;
    }

    private static IReadOnlyCollection<GdebaDocumentoExpedienteDto> MapDocumentosDetalle(XElement response)
    {
        var documentos = SoapGdebaExpedienteGateway.GetRepeatedValues(response, "documentosOficiales")
            .Select(x => new GdebaDocumentoExpedienteDto(
                x,
                TipoDocumentoCodigo: SoapGdebaExpedienteGateway.TryGetTipoDocumentoFromNumero(x),
                Referencia: null,
                FechaCreacion: null,
                FechaVinculacion: null,
                UsuarioAsociacion: null,
                UsuarioGenerador: null))
            .ToArray();

        return documentos;
    }

    private static IReadOnlyCollection<GdebaDocumentoExpedienteDto> MapDocumentosHistorial(XElement response)
    {
        var documentos = response
            .Descendants()
            .Where(x => SoapGdebaExpedienteGateway.IsElement(x, "documentosVinculados"))
            .Select(x => new
            {
                Numero = SoapGdebaExpedienteGateway.GetValue(x, "numeroDocumentoGDEBA") ?? SoapGdebaExpedienteGateway.GetValue(x, "numeroGDEBADocumento"),
                TipoDocumento = SoapGdebaExpedienteGateway.GetValue(x, "tipodeDocumento") ?? SoapGdebaExpedienteGateway.GetValue(x, "tipoDocumento"),
                Referencia = SoapGdebaExpedienteGateway.GetValue(x, "referencia"),
                FechaCreacion = SoapGdebaExpedienteGateway.ParseDate(SoapGdebaExpedienteGateway.GetValue(x, "fechaCreacion")),
                FechaVinculacion = SoapGdebaExpedienteGateway.ParseDate(
                    SoapGdebaExpedienteGateway.GetValue(x, "fechavinculacionDefinitiva") ??
                    SoapGdebaExpedienteGateway.GetValue(x, "fechaVinculacionDefinitiva")),
                UsuarioAsociacion = SoapGdebaExpedienteGateway.GetValue(x, "usuarioAsociacion"),
                UsuarioGenerador = SoapGdebaExpedienteGateway.GetValue(x, "usuarioGenerador")
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.Numero))
            .OrderBy(x => x.FechaVinculacion ?? x.FechaCreacion ?? DateTimeOffset.MinValue)
            .ThenBy(x => x.Numero, StringComparer.OrdinalIgnoreCase)
            .Select((x, index) => new GdebaDocumentoExpedienteDto(
                x.Numero!,
                x.TipoDocumento,
                x.Referencia,
                x.FechaCreacion,
                x.FechaVinculacion,
                x.UsuarioAsociacion,
                x.UsuarioGenerador,
                OrdenRespuesta: index + 1))
            .ToArray();

        return documentos;
    }

    private static IReadOnlyCollection<GdebaMovimientoExpedienteDto> MapMovimientosHistorial(XElement response)
    {
        var orden = 0;
        return response
            .Descendants()
            .Where(x => SoapGdebaExpedienteGateway.IsElement(x, "historialDeOperacion"))
            .Select(x => new GdebaMovimientoExpedienteDto(
                ++orden,
                SoapGdebaExpedienteGateway.ParseDate(SoapGdebaExpedienteGateway.GetValue(x, "fechaOperacion")),
                EstadoOrigen: null,
                EstadoDestino: SoapGdebaExpedienteGateway.GetValue(x, "estado"),
                UsuarioOrigen: SoapGdebaExpedienteGateway.GetValue(x, "usuario"),
                UsuarioDestino: SoapGdebaExpedienteGateway.GetValue(x, "destinatario"),
                Motivo: SoapGdebaExpedienteGateway.GetValue(x, "motivo"),
                ReparticionOrigen: SoapGdebaExpedienteGateway.GetValue(x, "origenPaseCodigoReparticion") ??
                    SoapGdebaExpedienteGateway.GetValue(x, "origenPaseDescripcionReparticion"),
                ReparticionDestino: SoapGdebaExpedienteGateway.GetValue(x, "destinoPaseCodigoReparticion") ??
                    SoapGdebaExpedienteGateway.GetValue(x, "destinoPaseDescripcionReparticion"),
                SectorDestino: SoapGdebaExpedienteGateway.GetValue(x, "destinoPaseCodigoSector") ??
                    SoapGdebaExpedienteGateway.GetValue(x, "destinoPaseDescripcionSector")))
            .ToArray();
    }

    private static IEnumerable<GdebaRelacionExpedienteDto> MapRelaciones(
        XElement response,
        string collectionName,
        string tipoRelacion)
    {
        return response
            .Descendants()
            .Where(x => SoapGdebaExpedienteGateway.IsElement(x, collectionName))
            .Select(x => new GdebaRelacionExpedienteDto(
                SoapGdebaExpedienteGateway.GetValue(x, "numeroGDEBA") ?? string.Empty,
                tipoRelacion,
                SoapGdebaExpedienteGateway.ParseBool(SoapGdebaExpedienteGateway.GetValue(x, "esCabecera"))))
            .Where(x => !string.IsNullOrWhiteSpace(x.NumeroExpedienteRelacionado));
    }

    private static IEnumerable<GdebaRelacionExpedienteDto> MapRelacionesHistorial(
        XElement response,
        string collectionName,
        string tipoRelacion)
    {
        return response
            .Descendants()
            .Where(x => SoapGdebaExpedienteGateway.IsElement(x, collectionName))
            .Select(x => new GdebaRelacionExpedienteDto(
                SoapGdebaExpedienteGateway.GetValue(x, "codigoExpediente") ?? string.Empty,
                tipoRelacion,
                EsCabecera: null,
                SoapGdebaExpedienteGateway.GetValue(x, "codigoTrata") ??
                    SoapGdebaExpedienteGateway.GetValue(x, "trataExpedienteASociado") ??
                    SoapGdebaExpedienteGateway.GetValue(x, "trataExpedienteVinculado"),
                SoapGdebaExpedienteGateway.GetValue(x, "descripcionTrata"),
                SoapGdebaExpedienteGateway.ParseDate(
                    SoapGdebaExpedienteGateway.GetValue(x, "fechaAsociacion") ??
                    SoapGdebaExpedienteGateway.GetValue(x, "fechaVinculacion")),
                SoapGdebaExpedienteGateway.GetValue(x, "usuarioAsociador") ??
                    SoapGdebaExpedienteGateway.GetValue(x, "usuarioVinculador")))
            .Where(x => !string.IsNullOrWhiteSpace(x.NumeroExpedienteRelacionado));
    }

    private static void ThrowIfSoapFault(XDocument document, string operationName)
    {
        var fault = SoapGdebaExpedienteGateway.FindSoapFault(document);
        if (fault is null)
        {
            return;
        }

        throw new GdebaOperationException(
            operationName,
            fault.Message,
            soapFaultCode: fault.Code);
    }

    private static SoapFault? FindSoapFault(XDocument document)
    {
        var fault = document.Descendants().FirstOrDefault(x => SoapGdebaExpedienteGateway.IsElement(x, "Fault"));
        if (fault is null)
        {
            return null;
        }

        var message = SoapGdebaExpedienteGateway.SimplifySoapFaultMessage(
            SoapGdebaExpedienteGateway.GetValue(fault, "faultstring") ?? fault.Value.Trim());
        return new SoapFault(SoapGdebaExpedienteGateway.GetValue(fault, "faultcode"), message);
    }

    private static string SimplifySoapFaultMessage(string message)
    {
        const string causedBy = " caused by: ";
        var causedByIndex = message.LastIndexOf(causedBy, StringComparison.OrdinalIgnoreCase);
        if (causedByIndex >= 0)
        {
            message = message[(causedByIndex + causedBy.Length)..];
        }

        const string saxPrefix = "org.xml.sax.SAXParseException: ";
        if (message.StartsWith(saxPrefix, StringComparison.OrdinalIgnoreCase))
        {
            message = message[saxPrefix.Length..];
        }

        return message.Trim();
    }

    private static XElement? FindFirstElement(XDocument document, string localName)
    {
        return document.Descendants().FirstOrDefault(x => SoapGdebaExpedienteGateway.IsElement(x, localName));
    }

    private static string? GetValue(XElement parent, string localName)
    {
        var value = parent
            .Elements()
            .FirstOrDefault(x => SoapGdebaExpedienteGateway.IsElement(x, localName))
            ?.Value;

        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static IReadOnlyCollection<string> GetRepeatedValues(XElement parent, string localName)
    {
        return parent
            .Elements()
            .Where(x => SoapGdebaExpedienteGateway.IsElement(x, localName))
            .Select(x => x.Value?.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .ToArray();
    }

    private static bool IsElement(XElement element, string localName)
    {
        return string.Equals(element.Name.LocalName, localName, StringComparison.OrdinalIgnoreCase);
    }

    private static DateTimeOffset? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var formats = new[]
        {
            "dd/MM/yyyy HH:mm:ss",
            "dd/MM/yyyy H:mm:ss",
            "dd/MM/yyyy",
            "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFK",
            "yyyy-MM-dd'T'HH:mm:ss",
            "yyyy-MM-dd HH:mm:ss"
        };

        if (DateTimeOffset.TryParseExact(
                value.Trim(),
                formats,
                CultureInfo.GetCultureInfo("es-AR"),
                DateTimeStyles.AssumeLocal,
                out var exactResult))
        {
            return exactResult;
        }

        return DateTimeOffset.TryParse(
            value.Trim(),
            CultureInfo.GetCultureInfo("es-AR"),
            DateTimeStyles.AssumeLocal,
            out var parsed)
            ? parsed
            : null;
    }

    private static bool? ParseBool(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return bool.TryParse(value.Trim(), out var result) ? result : null;
    }

    private static string? TryGetTipoDocumentoFromNumero(string numeroDocumento)
    {
        var index = numeroDocumento.IndexOf('-', StringComparison.Ordinal);
        return index > 0 ? numeroDocumento[..index] : null;
    }

    private static string EscapeXml(string value)
    {
        return System.Security.SecurityElement.Escape(value) ?? string.Empty;
    }

    private sealed record SoapFault(string? Code, string Message);
}
