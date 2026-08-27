using ServicioSistemaWebProxyGdebaDvba.Application.Perfiles.Models;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Perfiles.Contracts;

public interface IPerfilUsuarioService
{
    Task<PerfilUsuarioDto> ObtenerAsync(string usuarioInstitucional, CancellationToken cancellationToken);

    Task<PerfilUsuarioDto> GuardarUsuarioGdebaAsync(string usuarioInstitucional, string? usuarioGdeba, CancellationToken cancellationToken);

    Task<string?> ObtenerUsuarioGdebaAsync(string usuarioInstitucional, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<SeguimientoExpedienteDto>> ListarSeguimientosAsync(string usuarioInstitucional, CancellationToken cancellationToken);

    Task SeguirExpedienteAsync(string usuarioInstitucional, Guid expedienteId, CancellationToken cancellationToken);

    Task DejarDeSeguirAsync(string usuarioInstitucional, Guid expedienteId, CancellationToken cancellationToken);

    Task MarcarVistoAsync(string usuarioInstitucional, Guid expedienteId, CancellationToken cancellationToken);

    Task<bool> EstaSiguiendoAsync(string usuarioInstitucional, string numeroGdebaCompleto, CancellationToken cancellationToken);

    Task SeguirExpedientePorNumeroAsync(string usuarioInstitucional, string numeroGdebaCompleto, CancellationToken cancellationToken);

    Task DejarDeSeguirPorNumeroAsync(string usuarioInstitucional, string numeroGdebaCompleto, CancellationToken cancellationToken);

    Task MarcarVistoPorNumeroAsync(string usuarioInstitucional, string numeroGdebaCompleto, CancellationToken cancellationToken);

    Task<AperturaSeguimientoDto> AbrirExpedientePorNumeroAsync(string usuarioInstitucional, string numeroGdebaCompleto, CancellationToken cancellationToken);
}
