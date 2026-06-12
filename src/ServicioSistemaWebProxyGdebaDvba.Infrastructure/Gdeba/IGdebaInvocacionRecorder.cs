namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Gdeba;

public interface IGdebaInvocacionRecorder
{
    Task RegistrarAsync(string servicio, string metodo, bool exitosa, int? estadoHttp, CancellationToken cancellationToken);
}
