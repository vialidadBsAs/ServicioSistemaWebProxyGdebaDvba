using ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Gdeba;
using ServicioSistemaWebProxyGdebaDvba.Application.Documentos.Models;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.ControlCuotas.Models;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Gdeba;

public sealed class FakeGdebaTipoDocumentoGateway : IGdebaTipoDocumentoGateway
{
    public Task<GdebaTipoDocumentoDto?> ConsultarTipoDocumentoAsync(string acronimo, ContextoInvocacionGdeba contextoInvocacion, CancellationToken cancellationToken)
    {
        var codigo = string.IsNullOrWhiteSpace(acronimo)
            ? throw new ArgumentException("El acronimo del tipo documental es requerido.", nameof(acronimo))
            : acronimo.Trim().ToUpperInvariant();
        var tipoDocumento = new GdebaTipoDocumentoDto(
            codigo,
            string.Equals(codigo, "RESO", StringComparison.OrdinalIgnoreCase) ? "RS" : null,
            string.Equals(codigo, "RESO", StringComparison.OrdinalIgnoreCase) ? "Resolucion" : codigo,
            string.Equals(codigo, "RESO", StringComparison.OrdinalIgnoreCase) ? "Resolucion." : null,
            string.Equals(codigo, "RESO", StringComparison.OrdinalIgnoreCase) ? "Acto Administrativo" : null,
            "LIBRE",
            "ALTA",
            EsAutomatica: null,
            EsComunicable: null,
            EsConfidencial: null,
            EsEmbebido: null,
            EsEspecial: null,
            EsFirmaConjunta: null,
            EsFirmaExterna: null,
            EsManual: null,
            EsNotificable: null,
            TieneTemplate: null,
            TieneToken: null);

        return Task.FromResult<GdebaTipoDocumentoDto?>(tipoDocumento);
    }
}
