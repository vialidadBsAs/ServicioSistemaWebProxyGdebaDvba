namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Gdeba;

public interface IGdebaJwtTokenProvider
{
    Task<string> ObtenerTokenAsync(CancellationToken cancellationToken);

    void InvalidarToken();
}
