using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Gdeba;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Gdeba;

internal sealed class GdebaJwtTokenProvider : IGdebaJwtTokenProvider
{
    // El cache es estatico porque el proveedor se registra como typed client transitorio; el token GDEBA dura ~2 minutos y sin cache cada llamada SOAP pagaria un viaje extra al endpoint JWT.
    private static readonly SemaphoreSlim _renovacionToken = new(1, 1);
    private static readonly TimeSpan _margenRenovacion = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan _vidaPredeterminada = TimeSpan.FromSeconds(90);
    private static string? _tokenActual;
    private static DateTimeOffset _tokenValidoHasta = DateTimeOffset.MinValue;

    private readonly HttpClient _httpClient;
    private readonly IOptions<GdebaOptions> _options;
    private readonly ILogger<GdebaJwtTokenProvider> _logger;

    public GdebaJwtTokenProvider(HttpClient httpClient, IOptions<GdebaOptions> options, ILogger<GdebaJwtTokenProvider> logger)
    {
        _httpClient = httpClient;
        _options = options;
        _logger = logger;
    }

    public async Task<string> ObtenerTokenAsync(CancellationToken cancellationToken)
    {
        string? tokenVigente = GdebaJwtTokenProvider.TokenVigente();
        if (tokenVigente is not null)
        {
            return tokenVigente;
        }

        await _renovacionToken.WaitAsync(cancellationToken);
        try
        {
            tokenVigente = GdebaJwtTokenProvider.TokenVigente();
            if (tokenVigente is not null)
            {
                return tokenVigente;
            }

            string tokenNuevo = await this.SolicitarTokenAsync(cancellationToken);
            TimeSpan vida = GdebaJwtTokenProvider.CalcularVida(tokenNuevo);
            TimeSpan margen = vida > _margenRenovacion + _margenRenovacion ? _margenRenovacion : TimeSpan.FromTicks(vida.Ticks / 4);
            _tokenActual = tokenNuevo;
            _tokenValidoHasta = DateTimeOffset.Now + vida - margen;
            return tokenNuevo;
        }
        finally
        {
            _renovacionToken.Release();
        }
    }

    public void InvalidarToken()
    {
        _tokenActual = null;
        _tokenValidoHasta = DateTimeOffset.MinValue;
    }

    private static string? TokenVigente()
    {
        return _tokenActual is not null && DateTimeOffset.Now < _tokenValidoHasta ? _tokenActual : null;
    }

