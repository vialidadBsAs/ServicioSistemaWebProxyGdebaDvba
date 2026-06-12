using ServicioSistemaWebProxyGdebaDvba.Domain.Common;

namespace ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

public sealed class OperacionGdeba : DomainEntity
{
    private readonly List<InvocacionGdeba> _invocaciones = new();

    private OperacionGdeba()
    {
    }

    public OperacionGdeba(string servicio, string metodo, int? limiteDiario)
    {
        Servicio = NormalizarRequerido(servicio, nameof(servicio));
        Metodo = NormalizarRequerido(metodo, nameof(metodo));
        LimiteDiario = limiteDiario;
        Activa = true;
    }

    public string Servicio { get; private set; } = string.Empty;

    public string Metodo { get; private set; } = string.Empty;

    public int? LimiteDiario { get; private set; }

    public bool Activa { get; private set; }

    public IReadOnlyCollection<InvocacionGdeba> Invocaciones => _invocaciones;

    private static string NormalizarRequerido(string value, string parameterName)
    {
        return string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("El valor es requerido.", parameterName)
            : value.Trim();
    }
}
