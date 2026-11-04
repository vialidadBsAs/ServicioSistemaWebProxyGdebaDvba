using Microsoft.Extensions.Logging;
using ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Gdeba;
using ServicioSistemaWebProxyGdebaDvba.Application.Documentos.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Documentos.Models;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.ControlCuotas.Models;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities;
using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;
using URF.Core.Abstractions;
using URF.Core.Abstractions.Trackable;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Documentos.Services;

public sealed class CompletadoCatalogoTiposDocumentoService : ICompletadoCatalogoTiposDocumentoService
{
    public const int MaximoTiposPorTanda = 500;
    private static readonly TimeSpan PausaEntreConsultas = TimeSpan.FromMilliseconds(250);

    private readonly ITrackableRepository<TipoDocumentoGdeba> _tipoDocumentoRepository;
    private readonly IGdebaTipoDocumentoGateway _gdebaTipoDocumentoGateway;
    private readonly ICatalogoTiposDocumentoGdebaService _catalogoTiposDocumento;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CompletadoCatalogoTiposDocumentoService> _logger;

    public CompletadoCatalogoTiposDocumentoService(
        ITrackableRepository<TipoDocumentoGdeba> tipoDocumentoRepository,
        IGdebaTipoDocumentoGateway gdebaTipoDocumentoGateway,
        ICatalogoTiposDocumentoGdebaService catalogoTiposDocumento,
        IUnitOfWork unitOfWork,
        ILogger<CompletadoCatalogoTiposDocumentoService> logger)
    {
        _tipoDocumentoRepository = tipoDocumentoRepository;
        _gdebaTipoDocumentoGateway = gdebaTipoDocumentoGateway;
        _catalogoTiposDocumento = catalogoTiposDocumento;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<CompletadoCatalogoTiposDocumentoDto> CompletarAsync(int maximoTipos, CancellationToken cancellationToken)
    {
        int tamanoTanda = Math.Clamp(maximoTipos, 1, CompletadoCatalogoTiposDocumentoService.MaximoTiposPorTanda);
        List<TipoDocumentoGdeba> pendientes = (await _tipoDocumentoRepository.Query()
            .Where(x => x.Activo && x.CodigoTipoDocumentoGdeba == null)
            .OrderBy(x => x.Codigo)
            .SelectAsync(cancellationToken))
            .ToList();
        int confirmados = 0;
        int desactivados = 0;
        int conError = 0;
        string? motivoInterrupcion = null;
        ContextoInvocacionGdeba contextoInvocacion = ContextoInvocacionGdeba.Crear(OrigenInvocacionGdeba.Administrativo);

        foreach (TipoDocumentoGdeba tipo in pendientes.Take(tamanoTanda))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                GdebaTipoDocumentoDto? detalle = await _gdebaTipoDocumentoGateway.ConsultarTipoDocumentoAsync(tipo.Codigo, contextoInvocacion, cancellationToken);
                if (detalle is null)
                {
                    tipo.Desactivar();
                    _tipoDocumentoRepository.Update(tipo);
                    _tipoDocumentoRepository.ApplyChanges(tipo);
                    desactivados++;
                }
                else
                {
                    _catalogoTiposDocumento.IncorporarRespuestaGdeba(tipo, tipo.Codigo, detalle);
                    confirmados++;
                }
            }
            catch (Exception ex) when (GdebaNoAccesibleException.EsNoAccesible(ex))
            {
                // Sin GDEBA no tiene sentido seguir: se guarda lo consultado y la proxima tanda retoma desde el mismo punto.
                motivoInterrupcion = GdebaNoAccesibleException.DescribirMotivo(ex);
                _logger.LogWarning(ex, "Se interrumpe el completado del catalogo de tipos documentales en {Codigo}: {Motivo}.", tipo.Codigo, motivoInterrupcion);
                break;
            }
            catch (GdebaOperationException ex)
            {
                // Error de GDEBA para este tipo: no se desactiva, queda pendiente para otra tanda.
                conError++;
                _logger.LogWarning(ex, "GDEBA devolvio un error al consultar el tipo documental {Codigo}.", tipo.Codigo);
            }

            await Task.Delay(CompletadoCatalogoTiposDocumentoService.PausaEntreConsultas, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        int pendientesRestantes = pendientes.Count - confirmados - desactivados;
        _logger.LogInformation(
            "Completado del catalogo de tipos documentales: {Confirmados} confirmados, {Desactivados} desactivados, {ConError} con error, {Pendientes} pendientes.",
            confirmados, desactivados, conError, pendientesRestantes);
        return new CompletadoCatalogoTiposDocumentoDto(
            pendientes.Count, confirmados, desactivados, conError, pendientesRestantes, motivoInterrupcion is not null, motivoInterrupcion);
    }
}
