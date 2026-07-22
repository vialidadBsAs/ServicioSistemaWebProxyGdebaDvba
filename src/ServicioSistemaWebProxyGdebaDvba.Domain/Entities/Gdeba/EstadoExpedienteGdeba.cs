using ServicioSistemaWebProxyGdebaDvba.Domain.Common;

namespace ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

public sealed class EstadoExpedienteGdeba : DomainEntity
{
    private EstadoExpedienteGdeba()
    {
    }

    public EstadoExpedienteGdeba(string nombreGdeba)
    {
        NombreGdeba = NormalizarRequerido(nombreGdeba, nameof(nombreGdeba));
    }

    public string NombreGdeba { get; private set; } = string.Empty;

    private static string NormalizarRequerido(string value, string parameterName)
    {
        return string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("El valor es requerido.", parameterName)
            : value.Trim();
    }
}
