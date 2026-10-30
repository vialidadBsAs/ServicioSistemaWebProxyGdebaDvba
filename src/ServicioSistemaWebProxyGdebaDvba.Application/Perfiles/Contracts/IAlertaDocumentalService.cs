using ServicioSistemaWebProxyGdebaDvba.Application.Perfiles.Models;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Perfiles.Contracts;

public interface IAlertaDocumentalService
{
    Task<IReadOnlyCollection<AlertaDocumentalDto>> ListarAlertasAsync(string usuarioInstitucional, CancellationToken cancellationToken);

    Task<AlertaDocumentalDto> CrearAlertaAsync(string usuarioInstitucional, GuardarAlertaDocumentalRequest request, CancellationToken cancellationToken);

    Task<AlertaDocumentalDto> ActualizarAlertaAsync(string usuarioInstitucional, Guid alertaId, GuardarAlertaDocumentalRequest request, CancellationToken cancellationToken);

    Task EliminarAlertaAsync(string usuarioInstitucional, Guid alertaId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<CoincidenciaAlertaDocumentalDto>> ListarCoincidenciasAsync(string usuarioInstitucional, CancellationToken cancellationToken);

    Task<PendientesAlertasDocumentalesDto> ConsultarPendientesAsync(string usuarioInstitucional, DateTimeOffset? detectadasDespuesDe, CancellationToken cancellationToken);

    Task MarcarVistasAsync(string usuarioInstitucional, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<TipoDocumentoAlertaDto>> ListarTiposDocumentoAsync(CancellationToken cancellationToken);
}
