using ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Persistence;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities;
using URF.Core.Abstractions;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.ReadStores;

public sealed class ExpedienteCacheReadStore : IExpedienteCacheReadStore
{
    private readonly IRepository<Expediente> _expedienteRepository;
    private readonly IRepository<DocumentoGdeba> _documentoRepository;
    private readonly IRepository<TrataGdeba> _trataRepository;

    public ExpedienteCacheReadStore(
        IRepository<Expediente> expedienteRepository,
        IRepository<DocumentoGdeba> documentoRepository,
        IRepository<TrataGdeba> trataRepository)
    {
        _expedienteRepository = expedienteRepository;
        _documentoRepository = documentoRepository;
        _trataRepository = trataRepository;
    }

    public async Task<Expediente?> BuscarExpedienteParaDetalleAsync(
        string numeroGdebaCompleto,
        CancellationToken cancellationToken)
    {
        return await _expedienteRepository
            .Query()
            .Include(x => x.CacheControl)
            .Include(x => x.Trata)
            .Include($"{nameof(Expediente.Documentos)}.{nameof(ExpedienteDocumento.Documento)}")
            .Include(x => x.ArchivosAdjuntos)
            .Include(x => x.Relaciones)
            .FirstOrDefaultAsync(
                x => x.GdebaNumeroCompleto == numeroGdebaCompleto,
                cancellationToken);
    }

    public async Task<Expediente?> BuscarExpedienteParaMovimientosAsync(
        string numeroGdebaCompleto,
        CancellationToken cancellationToken)
    {
        return await _expedienteRepository
            .Query()
            .Include(x => x.CacheControl)
            .Include(x => x.HistorialCacheControl)
            .Include(x => x.Movimientos)
            .Include($"{nameof(Expediente.Documentos)}.{nameof(ExpedienteDocumento.Documento)}")
            .Include(x => x.Relaciones)
            .FirstOrDefaultAsync(
                x => x.GdebaNumeroCompleto == numeroGdebaCompleto,
                cancellationToken);
    }

    public async Task<Expediente?> BuscarExpedienteCompletoAsync(
        string numeroGdebaCompleto,
        CancellationToken cancellationToken)
    {
        return await _expedienteRepository
            .Query()
            .Include(x => x.CacheControl)
            .Include(x => x.HistorialCacheControl)
            .Include(x => x.Trata)
            .Include(x => x.Movimientos)
            .Include($"{nameof(Expediente.Documentos)}.{nameof(ExpedienteDocumento.Documento)}")
            .Include(x => x.ArchivosAdjuntos)
            .Include(x => x.Relaciones)
            .FirstOrDefaultAsync(
                x => x.GdebaNumeroCompleto == numeroGdebaCompleto,
                cancellationToken);
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
