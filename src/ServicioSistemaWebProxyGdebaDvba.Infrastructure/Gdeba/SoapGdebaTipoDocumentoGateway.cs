using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Gdeba;
using ServicioSistemaWebProxyGdebaDvba.Application.Documentos.Models;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.ControlCuotas.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.ControlCuotas.Models;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Gdeba;

public sealed class SoapGdebaTipoDocumentoGateway : IGdebaTipoDocumentoGateway
{
    private const string ServicioConsultaTipoDocumento = "ws_gdeba_consultaTipoDocumento";
    private const string OperacionConsultarTipoDocumento = "consultarTipoDocumento";

    private readonly HttpClient _httpClient;
    private readonly IGdebaJwtTokenProvider _tokenProvider;
    private readonly IRegistroInvocacionesGdeba _registroInvocaciones;
    private readonly IOptions<GdebaOptions> _options;
    private readonly ILogger<SoapGdebaTipoDocumentoGateway> _logger;

    public SoapGdebaTipoDocumentoGateway(HttpClient httpClient, IGdebaJwtTokenProvider tokenProvider,
        IRegistroInvocacionesGdeba registroInvocaciones, IOptions<GdebaOptions> options,
        ILogger<SoapGdebaTipoDocumentoGateway> logger)
    {
        _httpClient = httpClient;
        _tokenProvider = tokenProvider;
        _registroInvocaciones = registroInvocaciones;
        _options = options;
        _logger = logger;
    }

    public async Task<GdebaTipoDocumentoDto?> ConsultarTipoDocumentoAsync(string acronimo, ContextoInvocacionGdeba contextoInvocacion,
        CancellationToken cancellationToken)
    {
        var acronimoNormalizado = string.IsNullOrWhiteSpace(acronimo)
            ? throw new ArgumentException("El acronimo del tipo documental es requerido.", nameof(acronimo))
            : acronimo.Trim().ToUpperInvariant();
        var contractOptions = this.ResolveSoapContractOptions();
        var serviceContractOptions = this.ResolveConsultaTipoDocumentoServiceContractOptions(contractOptions);
        var serviceOptions = this.ResolveConsultaTipoDocumentoServiceOptions();
        var envelope = SoapGdebaTipoDocumentoGateway.BuildEnvelope(contractOptions, serviceContractOptions, acronimoNormalizado);
        var document = await this.SendSoapAsync(
            serviceOptions,
            SoapGdebaTipoDocumentoGateway.ResolveSoapOperationContractOptions(serviceContractOptions, OperacionConsultarTipoDocumento),
            envelope,
            contextoInvocacion,
            cancellationToken);
        var response = SoapGdebaTipoDocumentoGateway.FindFirstElement(document, "return") ??
            SoapGdebaTipoDocumentoGateway.FindFirstElement(document, "response");
        if (response is null)
        {
            return null;
        }

        return new GdebaTipoDocumentoDto(
            SoapGdebaTipoDocumentoGateway.GetValue(response, "acronimo") ?? acronimoNormalizado,
            SoapGdebaTipoDocumentoGateway.GetValue(response, "codigoTipoDocumentoGDEBA"),
            SoapGdebaTipoDocumentoGateway.GetValue(response, "nombre"),
            SoapGdebaTipoDocumentoGateway.GetValue(response, "descripcion"),
            SoapGdebaTipoDocumentoGateway.GetValue(response, "familia"),
            SoapGdebaTipoDocumentoGateway.GetValue(response, "tipoProduccion"),
            SoapGdebaTipoDocumentoGateway.GetValue(response, "estado"),
            SoapGdebaTipoDocumentoGateway.ParseBool(SoapGdebaTipoDocumentoGateway.GetValue(response, "esAutomatica")),
            SoapGdebaTipoDocumentoGateway.ParseBool(SoapGdebaTipoDocumentoGateway.GetValue(response, "esComunicable")),
            SoapGdebaTipoDocumentoGateway.ParseBool(SoapGdebaTipoDocumentoGateway.GetValue(response, "esConfidencial")),
            SoapGdebaTipoDocumentoGateway.ParseBool(SoapGdebaTipoDocumentoGateway.GetValue(response, "esEmbebido")),
            SoapGdebaTipoDocumentoGateway.ParseBool(SoapGdebaTipoDocumentoGateway.GetValue(response, "esEspecial")),
            SoapGdebaTipoDocumentoGateway.ParseBool(SoapGdebaTipoDocumentoGateway.GetValue(response, "esFirmaConjunta")),
            SoapGdebaTipoDocumentoGateway.ParseBool(SoapGdebaTipoDocumentoGateway.GetValue(response, "esFirmaExterna")),
            SoapGdebaTipoDocumentoGateway.ParseBool(SoapGdebaTipoDocumentoGateway.GetValue(response, "esManual")),
            SoapGdebaTipoDocumentoGateway.ParseBool(SoapGdebaTipoDocumentoGateway.GetValue(response, "esNotificable")),
            SoapGdebaTipoDocumentoGateway.ParseBool(SoapGdebaTipoDocumentoGateway.GetValue(response, "tieneTemplate")),
            SoapGdebaTipoDocumentoGateway.ParseBool(SoapGdebaTipoDocumentoGateway.GetValue(response, "tieneToken")));
    }

