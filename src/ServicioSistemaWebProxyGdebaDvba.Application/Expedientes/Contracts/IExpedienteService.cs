using ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Models;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Contracts;

public interface IExpedienteService
{
    Task<ConsultarExpedienteDetalladoResult> ConsultarDetalleAsync(
        ConsultarExpedienteDetalladoRequest request,
        CancellationToken cancellationToken);

    Task<ConsultarMovimientosExpedienteResult> ConsultarMovimientosAsync(
        ConsultarMovimientosExpedienteRequest request,
        CancellationToken cancellationToken);

    Task<ObtenerExpedienteRecursoResult<CabeceraExpedienteDto>> ObtenerCabeceraAsync(
        ObtenerExpedienteRecursoRequest request,
        CancellationToken cancellationToken);

    Task<ObtenerExpedienteRecursoResult<IReadOnlyCollection<DocumentoExpedienteDto>>> ObtenerDocumentosAsync(
        ObtenerExpedienteRecursoRequest request,
        CancellationToken cancellationToken);

    Task<ObtenerExpedienteRecursoResult<IReadOnlyCollection<ArchivoAdjuntoExpedienteDto>>> ObtenerAdjuntosAsync(
        ObtenerExpedienteRecursoRequest request,
        CancellationToken cancellationToken);

    Task<ObtenerExpedienteRecursoResult<IReadOnlyCollection<MovimientoExpedienteDto>>> ObtenerPasesAsync(
        ObtenerExpedienteRecursoRequest request,
        CancellationToken cancellationToken);

    Task<ObtenerExpedienteRecursoResult<IReadOnlyCollection<RelacionExpedienteDto>>> ObtenerRelacionesAsync(
        ObtenerExpedienteRecursoRequest request,
        CancellationToken cancellationToken);

    Task<ObtenerExpedienteRecursoResult<ExpedienteCompletoDto>> ObtenerCompletoAsync(
        ObtenerExpedienteRecursoRequest request,
        CancellationToken cancellationToken);

    Task ConsolidarDetalleEnCacheAsync(
        GdebaExpedienteDetalladoDto detalle,
        DateTimeOffset fechaConsulta,
        CancellationToken cancellationToken);

    Task<ConsultarExpedienteSinCacheResult> ConsultarExpedienteSinCacheAsync(
        ConsultarExpedienteSinCacheRequest request,
        CancellationToken cancellationToken);
}
