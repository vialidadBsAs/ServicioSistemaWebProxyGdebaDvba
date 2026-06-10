using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Gdeba;

internal sealed class GdebaJwtTokenProvider : IGdebaJwtTokenProvider
{
    private readonly HttpClient _httpClient;
    private readonly IOptions<GdebaOptions> _options;

    public GdebaJwtTokenProvider(HttpClient httpClient, IOptions<GdebaOptions> options)
    {
        _httpClient = httpClient;
        _options = options;
    }

    public async Task<string> ObtenerTokenAsync(CancellationToken cancellationToken)
    {
        var environmentOptions = ResolveEnvironmentOptions();
        var jwtOptions = environmentOptions.Jwt;

        if (string.IsNullOrWhiteSpace(jwtOptions.Endpoint))
        {
            throw new InvalidOperationException("No esta configurado el endpoint JWT de GDEBA.");
        }

        if (string.IsNullOrWhiteSpace(jwtOptions.Username) || string.IsNullOrWhiteSpace(jwtOptions.Password))
        {
            throw new InvalidOperationException("No estan configuradas las credenciales JWT de GDEBA.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, jwtOptions.Endpoint);
        var basicCredentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{jwtOptions.Username}:{jwtOptions.Password}"));

        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicCredentials);
        request.Content = new StringContent(string.Empty, Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"No se pudo obtener token JWT de GDEBA. StatusCode={(int)response.StatusCode}. Respuesta: {content}");
        }

        return ExtraerToken(content);
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
