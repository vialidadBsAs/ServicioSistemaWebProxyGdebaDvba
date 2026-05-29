using Microsoft.Extensions.Options;
using ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Gdeba;
using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Gdeba;

public sealed class GdebaExecutionContext : IGdebaExecutionContext
{
    public GdebaExecutionContext(IOptions<GdebaOptions> options)
    {
        EnvironmentName = string.IsNullOrWhiteSpace(options.Value.CurrentEnvironment)
            ? GdebaEnvironmentNames.Hml
            : options.Value.CurrentEnvironment.Trim().ToUpperInvariant();

        Ambiente = EnvironmentName switch
        {
            GdebaEnvironmentNames.Hml => AmbienteGdeba.Hml,
            GdebaEnvironmentNames.Prod => AmbienteGdeba.Prod,
            _ => throw new InvalidOperationException($"Ambiente GDEBA no soportado: '{EnvironmentName}'.")
        };
    }

    public AmbienteGdeba Ambiente { get; }

    public string EnvironmentName { get; }
}
