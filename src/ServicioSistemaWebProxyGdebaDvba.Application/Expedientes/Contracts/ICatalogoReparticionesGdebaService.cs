using ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Models;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Contracts;

/// <summary>
/// Mantiene el catalogo local de reparticiones y sectores GDEBA a partir de lo que informa cada historial de pases.
/// Prepara altas y actualizaciones en la unidad de trabajo del caso de uso que lo invoca; no confirma por su cuenta.
/// </summary>
public interface ICatalogoReparticionesGdebaService
{
    Task RegistrarDesdeMovimientosAsync(IReadOnlyCollection<GdebaMovimientoExpedienteDto> movimientos, CancellationToken cancellationToken);

    /// <summary>Completa la cabecera con la reparticion caratuladora del numero y las descripciones de reparticiones y sector segun el catalogo.</summary>
    Task<CabeceraExpedienteDto> CompletarDescripcionesAsync(CabeceraExpedienteDto cabecera, CancellationToken cancellationToken);
}
