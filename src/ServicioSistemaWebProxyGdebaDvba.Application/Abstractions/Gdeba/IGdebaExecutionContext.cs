using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Gdeba;

public interface IGdebaExecutionContext
{
    AmbienteGdeba Ambiente { get; }

    string EnvironmentName { get; }
}
