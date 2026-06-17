using ServicioSistemaWebProxyGdebaDvba.Domain.Common;

namespace ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

public sealed class OperacionGdeba : DomainEntity
{
    private readonly List<InvocacionGdeba> _invocaciones = new();

    private OperacionGdeba()
    {
    }

    public OperacionGdeba(
        string servicio,
        string metodo,
        int? limiteDiario,
        decimal umbralAdvertenciaPorcentaje = 80m,
        decimal umbralCriticoPorcentaje = 90m)
    {
        Servicio = NormalizarRequerido(servicio, nameof(servicio));
        Metodo = NormalizarRequerido(metodo, nameof(metodo));
        LimiteDiario = limiteDiario;
        UmbralAdvertenciaPorcentaje = ValidarPorcentaje(
            umbralAdvertenciaPorcentaje,
            nameof(umbralAdvertenciaPorcentaje));
        UmbralCriticoPorcentaje = ValidarPorcentaje(
            umbralCriticoPorcentaje,
            nameof(umbralCriticoPorcentaje));

        if (UmbralCriticoPorcentaje < UmbralAdvertenciaPorcentaje)
        {
            throw new ArgumentException("El umbral critico no puede ser menor al umbral de advertencia.");
        }

        Activa = true;
    }

    public string Servicio { get; private set; } = string.Empty;

    public string Metodo { get; private set; } = string.Empty;

    public int? LimiteDiario { get; private set; }

    public decimal UmbralAdvertenciaPorcentaje { get; private set; }

    public decimal UmbralCriticoPorcentaje { get; private set; }

    public bool Activa { get; private set; }

    public IReadOnlyCollection<InvocacionGdeba> Invocaciones => _invocaciones;

    private static string NormalizarRequerido(string value, string parameterName)
    {
        return string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("El valor es requerido.", parameterName)
            : value.Trim();
    }

    private static decimal ValidarPorcentaje(decimal value, string parameterName)
    {
        return value is >= 0 and <= 100
            ? value
            : throw new ArgumentOutOfRangeException(parameterName, "El porcentaje debe estar entre 0 y 100.");
    }
}
