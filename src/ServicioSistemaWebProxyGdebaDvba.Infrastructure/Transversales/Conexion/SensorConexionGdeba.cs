using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Gdeba;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.Conexion;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.Conexion.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.ControlCuotas.Models;
using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;
using ServicioSistemaWebProxyGdebaDvba.Domain.ValueObjects;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Transversales.Conexion;

/// <summary>
/// Circuit breaker de conexion con GDEBA. Estado en memoria del proceso (singleton).
/// La sonda de reconexion es unica (single-flight con Interlocked) y respeta el freno configurado.
/// </summary>
public sealed class SensorConexionGdeba : ISensorConexionGdeba
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SensorConexionGdeba> _logger;
    private readonly long _intervaloSondaMs;

    private volatile bool _accesible = true;
    private int _sondaEnCurso;
    private long _ultimaSondaTicks = long.MinValue;

    public SensorConexionGdeba(IServiceScopeFactory scopeFactory, IOptions<GdebaConexionOptions> options, ILogger<SensorConexionGdeba> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _intervaloSondaMs = Math.Max(1, options.Value.SegundosReintentoSonda) * 1000L;
    }

    public bool Accesible => _accesible;

    public void RegistrarExito() => _accesible = true;

    public void RegistrarFalloConexion() => _accesible = false;

    public void SondearSiCorresponde(string numeroGdebaCompleto)
    {
        if (_accesible || string.IsNullOrWhiteSpace(numeroGdebaCompleto))
        {
            return;
        }

        // Single-flight: solo una sonda a la vez, sin importar cuantos usuarios haya.
        if (Interlocked.CompareExchange(ref _sondaEnCurso, 1, 0) != 0)
        {
            return;
        }

        // Freno: no reintentar antes del intervalo configurado.
        long ahora = Environment.TickCount64;
        if (ahora - Interlocked.Read(ref _ultimaSondaTicks) < _intervaloSondaMs)
        {
            Interlocked.Exchange(ref _sondaEnCurso, 0);
            return;
        }

        Interlocked.Exchange(ref _ultimaSondaTicks, ahora);
        _ = Task.Run(() => this.EjecutarSondaAsync(numeroGdebaCompleto));
    }

    private async Task EjecutarSondaAsync(string numeroGdebaCompleto)
    {
        try
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            IGdebaExpedienteGateway gateway = scope.ServiceProvider.GetRequiredService<IGdebaExpedienteGateway>();
            await gateway.ConsultarExpedienteDetalladoAsync(
                NumeroGdebaCompleto.Create(numeroGdebaCompleto),
                ContextoInvocacionGdeba.Crear(OrigenInvocacionGdeba.Interactiva),
                CancellationToken.None);
            this.RegistrarExito();
        }
        catch (GdebaOperationException)
        {
            // GDEBA respondio aunque haya rechazado el pedido: el enlace esta vivo.
            this.RegistrarExito();
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException)
        {
            // Sigue caido: la proxima actividad volvera a intentar pasado el freno.
            this.RegistrarFalloConexion();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "La sonda de reconexion con GDEBA fallo por un motivo inesperado.");
        }
        finally
        {
            Interlocked.Exchange(ref _sondaEnCurso, 0);
        }
    }
}
