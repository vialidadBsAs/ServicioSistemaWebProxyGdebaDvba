namespace ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Contracts;

public interface IExpedienteRefreshCoordinator
{
    Task<T> ExecuteAsync<T>(
        string numeroGdebaCompleto,
        string operacionGdeba,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken);
}