    private static TimeSpan CalcularVida(string token)
    {
        // La vida se toma de exp-iat del propio JWT y se mide contra el reloj local, para no depender del reloj del emisor.
        try
        {
            string[] partes = token.Split('.');
            if (partes.Length < 2)
            {
                return _vidaPredeterminada;
            }

            string payload = partes[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            using JsonDocument document = JsonDocument.Parse(Convert.FromBase64String(payload));
            if (document.RootElement.TryGetProperty("exp", out JsonElement exp) &&
                document.RootElement.TryGetProperty("iat", out JsonElement iat))
            {
                long segundos = exp.GetInt64() - iat.GetInt64();
                if (segundos > 0)
                {
                    return TimeSpan.FromSeconds(segundos);
                }
            }

            return _vidaPredeterminada;
        }
        catch (Exception ex) when (ex is FormatException or JsonException)
        {
            return _vidaPredeterminada;
        }
    }

    private async Task<string> SolicitarTokenAsync(CancellationToken cancellationToken)
    {
        var environmentOptions = ResolveEnvironmentOptions();
        var jwtOptions = environmentOptions.Jwt;

        if (string.IsNullOrWhiteSpace(jwtOptions.Endpoint))
        {
            throw new GdebaConfigurationException(
                "Configuracion de GDEBA incompleta",
                "El proxy no tiene configurado el acceso JWT de GDEBA para completar la operacion.",
                "No esta configurado el endpoint JWT de GDEBA para el ambiente activo.");
        }

        if (string.IsNullOrWhiteSpace(jwtOptions.Username) || string.IsNullOrWhiteSpace(jwtOptions.Password))
        {
            throw new GdebaConfigurationException(
                "Credenciales GDEBA no configuradas",
                "El proxy no tiene configuradas las credenciales de GDEBA necesarias para completar la operacion.",
                "No estan configuradas las credenciales JWT de GDEBA para el ambiente activo.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, jwtOptions.Endpoint);
        var basicCredentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{jwtOptions.Username}:{jwtOptions.Password}"));

        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicCredentials);
        request.Content = new StringContent(string.Empty, Encoding.UTF8, "application/json");

        // Toda falla en este paso es de origen "credenciales IOP": el pedido a GDEBA no llego a enviarse. Incluye la falta de respuesta
        // del servicio de tokens; la cancelacion pedida por quien llama se deja pasar.
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException || (ex is OperationCanceledException && !cancellationToken.IsCancellationRequested))
        {
            _logger.LogWarning(ex, "El servicio de tokens de IOP no respondio.");
            throw new GdebaNoAccesibleException("el servicio de tokens de IOP no respondio", OrigenGdebaNoAccesible.CredencialesIop, ex);
        }

        using HttpResponseMessage respuestaDescartable = response;
        string content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            // Sin token el pedido nunca llega a GDEBA: es "GDEBA no accesible", igual que el enlace caido, cualquiera sea el estado que
            // devuelva el servicio de tokens (503, 401 por certificado, etc.). La respuesta cruda queda solo en el log.
            int estadoHttp = (int)response.StatusCode;
            _logger.LogWarning("El servicio de tokens de IOP respondio HTTP {EstadoHttp}. Respuesta: {Respuesta}", estadoHttp, GdebaJwtTokenProvider.RecortarParaLog(content));
            throw new GdebaNoAccesibleException($"el servicio de tokens de IOP respondio HTTP {estadoHttp}", OrigenGdebaNoAccesible.CredencialesIop);
        }

        // Una respuesta exitosa sin token utilizable deja al proxy igual de imposibilitado de llegar a GDEBA. No se registra el contenido:
        // podria traer un token con una forma no prevista.
        try
        {
            return GdebaJwtTokenProvider.ExtraerToken(content);
        }
        catch (Exception ex) when (ex is InvalidOperationException or JsonException)
        {
            _logger.LogWarning("El servicio de tokens de IOP respondio HTTP {EstadoHttp} sin un token utilizable: {Motivo}", (int)response.StatusCode, ex.Message);
            throw new GdebaNoAccesibleException("el servicio de tokens de IOP no devolvio un token utilizable", OrigenGdebaNoAccesible.CredencialesIop, ex);
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
            : throw new GdebaConfigurationException(
                "Configuracion de GDEBA incompleta",
                "El proxy no tiene configurado el ambiente GDEBA necesario para completar la operacion.",
                $"No existe configuracion GDEBA para el ambiente activo '{environmentName}'.");
    }

    private static string RecortarParaLog(string contenido)
    {
        const int largoMaximo = 500;
        string texto = contenido.ReplaceLineEndings(" ").Trim();
        return texto.Length <= largoMaximo ? texto : texto[..largoMaximo] + "...";
    }

    private static string ExtraerToken(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("La respuesta JWT de GDEBA no contiene token.");
        }

        var trimmed = content.Trim().Trim('"');
        if (!trimmed.StartsWith('{'))
        {
            return trimmed;
        }

        using var document = JsonDocument.Parse(trimmed);
        var root = document.RootElement;

        foreach (var propertyName in new[] { "token", "access_token", "jwt", "id_token" })
        {
            if (root.TryGetProperty(propertyName, out var tokenProperty) &&
                tokenProperty.ValueKind == JsonValueKind.String)
            {
                var token = tokenProperty.GetString();
                if (!string.IsNullOrWhiteSpace(token))
                {
                    return token;
                }
            }
        }

        throw new InvalidOperationException("No se encontro el token JWT en la respuesta de GDEBA.");
    }
}
