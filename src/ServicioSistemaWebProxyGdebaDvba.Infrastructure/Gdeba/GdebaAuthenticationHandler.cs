using System.Net;
using System.Net.Http.Headers;
using ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Gdeba;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.Conexion.Contracts;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Gdeba;

/// <summary>
/// Unico punto de autenticacion GDEBA: inyecta el Bearer en cada llamada saliente y, si el servidor rechaza el token, lo renueva y reintenta una sola vez.
/// Por ser el paso obligado de toda llamada real, tambien alimenta el sensor de conexion: asi el aviso de "sin conexion" no depende de
/// que operacion se haya invocado (expediente, PDF, enriquecimiento, catalogo).
/// </summary>
internal sealed class GdebaAuthenticationHandler : DelegatingHandler
{
    private readonly IGdebaJwtTokenProvider _tokenProvider;
    private readonly ISensorConexionGdeba _sensorConexion;

    public GdebaAuthenticationHandler(IGdebaJwtTokenProvider tokenProvider, ISensorConexionGdeba sensorConexion)
    {
        _tokenProvider = tokenProvider;
        _sensorConexion = sensorConexion;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            response = await this.EnviarAutenticadoAsync(request, cancellationToken);
        }
        catch (Exception ex) when (ex is GdebaNoAccesibleException or HttpRequestException)
        {
            // Sin token de IOP o sin llegar al servidor. La cancelacion no se clasifica aca: desde este punto no se distingue el timeout
            // de un pedido abandonado por quien llama; eso lo resuelve quien conoce el token original (servicios y middleware de la Api).
            _sensorConexion.RegistrarFalloConexion(GdebaNoAccesibleException.DeterminarOrigen(ex));
            throw;
        }

        // 401/403: el gateway rechazo el acceso y GDEBA no vio el pedido. Cualquier otra respuesta, aun de error, prueba que se llega al servicio.
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            _sensorConexion.RegistrarFalloConexion(OrigenGdebaNoAccesible.ServicioGdeba);
        }
        else
        {
            _sensorConexion.RegistrarExito();
        }

        return response;
    }

    private async Task<HttpResponseMessage> EnviarAutenticadoAsync(HttpRequestMessage request, CancellationToken cancellationToken)
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
