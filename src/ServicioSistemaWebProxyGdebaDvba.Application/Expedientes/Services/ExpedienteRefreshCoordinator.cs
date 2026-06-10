using System.Collections.Concurrent;
using ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Contracts;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Services;

public sealed class ExpedienteRefreshCoordinator : IExpedienteRefreshCoordinator
{
    private readonly ConcurrentDictionary<string, LockEntry> _locks =
        new(StringComparer.OrdinalIgnoreCase);

    public async Task<T> ExecuteAsync<T>(
        string numeroGdebaCompleto,
        string operacionGdeba,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(action);

        var key = $"{numeroGdebaCompleto}:{operacionGdeba}";
        var entry = AcquireEntry(key);

        try
        {
            await entry.Semaphore.WaitAsync(cancellationToken);
            try
            {
                return await action(cancellationToken);
            }
            finally
            {
                entry.Semaphore.Release();
            }
        }
        finally
        {
            ReleaseEntry(key, entry);
        }
    }

    private LockEntry AcquireEntry(string key)
    {
        while (true)
        {
            var entry = _locks.GetOrAdd(key, static _ => new LockEntry());
            lock (entry.SyncRoot)
            {
                if (entry.Removed)
                {
                    continue;
                }

                entry.Users++;
                return entry;
            }
        }
    }

    private void ReleaseEntry(string key, LockEntry entry)
    {
        lock (entry.SyncRoot)
        {
            entry.Users--;
            if (entry.Users != 0)
            {
                return;
            }

            entry.Removed = true;
            _locks.TryRemove(new KeyValuePair<string, LockEntry>(key, entry));
        }
    }

    private sealed class LockEntry
    {
        public object SyncRoot { get; } = new();

        public SemaphoreSlim Semaphore { get; } = new(1, 1);

        public int Users { get; set; }

        public bool Removed { get; set; }
    }
}
