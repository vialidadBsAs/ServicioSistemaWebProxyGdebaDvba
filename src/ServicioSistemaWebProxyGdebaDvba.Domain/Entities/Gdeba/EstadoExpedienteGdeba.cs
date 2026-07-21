using ServicioSistemaWebProxyGdebaDvba.Domain.Common;

namespace ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

public sealed class EstadoExpedienteGdeba : DomainEntity
{
    private EstadoExpedienteGdeba()
    {
    }

    public EstadoExpedienteGdeba(string nombreGdeba, bool habilitadoParaDescubrimiento, int prioridadDescubrimiento)
    {
        NombreGdeba = NormalizarRequerido(nombreGdeba, nameof(nombreGdeba));
        HabilitadoParaDescubrimiento = habilitadoParaDescubrimiento;
        PrioridadDescubrimiento = prioridadDescubrimiento;
    }

    public string NombreGdeba { get; private set; } = string.Empty;

    public bool HabilitadoParaDescubrimiento { get; private set; }

    public int PrioridadDescubrimiento { get; private set; }

    public void ConfigurarDescubrimiento(bool habilitadoParaDescubrimiento, int prioridadDescubrimiento)
    {
        MarcarComoModificada();
        HabilitadoParaDescubrimiento = habilitadoParaDescubrimiento;
        PrioridadDescubrimiento = prioridadDescubrimiento;
    }

    private static string NormalizarRequerido(string value, string parameterName)
    {
        return string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("El valor es requerido.", parameterName)
            : value.Trim();
    }
}
