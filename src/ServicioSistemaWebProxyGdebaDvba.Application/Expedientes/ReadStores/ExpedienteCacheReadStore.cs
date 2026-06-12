using ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Persistence;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities;
using URF.Core.Abstractions;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.ReadStores;

public sealed class ExpedienteCacheReadStore : IExpedienteCacheReadStore
{
    private readonly IRepository<Expediente> _expedienteRepository;
    private readonly IRepository<DocumentoGdeba> _documentoRepository;
    private readonly IRepository<TrataGdeba> _trataRepository;
    private readonly IRepository<MovimientoExpediente> _movimientoRepository;
    private readonly IRepository<ExpedienteDocumento> _expedienteDocumentoRepository;
    private readonly IRepository<ExpedienteRelacion> _expedienteRelacionRepository;
    private readonly IRepository<ArchivoAdjuntoExpediente> _archivoAdjuntoRepository;

    public ExpedienteCacheReadStore( IRepository<Expediente> expedienteRepository, IRepository<DocumentoGdeba> documentoRepository, IRepository<TrataGdeba> trataRepository,
        IRepository<MovimientoExpediente> movimientoRepository, IRepository<ExpedienteDocumento> expedienteDocumentoRepository, IRepository<ExpedienteRelacion> expedienteRelacionRepository,
        IRepository<ArchivoAdjuntoExpediente> archivoAdjuntoRepository)
    {
        _expedienteRepository = expedienteRepository;
        _documentoRepository = documentoRepository;
        _trataRepository = trataRepository;
        _movimientoRepository = movimientoRepository;
        _expedienteDocumentoRepository = expedienteDocumentoRepository;
        _expedienteRelacionRepository = expedienteRelacionRepository;
        _archivoAdjuntoRepository = archivoAdjuntoRepository;
    }

    public async Task<Expediente?> CargarExpedienteAsync(
        string numeroGdebaCompleto,
        CancellationToken cancellationToken)
    {
        var expediente = await _expedienteRepository
            .Query()
            .Include(x => x.CacheControl)
            .Include(x => x.HistorialCacheControl)
            .Include(x => x.Trata)
            .FirstOrDefaultAsync(
                x => x.GdebaNumeroCompleto == numeroGdebaCompleto,
                cancellationToken);

        if (expediente is null)
        {
            return null;
        }

        await _movimientoRepository
            .Query()
            .Where(x => x.ExpedienteId == expediente.Id)
            .SelectAsync(cancellationToken);

        await _expedienteDocumentoRepository
            .Query()
            .Include(x => x.Documento)
            .Where(x => x.ExpedienteId == expediente.Id)
            .SelectAsync(cancellationToken);

        await _archivoAdjuntoRepository
            .Query()
            .Where(x => x.ExpedienteId == expediente.Id)
            .SelectAsync(cancellationToken);

        await _expedienteRelacionRepository
            .Query()
            .Where(x => x.ExpedienteOrigenId == expediente.Id)
            .SelectAsync(cancellationToken);

        return expediente;
    }

    public async Task<TrataGdeba?> BuscarTrataPorCodigoAsync(
        string codigo,
        CancellationToken cancellationToken)
    {
        return await _trataRepository
            .Query()
            .FirstOrDefaultAsync(x => x.Codigo == codigo, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<string, DocumentoGdeba>> BuscarDocumentosPorNumeroActuacionAsync(
        IEnumerable<string> numerosActuacionCompletos,
        CancellationToken cancellationToken)
    {
        var numeros = numerosActuacionCompletos
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (numeros.Length == 0)
        {
            return new Dictionary<string, DocumentoGdeba>(StringComparer.OrdinalIgnoreCase);
        }

        var documentos = await _documentoRepository
            .Query()
            .Where(x => numeros.Contains(x.NumeroActuacionCompleto))
            .SelectAsync(cancellationToken);

        return documentos.ToDictionary(
            x => x.NumeroActuacionCompleto,
            StringComparer.OrdinalIgnoreCase);
    }
}
