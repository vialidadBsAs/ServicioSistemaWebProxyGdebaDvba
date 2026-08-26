using System.Net;
using System.Net.Http.Headers;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Gdeba;

/// <summary>
/// Unico punto de autenticacion GDEBA: inyecta el Bearer en cada llamada saliente y, si el servidor rechaza el token, lo renueva y reintenta una sola vez.
/// </summary>
internal sealed class GdebaAuthenticationHandler : DelegatingHandler
{
    private readonly IGdebaJwtTokenProvider _tokenProvider;

    public GdebaAuthenticationHandler(IGdebaJwtTokenProvider tokenProvider)
    {
        _tokenProvider = tokenProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // El contenido se captura antes del primer envio porque un HttpRequestMessage no puede reenviarse.
        byte[]? contenido = request.Content is null ? null : await request.Content.ReadAsByteArrayAsync(cancellationToken);
        MediaTypeHeaderValue? tipoContenido = request.Content?.Headers.ContentType;

        string token = await _tokenProvider.ObtenerTokenAsync(cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        HttpResponseMessage response = await base.SendAsync(request, cancellationToken);
        if (response.StatusCode != HttpStatusCode.Unauthorized)
        {
            return response;
        }

        response.Dispose();
        _tokenProvider.InvalidarToken();
        string tokenRenovado = await _tokenProvider.ObtenerTokenAsync(cancellationToken);
        using HttpRequestMessage reintento = GdebaAuthenticationHandler.ClonarRequest(request, contenido, tipoContenido);
        reintento.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenRenovado);
        return await base.SendAsync(reintento, cancellationToken);
    }

    private static HttpRequestMessage ClonarRequest(HttpRequestMessage original, byte[]? contenido, MediaTypeHeaderValue? tipoContenido)
    {
        HttpRequestMessage clon = new(original.Method, original.RequestUri);
        foreach (KeyValuePair<string, IEnumerable<string>> encabezado in original.Headers)
        {
            if (string.Equals(encabezado.Key, "Authorization", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            clon.Headers.TryAddWithoutValidation(encabezado.Key, encabezado.Value);
        }

        if (contenido is not null)
        {
            ByteArrayContent contenidoClonado = new(contenido);
            if (tipoContenido is not null)
            {
                contenidoClonado.Headers.ContentType = tipoContenido;
            }

            clon.Content = contenidoClonado;
        }

        return clon;
    }
}
