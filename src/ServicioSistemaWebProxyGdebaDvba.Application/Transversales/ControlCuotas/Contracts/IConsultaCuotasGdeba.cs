using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.ControlCuotas.Models;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Transversales.ControlCuotas.Contracts;

public interface IConsultaCuotasGdeba
{
    Task<ConsultaCuotasGdebaResult> ConsultarCuotasAsync(DateOnly fecha, CancellationToken cancellationToken);
}
