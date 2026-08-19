using ServicioSistemaWebProxyGdebaDvba.Application.Workers.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Workers.Models;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities.Configuracion;
using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;
using URF.Core.Abstractions;
using URF.Core.Abstractions.Trackable;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Workers.Services;

public sealed class ConfiguracionDatosWorkerService : IConfiguracionDatosWorkerService
{
    private readonly ITrackableRepository<ConfiguracionDescubrimientoTemaExpediente> _configuracionDescubrimientoTemaRepository;
    private readonly ITrackableRepository<ConfiguracionDescubrimientoTrataExpediente> _configuracionDescubrimientoTrataRepository;
    private readonly ITrackableRepository<ConfiguracionEnriquecimientoMetadataDocumentoTemaExpediente> _configuracionEnriquecimientoTemaRepository;
    private readonly IRepository<TemaExpediente> _temaRepository;
    private readonly IRepository<TrataHabilitadaVialidad> _trataHabilitadaVialidadRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ConfiguracionDatosWorkerService(
        ITrackableRepository<ConfiguracionDescubrimientoTemaExpediente> configuracionDescubrimientoTemaRepository,
        ITrackableRepository<ConfiguracionDescubrimientoTrataExpediente> configuracionDescubrimientoTrataRepository,
        ITrackableRepository<ConfiguracionEnriquecimientoMetadataDocumentoTemaExpediente> configuracionEnriquecimientoTemaRepository,
        IRepository<TemaExpediente> temaRepository,
        IRepository<TrataHabilitadaVialidad> trataHabilitadaVialidadRepository,
        IUnitOfWork unitOfWork)
    {
        _configuracionDescubrimientoTemaRepository = configuracionDescubrimientoTemaRepository;
        _configuracionDescubrimientoTrataRepository = configuracionDescubrimientoTrataRepository;
        _configuracionEnriquecimientoTemaRepository = configuracionEnriquecimientoTemaRepository;
        _temaRepository = temaRepository;
        _trataHabilitadaVialidadRepository = trataHabilitadaVialidadRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ConfiguracionDatosWorkerDto> ConsultarAsync(ProcesoWorker proceso, CancellationToken cancellationToken)
    {
        var temas = proceso switch
        {
            ProcesoWorker.DescubrimientoExpedientes => (await _configuracionDescubrimientoTemaRepository.Query().SelectAsync(cancellationToken))
                .Select(x => new ConfiguracionTemaWorkerDto(x.TemaExpedienteId, x.Habilitado, x.Prioridad)).ToArray(),
            ProcesoWorker.EnriquecimientoDetalleDocumental => (await _configuracionEnriquecimientoTemaRepository.Query().SelectAsync(cancellationToken))
                .Select(x => new ConfiguracionTemaWorkerDto(x.TemaExpedienteId, x.Habilitado, x.Prioridad)).ToArray(),
            ProcesoWorker.ExpedienteDetallado => Array.Empty<ConfiguracionTemaWorkerDto>(),
            _ => throw new InvalidOperationException("El proceso de Worker no está soportado.")
        };
        var tratas = proceso == ProcesoWorker.DescubrimientoExpedientes
            ? (await _configuracionDescubrimientoTrataRepository.Query().SelectAsync(cancellationToken))
                .Select(x => new ConfiguracionTrataDescubrimientoWorkerDto(x.CodigoTrata, x.Habilitada, x.Prioridad)).ToArray()
            : Array.Empty<ConfiguracionTrataDescubrimientoWorkerDto>();
        return new ConfiguracionDatosWorkerDto(temas, tratas);
    }

    public async Task<ConfiguracionTemaWorkerDto> GuardarTemaAsync(GuardarConfiguracionTemaWorkerRequest request, CancellationToken cancellationToken)
    {
        var temaExiste = await _temaRepository.Query().AnyAsync(x => x.Id == request.TemaExpedienteId, cancellationToken);
        if (!temaExiste) throw new InvalidOperationException("El tema seleccionado no existe.");

        switch (request.Proceso)
        {
            case ProcesoWorker.DescubrimientoExpedientes:
                var configuracionDescubrimiento = await _configuracionDescubrimientoTemaRepository.Query().FirstOrDefaultAsync(x => x.TemaExpedienteId == request.TemaExpedienteId, cancellationToken);
                if (configuracionDescubrimiento is null)
                {
                    configuracionDescubrimiento = new ConfiguracionDescubrimientoTemaExpediente(request.TemaExpedienteId, request.Habilitado, request.Prioridad);
                    _configuracionDescubrimientoTemaRepository.Insert(configuracionDescubrimiento);
                }
                else
                {
                    configuracionDescubrimiento.Actualizar(request.Habilitado, request.Prioridad);
                    _configuracionDescubrimientoTemaRepository.Update(configuracionDescubrimiento);
                }

                _configuracionDescubrimientoTemaRepository.ApplyChanges(configuracionDescubrimiento);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return new ConfiguracionTemaWorkerDto(configuracionDescubrimiento.TemaExpedienteId, configuracionDescubrimiento.Habilitado, configuracionDescubrimiento.Prioridad);

            case ProcesoWorker.EnriquecimientoDetalleDocumental:
                var configuracionEnriquecimiento = await _configuracionEnriquecimientoTemaRepository.Query().FirstOrDefaultAsync(x => x.TemaExpedienteId == request.TemaExpedienteId, cancellationToken);
                if (configuracionEnriquecimiento is null)
                {
                    configuracionEnriquecimiento = new ConfiguracionEnriquecimientoMetadataDocumentoTemaExpediente(request.TemaExpedienteId, request.Habilitado, request.Prioridad);
                    _configuracionEnriquecimientoTemaRepository.Insert(configuracionEnriquecimiento);
                }
                else
                {
                    configuracionEnriquecimiento.Actualizar(request.Habilitado, request.Prioridad);
                    _configuracionEnriquecimientoTemaRepository.Update(configuracionEnriquecimiento);
                }

                _configuracionEnriquecimientoTemaRepository.ApplyChanges(configuracionEnriquecimiento);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return new ConfiguracionTemaWorkerDto(configuracionEnriquecimiento.TemaExpedienteId, configuracionEnriquecimiento.Habilitado, configuracionEnriquecimiento.Prioridad);

            default:
                throw new InvalidOperationException("El proceso de Worker no está soportado.");
        }
    }

    public async Task QuitarTemaAsync(ProcesoWorker proceso, Guid temaExpedienteId, CancellationToken cancellationToken)
    {
        switch (proceso)
        {
            case ProcesoWorker.DescubrimientoExpedientes:
                var configuracionDescubrimiento = await _configuracionDescubrimientoTemaRepository.Query().FirstOrDefaultAsync(x => x.TemaExpedienteId == temaExpedienteId, cancellationToken);
                if (configuracionDescubrimiento is not null) _configuracionDescubrimientoTemaRepository.Delete(configuracionDescubrimiento);
                break;

            case ProcesoWorker.EnriquecimientoDetalleDocumental:
                var configuracionEnriquecimiento = await _configuracionEnriquecimientoTemaRepository.Query().FirstOrDefaultAsync(x => x.TemaExpedienteId == temaExpedienteId, cancellationToken);
                if (configuracionEnriquecimiento is not null) _configuracionEnriquecimientoTemaRepository.Delete(configuracionEnriquecimiento);
                break;

            default:
                throw new InvalidOperationException("El proceso de Worker no está soportado.");
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<ConfiguracionTrataDescubrimientoWorkerDto> GuardarTrataDescubrimientoAsync(GuardarConfiguracionTrataDescubrimientoWorkerRequest request, CancellationToken cancellationToken)
    {
        var codigoTrata = this.NormalizarCodigoTrata(request.CodigoTrata);
        var trataExiste = await _trataHabilitadaVialidadRepository.Query().AnyAsync(x => x.CodigoTrata == codigoTrata, cancellationToken);
        if (!trataExiste) throw new InvalidOperationException($"La trata '{codigoTrata}' no está habilitada localmente.");

        var configuracion = await _configuracionDescubrimientoTrataRepository.Query().FirstOrDefaultAsync(x => x.CodigoTrata == codigoTrata, cancellationToken);
        if (configuracion is null)
        {
            configuracion = new ConfiguracionDescubrimientoTrataExpediente(codigoTrata, request.Habilitada, request.Prioridad);
            _configuracionDescubrimientoTrataRepository.Insert(configuracion);
        }
        else
        {
            configuracion.Actualizar(request.Habilitada, request.Prioridad);
            _configuracionDescubrimientoTrataRepository.Update(configuracion);
        }

        _configuracionDescubrimientoTrataRepository.ApplyChanges(configuracion);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new ConfiguracionTrataDescubrimientoWorkerDto(configuracion.CodigoTrata, configuracion.Habilitada, configuracion.Prioridad);
    }

    public async Task QuitarTrataDescubrimientoAsync(string codigoTrata, CancellationToken cancellationToken)
    {
        var codigoNormalizado = this.NormalizarCodigoTrata(codigoTrata);
        var configuracion = await _configuracionDescubrimientoTrataRepository.Query().FirstOrDefaultAsync(x => x.CodigoTrata == codigoNormalizado, cancellationToken);
        if (configuracion is not null)
        {
            _configuracionDescubrimientoTrataRepository.Delete(configuracion);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    private string NormalizarCodigoTrata(string codigoTrata)
    {
        if (string.IsNullOrWhiteSpace(codigoTrata)) throw new ArgumentException("El código de trata es requerido.", nameof(codigoTrata));
        return codigoTrata.Trim().ToUpperInvariant();
    }
}
