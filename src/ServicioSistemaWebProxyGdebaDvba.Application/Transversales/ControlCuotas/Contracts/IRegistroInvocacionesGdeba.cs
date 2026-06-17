using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.ControlCuotas.Models;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Transversales.ControlCuotas.Contracts;

public interface IRegistroInvocacionesGdeba
{
    Task AgregarInvocacionAsync(
        string servicio,
        string metodo,
        ContextoInvocacionGdeba contextoInvocacion,
        bool servidorRespondio,
        bool exitosa,
        int? estadoHttp,
        long? duracionMilisegundos,
        CancellationToken cancellationToken);
}
