using ServicioSistemaWebProxyGdebaDvba.Application.Perfiles.Models;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Perfiles.Contracts;

public interface IAlertaDocumentalService
{
    Task<IReadOnlyCollection<AlertaDocumentalDto>> ListarAlertasAsync(string usuarioInstitucional, CancellationToken cancellationToken);

    Task<AlertaDocumentalDto> CrearAlertaAsync(string usuarioInstitucional, GuardarAlertaDocumentalRequest request, CancellationToken cancellationToken);

    Task<AlertaDocumentalDto> ActualizarAlertaAsync(string usuarioInstitucional, Guid alertaId, GuardarAlertaDocumentalRequest request, CancellationToken cancellationToken);

    Task EliminarAlertaAsync(string usuarioInstitucional, Guid alertaId, CancellationToken cancellationToken);

    Task<NovedadesAlertasDocumentalesDto> ListarNovedadesAsync(string usuarioInstitucional, CancellationToken cancellationToken);

    Task<HistoricoAlertasDocumentalesDto> ConsultarHistoricoAsync(string usuarioInstitucional, ConsultaHistoricoAlertasRequest request, CancellationToken cancellationToken);

    Task<PendientesAlertasDocumentalesDto> ConsultarPendientesAsync(string usuarioInstitucional, DateTimeOffset? detectadasDespuesDe, CancellationToken cancellationToken);

    Task MarcarVistasAsync(string usuarioInstitucional, CancellationToken cancellationToken);

    Task<bool> MarcarVistaAsync(string usuarioInstitucional, Guid coincidenciaId, CancellationToken cancellationToken);

    Task<ResumenHistoricoAlertasDto> ResumirHistoricoAsync(string usuarioInstitucional, CancellationToken cancellationToken);

    Task<int> MarcarHistoricoRevisadoAsync(string usuarioInstitucional, IReadOnlyCollection<Guid>? coincidenciaIds, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<TipoDocumentoAlertaDto>> ListarTiposDocumentoAsync(CancellationToken cancellationToken);
}
