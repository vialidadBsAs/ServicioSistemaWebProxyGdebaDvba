using System.Diagnostics;
using System.Globalization;
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

public sealed class SoapGdebaDocumentoGateway : IGdebaDocumentoGateway
{
    private const string ServicioConsultaDocumento = "ws_gdeba_consultaDocumento";

    private readonly HttpClient _httpClient;
    private readonly IGdebaJwtTokenProvider _tokenProvider;
    private readonly IRegistroInvocacionesGdeba _registroInvocaciones;
    private readonly IOptions<GdebaOptions> _options;
    private readonly ILogger<SoapGdebaDocumentoGateway> _logger;

    public SoapGdebaDocumentoGateway(
        HttpClient httpClient,
        IGdebaJwtTokenProvider tokenProvider,
        IRegistroInvocacionesGdeba registroInvocaciones,
        IOptions<GdebaOptions> options,
        ILogger<SoapGdebaDocumentoGateway> logger)
    {
        _httpClient = httpClient;
        _tokenProvider = tokenProvider;
        _registroInvocaciones = registroInvocaciones;
        _options = options;
        _logger = logger;
    }

    public async Task<GdebaDocumentoDetalleDto?> BuscarDetallePorNumeroAsync(
        string numeroDocumento,
        ContextoInvocacionGdeba contextoInvocacion,
        CancellationToken cancellationToken)
    {
        var numeroNormalizado = string.IsNullOrWhiteSpace(numeroDocumento)
            ? throw new ArgumentException("El numero del documento es requerido.", nameof(numeroDocumento))
            : numeroDocumento.Trim();

        var contractOptions = this.ResolveSoapContractOptions();
        var serviceContractOptions = this.ResolveConsultaDocumentoServiceContractOptions(contractOptions);
        var serviceOptions = this.ResolveConsultaDocumentoServiceOptions();
        const string operationName = "buscarDetallePorNumero";
        var envelope = SoapGdebaDocumentoGateway.BuildEnvelope(
            contractOptions, serviceContractOptions, operationName,
            numeroNormalizado);

        var document = await this.SendSoapAsync(
            serviceOptions,
            SoapGdebaDocumentoGateway.ResolveSoapOperationContractOptions(
                serviceContractOptions, operationName),
            operationName,
            envelope,
            contextoInvocacion,
            cancellationToken);
        var response = SoapGdebaDocumentoGateway.FindFirstElement(
                document, "response") ??
            SoapGdebaDocumentoGateway.FindFirstElement(document, "return");
        if (response is null)
        {
            return null;
        }

        var tipoDocumento = SoapGdebaDocumentoGateway.FindFirstChild(response, "tipoDocumento");
        return new GdebaDocumentoDetalleDto(
            SoapGdebaDocumentoGateway.GetValue(response, "numeroDocumento") ?? numeroNormalizado,
            SoapGdebaDocumentoGateway.GetValue(response, "numeroEspecial"),
            SoapGdebaDocumentoGateway.GetValue(tipoDocumento, "acronimo") ??
                SoapGdebaDocumentoGateway.GetValue(response, "acronimo") ??
                SoapGdebaDocumentoGateway.GetValue(response, "tipoDocumento"),
            SoapGdebaDocumentoGateway.GetValue(tipoDocumento, "nombre"),
            SoapGdebaDocumentoGateway.GetValue(tipoDocumento, "descripcion"),
            SoapGdebaDocumentoGateway.GetValue(response, "referencia"),
            SoapGdebaDocumentoGateway.ParseDate(SoapGdebaDocumentoGateway.GetValue(response, "fechaCreacion")),
            SoapGdebaDocumentoGateway.JoinValues(response, "listaFirmantes"),
            SoapGdebaDocumentoGateway.GetValue(response, "urlArchivo"),
            SoapGdebaDocumentoGateway.ParseBool(SoapGdebaDocumentoGateway.GetValue(response, "puedeVerDocumento")),
            SoapGdebaDocumentoGateway.MapHistorial(response));
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
        var endpoint = SoapGdebaDocumentoGateway.ResolveSoapEndpoint(serviceOptions);
        _logger.LogInformation(
            "Invocando operacion SOAP GDEBA {Operacion} en {Endpoint}.",
            operationName,
            endpoint);

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
                                operationName,
                                "GDEBA devolvio una respuesta XML invalida.",
                                (int)response.StatusCode,
                                innerException: ex);
                        }
                    }
                }

                if (!response.IsSuccessStatusCode)
                {
                    var fault = document is null ? null : SoapGdebaDocumentoGateway.FindSoapFault(document);
                    throw new GdebaOperationException(
                        operationName,
                        fault?.Message ??
                        $"GDEBA devolvio el error HTTP {(int)response.StatusCode}.",
                        (int)response.StatusCode,
                        fault?.Code);
                }

                if (document is null)
                {
                    throw new GdebaOperationException(operationName, "GDEBA devolvio una respuesta vacia.", (int)response.StatusCode);
                }

                SoapGdebaDocumentoGateway.ThrowIfSoapFault(document, operationName);
                stopwatch.Stop();
                await _registroInvocaciones.AgregarInvocacionAsync(
                    ServicioConsultaDocumento, operationName,
                    contextoInvocacion, servidorRespondio: true,
                    exitosa: true, statusCode, stopwatch.ElapsedMilliseconds,
                    CancellationToken.None);
                return document;
            }
        }
        catch
        {
            stopwatch.Stop();
            await _registroInvocaciones.AgregarInvocacionAsync(
                ServicioConsultaDocumento, operationName, contextoInvocacion,
                servidorRespondio: response is not null, exitosa: false,
                statusCode, stopwatch.ElapsedMilliseconds,
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

    private GdebaSoapServiceContractOptions ResolveConsultaDocumentoServiceContractOptions(
        GdebaSoapContractsOptions contractOptions)
    {
        if (!contractOptions.Services.TryGetValue(
                GdebaSoapServiceNames.ConsultaDocumento,
                out var serviceContractOptions) ||
            string.IsNullOrWhiteSpace(serviceContractOptions.Namespace))
        {
            throw new InvalidOperationException(
                $"No esta configurado el namespace XML del contrato SOAP '{GdebaSoapServiceNames.ConsultaDocumento}'.");
        }

        return serviceContractOptions;
    }

    private SoapServiceOptions ResolveConsultaDocumentoServiceOptions()
    {
        var soapOptions = this.ResolveEnvironmentOptions().Soap;
        if (soapOptions.Services.TryGetValue(
                GdebaSoapServiceNames.ConsultaDocumento,
                out var configuredService))
        {
            SoapGdebaDocumentoGateway.ValidateSoapServiceOptions(configuredService, GdebaSoapServiceNames.ConsultaDocumento);
            return configuredService;
        }

        throw new InvalidOperationException(
            $"No esta configurado el endpoint SOAP del servicio '{GdebaSoapServiceNames.ConsultaDocumento}'.");
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

    private static GdebaSoapOperationContractOptions?
        ResolveSoapOperationContractOptions(
            GdebaSoapServiceContractOptions serviceContractOptions,
            string operationName)
    {
        return serviceContractOptions.Operations.TryGetValue(operationName, out var operationContractOptions)
            ? operationContractOptions
            : null;
    }

    private static string BuildEnvelope(
        GdebaSoapContractsOptions contractOptions,
        GdebaSoapServiceContractOptions serviceContractOptions,
        string operationName,
        string numeroDocumento)
    {
        return $$"""
            <Envelope xmlns="{{contractOptions.EnvelopeNamespace}}">
                <Body>
                    <{{operationName}} xmlns="{{serviceContractOptions.Namespace}}">
                        <Assignee xmlns="">false</Assignee>
                        <numeroDocumento xmlns="">{{SoapGdebaDocumentoGateway.EscapeXml(numeroDocumento)}}</numeroDocumento>
                        <usuarioConsulta xmlns=""></usuarioConsulta>
                    </{{operationName}}>
                </Body>
            </Envelope>
            """;
    }

    private static IReadOnlyCollection<GdebaHistorialDocumentoDto> MapHistorial(XElement response)
    {
        return response
            .Descendants()
            .Where(x => SoapGdebaDocumentoGateway.IsElement(x, "listaHistorial"))
            .Select(x => new GdebaHistorialDocumentoDto(
                SoapGdebaDocumentoGateway.ParseLong(
                    SoapGdebaDocumentoGateway.GetValue(x, "id") ??
                    SoapGdebaDocumentoGateway.GetValue(x, "idHistorial")) ?? 0,
                SoapGdebaDocumentoGateway.GetValue(x, "actividad"),
                SoapGdebaDocumentoGateway.ParseDate(SoapGdebaDocumentoGateway.GetValue(x, "fechaInicio")),
                SoapGdebaDocumentoGateway.ParseDate(SoapGdebaDocumentoGateway.GetValue(x, "fechaFin")),
                SoapGdebaDocumentoGateway.GetValue(x, "usuario"),
                SoapGdebaDocumentoGateway.GetValue(x, "nombreUsuario"),
                SoapGdebaDocumentoGateway.GetValue(x, "workflowOrigen")))
            .Where(x => x.IdGdeba > 0)
            .ToArray();
    }

    private static void ThrowIfSoapFault(XDocument document, string operationName)
    {
        var fault = SoapGdebaDocumentoGateway.FindSoapFault(document);
        if (fault is null)
        {
            return;
        }

        throw new GdebaOperationException(operationName, fault.Message, soapFaultCode: fault.Code);
    }

    private static SoapFault? FindSoapFault(XDocument document)
    {
        var fault = document.Descendants().FirstOrDefault(x => SoapGdebaDocumentoGateway.IsElement(x, "Fault"));
        if (fault is null)
        {
            return null;
        }

        return new SoapFault(
            SoapGdebaDocumentoGateway.GetValue(fault, "faultcode"),
            SoapGdebaDocumentoGateway.GetValue(fault, "faultstring") ??
            fault.Value.Trim());
    }

    private static XElement? FindFirstElement(XDocument document, string localName)
    {
        return document.Descendants().FirstOrDefault(x => SoapGdebaDocumentoGateway.IsElement(x, localName));
    }

    private static XElement? FindFirstChild(XElement? parent, string localName)
    {
        if (parent is null)
        {
            return null;
        }

        return parent.Elements().FirstOrDefault(x => SoapGdebaDocumentoGateway.IsElement(x, localName));
    }

    private static string? GetValue(XElement? parent, string localName)
    {
        if (parent is null)
        {
            return null;
        }

        var value = parent
            .Elements()
            .FirstOrDefault(x => SoapGdebaDocumentoGateway.IsElement(x, localName))
            ?.Value;

        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? JoinValues(XElement parent, string localName)
    {
        var values = parent
            .Descendants()
            .Where(x => SoapGdebaDocumentoGateway.IsElement(x, localName))
            .Select(x => x.Value?.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return values.Length == 0 ? null : string.Join(";", values);
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

    private static long? ParseLong(string? value)
    {
        return long.TryParse(value?.Trim(), out var result) ? result : null;
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

    private static string EscapeXml(string value)
    {
        return System.Security.SecurityElement.Escape(value) ?? string.Empty;
    }

    private sealed record SoapFault(string? Code, string Message);
}
