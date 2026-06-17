using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Transversales.ControlCuotas.Models;

public sealed record ContextoInvocacionGdeba(
    OrigenInvocacionGdeba Origen,
    Guid SolicitudId,
    int NumeroIntento)
{
    public static ContextoInvocacionGdeba Crear(OrigenInvocacionGdeba origen)
    {
        return new ContextoInvocacionGdeba(origen, Guid.NewGuid(), 1);
    }

    public ContextoInvocacionGdeba ParaIntento(int numeroIntento)
    {
        return this with
        {
            NumeroIntento = numeroIntento > 0
                ? numeroIntento
                : throw new ArgumentOutOfRangeException(nameof(numeroIntento))
        };
    }
}