    private async Task<XDocument> SendSoapAsync(SoapServiceOptions serviceOptions, GdebaSoapOperationContractOptions? operationContractOptions,
        string envelope, ContextoInvocacionGdeba contextoInvocacion, CancellationToken cancellationToken)
    {
        var token = await _tokenProvider.ObtenerTokenAsync(cancellationToken);
        var endpoint = SoapGdebaTipoDocumentoGateway.ResolveSoapEndpoint(serviceOptions);
        _logger.LogInformation("Invocando operacion SOAP GDEBA {Operacion} en {Endpoint}.", OperacionConsultarTipoDocumento, endpoint);

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
                            throw new GdebaOperationException(
                                OperacionConsultarTipoDocumento,
                                "GDEBA devolvio una respuesta XML invalida.",
                                (int)response.StatusCode,
                                innerException: ex);
                        }
                    }
                }

                if (!response.IsSuccessStatusCode)
                {
                    var fault = document is null ? null : SoapGdebaTipoDocumentoGateway.FindSoapFault(document);
                    throw new GdebaOperationException(
                        OperacionConsultarTipoDocumento,
                        fault?.Message ?? $"GDEBA devolvio el error HTTP {(int)response.StatusCode}.",
                        (int)response.StatusCode,
                        fault?.Code);
                }

                if (document is null)
                {
                    throw new GdebaOperationException(OperacionConsultarTipoDocumento, "GDEBA devolvio una respuesta vacia.", (int)response.StatusCode);
                }

                SoapGdebaTipoDocumentoGateway.ThrowIfSoapFault(document);
                stopwatch.Stop();
                await _registroInvocaciones.AgregarInvocacionAsync(
                    ServicioConsultaTipoDocumento, OperacionConsultarTipoDocumento,
                    contextoInvocacion, servidorRespondio: true, exitosa: true,
                    statusCode, stopwatch.ElapsedMilliseconds, CancellationToken.None);
                return document;
            }
        }
        catch
        {
            stopwatch.Stop();
            await _registroInvocaciones.AgregarInvocacionAsync(
                ServicioConsultaTipoDocumento, OperacionConsultarTipoDocumento,
                contextoInvocacion, servidorRespondio: response is not null, exitosa: false,
                statusCode, stopwatch.ElapsedMilliseconds, CancellationToken.None);
            throw;
        }
    }

    private GdebaSoapContractsOptions ResolveSoapContractOptions()
    {
        var contractOptions = _options.Value.SoapContracts;
        return string.IsNullOrWhiteSpace(contractOptions.EnvelopeNamespace)
            ? throw new InvalidOperationException("No esta configurado el namespace SOAP Envelope.")
            : contractOptions;
    }

    private GdebaSoapServiceContractOptions ResolveConsultaTipoDocumentoServiceContractOptions(GdebaSoapContractsOptions contractOptions)
    {
        if (!contractOptions.Services.TryGetValue(GdebaSoapServiceNames.ConsultaTipoDocumento, out var serviceContractOptions) ||
            string.IsNullOrWhiteSpace(serviceContractOptions.Namespace))
        {
            throw new InvalidOperationException(
                $"No esta configurado el namespace XML del contrato SOAP '{GdebaSoapServiceNames.ConsultaTipoDocumento}'.");
        }

        return serviceContractOptions;
    }

    private SoapServiceOptions ResolveConsultaTipoDocumentoServiceOptions()
    {
        var serviceOptions = this.ResolveEnvironmentOptions().Soap;
        if (serviceOptions.Services.TryGetValue(GdebaSoapServiceNames.ConsultaTipoDocumento, out var configuredService) &&
            !string.IsNullOrWhiteSpace(configuredService.Wsdl))
        {
            return configuredService;
        }

        throw new InvalidOperationException(
            $"No esta configurado el endpoint SOAP del servicio '{GdebaSoapServiceNames.ConsultaTipoDocumento}'.");
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

    private static GdebaSoapOperationContractOptions? ResolveSoapOperationContractOptions(
        GdebaSoapServiceContractOptions serviceContractOptions, string operationName)
    {
        return serviceContractOptions.Operations.TryGetValue(operationName, out var operationContractOptions)
            ? operationContractOptions
            : null;
    }

    private static string BuildEnvelope(GdebaSoapContractsOptions contractOptions,
        GdebaSoapServiceContractOptions serviceContractOptions, string acronimo)
    {
        return $$"""
            <Envelope xmlns="{{contractOptions.EnvelopeNamespace}}">
                <Body>
                    <consultarTipoDocumento xmlns="{{serviceContractOptions.Namespace}}">
                        <acronimo xmlns="">{{SoapGdebaTipoDocumentoGateway.EscapeXml(acronimo)}}</acronimo>
                    </consultarTipoDocumento>
                </Body>
            </Envelope>
            """;
    }

    private static void ThrowIfSoapFault(XDocument document)
    {
        var fault = SoapGdebaTipoDocumentoGateway.FindSoapFault(document);
        if (fault is not null)
        {
            throw new GdebaOperationException(OperacionConsultarTipoDocumento, fault.Message, soapFaultCode: fault.Code);
        }
    }

    private static SoapFault? FindSoapFault(XDocument document)
    {
        var fault = document.Descendants().FirstOrDefault(x => SoapGdebaTipoDocumentoGateway.IsElement(x, "Fault"));
        return fault is null
            ? null
            : new SoapFault(
                SoapGdebaTipoDocumentoGateway.GetValue(fault, "faultcode"),
                SoapGdebaTipoDocumentoGateway.GetValue(fault, "faultstring") ?? fault.Value.Trim());
    }

    private static XElement? FindFirstElement(XDocument document, string localName)
    {
        return document.Descendants().FirstOrDefault(x => SoapGdebaTipoDocumentoGateway.IsElement(x, localName));
    }

    private static string? GetValue(XElement? parent, string localName)
    {
        var value = parent?.Elements().FirstOrDefault(x => SoapGdebaTipoDocumentoGateway.IsElement(x, localName))?.Value;
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static bool? ParseBool(string? value)
    {
        return bool.TryParse(value?.Trim(), out var result) ? result : null;
    }

    private static bool IsElement(XElement element, string localName)
    {
        return string.Equals(element.Name.LocalName, localName, StringComparison.OrdinalIgnoreCase);
    }

    private static Uri ResolveSoapEndpoint(SoapServiceOptions serviceOptions)
    {
        return string.IsNullOrWhiteSpace(serviceOptions.Wsdl)
            ? throw new InvalidOperationException("No esta configurado el WSDL del servicio SOAP.")
            : new Uri(serviceOptions.Wsdl.Trim(), UriKind.Absolute);
    }

    private static string EscapeXml(string value)
    {
        return System.Security.SecurityElement.Escape(value) ?? string.Empty;
    }

    private sealed record SoapFault(string? Code, string Message);
}
