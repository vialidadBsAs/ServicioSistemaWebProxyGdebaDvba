using ServicioSistemaWebProxyGdebaDvba.Domain.Common;

namespace ServicioSistemaWebProxyGdebaDvba.Domain.Entities.Configuracion;

public sealed class ConfiguracionDescubrimientoTrataExpediente : DomainEntity
{
    private ConfiguracionDescubrimientoTrataExpediente()
    {
    }

    public ConfiguracionDescubrimientoTrataExpediente(string codigoTrata, bool habilitada, int prioridad)
    {
        CodigoTrata = NormalizarRequerido(codigoTrata, nameof(codigoTrata));
        Habilitada = habilitada;
        Prioridad = prioridad;
    }

    public string CodigoTrata { get; private set; } = string.Empty;

    public bool Habilitada { get; private set; }

    public int Prioridad { get; private set; }

    public void Actualizar(bool habilitada, int prioridad)
    {
        Habilitada = habilitada;
        Prioridad = prioridad;
        this.MarcarComoModificada();
    }

    private static string NormalizarRequerido(string value, string parameterName)
    {
        return string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("El valor es requerido.", parameterName)
            : value.Trim();
    }
}
