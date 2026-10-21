using ServicioSistemaWebProxyGdebaDvba.Domain.Common;

namespace ServicioSistemaWebProxyGdebaDvba.Domain.Entities.Configuracion;

public sealed class ProcesoDescubrimientoTrataEstadoExpediente : DomainEntity
{
    private ProcesoDescubrimientoTrataEstadoExpediente()
    {
    }

    public ProcesoDescubrimientoTrataEstadoExpediente(string codigoTrata, Guid estadoExpedienteGdebaId)
    {
        CodigoTrata = string.IsNullOrWhiteSpace(codigoTrata) ? throw new ArgumentException("El codigo de trata es requerido.", nameof(codigoTrata)) : codigoTrata.Trim();
        EstadoExpedienteGdebaId = estadoExpedienteGdebaId == Guid.Empty ? throw new ArgumentException("El estado GDEBA es requerido.", nameof(estadoExpedienteGdebaId)) : estadoExpedienteGdebaId;
    }

    public string CodigoTrata { get; private set; } = string.Empty;
    public Guid EstadoExpedienteGdebaId { get; private set; }
    public DateTimeOffset? FechaUltimaConsulta { get; private set; }
    public DateTimeOffset? FechaUltimoResultadoHabilitado { get; private set; }
    public int ConsultasSinResultadosConsecutivas { get; private set; }
    public DateTimeOffset? OmitirHasta { get; private set; }

    public void RegistrarResultado(DateTimeOffset fecha, bool huboResultadosHabilitados, int umbralPausa, int diasPausa)
    {
        FechaUltimaConsulta = fecha;
        if (huboResultadosHabilitados)
        {
            FechaUltimoResultadoHabilitado = fecha;
            ConsultasSinResultadosConsecutivas = 0;
            OmitirHasta = null;
        }
        else
        {
            ConsultasSinResultadosConsecutivas++;
            OmitirHasta = ConsultasSinResultadosConsecutivas >= Math.Max(1, umbralPausa) ? fecha.AddDays(Math.Max(1, diasPausa)) : null;
        }

        MarcarComoModificada();
    }

    /// <summary>
    /// GDEBA no pudo responder la consulta (fault del servicio, respuesta que supera sus limites, rechazo del gateway). Cuenta como
    /// consulta realizada sin resultados: asi la combinacion no vuelve a quedar primera por "nunca consultada", el interruptor del dia la
    /// saltea y, si falla de forma consecutiva, entra en pausa como una combinacion vacia.
    /// </summary>
    public void RegistrarConsultaFallida(DateTimeOffset fecha, int umbralPausa, int diasPausa)
    {
        this.RegistrarResultado(fecha, huboResultadosHabilitados: false, umbralPausa, diasPausa);
    }
}
