using ServicioSistemaWebProxyGdebaDvba.Domain.Common;
using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;

namespace ServicioSistemaWebProxyGdebaDvba.Domain.Entities.Worker;

public sealed class OmisionCorridaProgramadaWorker : DomainEntity
{
    private OmisionCorridaProgramadaWorker()
    {
    }

    public OmisionCorridaProgramadaWorker(ProcesoWorker proceso, DateOnly fechaLocal, string omitidaPor, DateTimeOffset fechaRegistro)
    {
        Proceso = proceso;
        FechaLocal = fechaLocal;
        OmitidaPor = string.IsNullOrWhiteSpace(omitidaPor) ? "Administracion" : omitidaPor.Trim();
        FechaRegistro = fechaRegistro;
    }

    public ProcesoWorker Proceso { get; private set; }
    public DateOnly FechaLocal { get; private set; }
    public string OmitidaPor { get; private set; } = string.Empty;
    public DateTimeOffset FechaRegistro { get; private set; }
}
