using Microsoft.EntityFrameworkCore;
using ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Persistence;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence;

public sealed class ExpedienteCacheReadStore : IExpedienteCacheReadStore
{
    private readonly ProxyGdebaDbContext _dbContext;

    public ExpedienteCacheReadStore(ProxyGdebaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Expediente? BuscarExpedienteParaDetalle(string numeroGdebaCompleto)
    {
        return _dbContext.Expedientes
            .Include(x => x.CacheControl)
            .Include(x => x.Trata)
            .Include(x => x.Documentos)
                .ThenInclude(x => x.Documento)
            .Include(x => x.ArchivosAdjuntos)
            .Include(x => x.Relaciones)
            .FirstOrDefault(x => x.GdebaNumeroCompleto == numeroGdebaCompleto);
    }

    public TrataGdeba? BuscarTrataPorCodigo(string codigo)
    {
        return _dbContext.Tratas.FirstOrDefault(x => x.Codigo == codigo);
    }

    public IReadOnlyDictionary<string, DocumentoGdeba> BuscarDocumentosPorNumeroActuacion(
        IEnumerable<string> numerosActuacionCompletos)
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

        return _dbContext.Documentos
            .Where(x => numeros.Contains(x.NumeroActuacionCompleto))
            .ToDictionary(x => x.NumeroActuacionCompleto, StringComparer.OrdinalIgnoreCase);
    }
}
