using ServicioSistemaWebProxyGdebaDvba.Domain.Common;

namespace ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

public sealed class ExpedienteCache : Entity
{
    private ExpedienteCache()
    {
    }

    public ExpedienteCache(string numeroExpediente, string? estado, string? codigoTrata, DateTimeOffset cachedAt)
    {
        NumeroExpediente = numeroExpediente;
        Estado = estado;
        CodigoTrata = codigoTrata;
        CachedAt = cachedAt;
    }

    public string NumeroExpediente { get; private set; } = string.Empty;

    public string? Estado { get; private set; }

    public string? CodigoTrata { get; private set; }

    public DateTimeOffset CachedAt { get; private set; }

    public DateTimeOffset? ExpiresAt { get; private set; }

    public bool TieneDatosParciales { get; private set; }
}
