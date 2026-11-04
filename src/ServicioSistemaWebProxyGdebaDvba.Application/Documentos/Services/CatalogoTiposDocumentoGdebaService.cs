using ServicioSistemaWebProxyGdebaDvba.Application.Documentos.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Documentos.Models;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities;
using URF.Core.Abstractions.Trackable;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Documentos.Services;

public sealed class CatalogoTiposDocumentoGdebaService : ICatalogoTiposDocumentoGdebaService
{
    private readonly ITrackableRepository<TipoDocumentoGdeba> _tipoDocumentoRepository;

    public CatalogoTiposDocumentoGdebaService(ITrackableRepository<TipoDocumentoGdeba> tipoDocumentoRepository)
    {
        _tipoDocumentoRepository = tipoDocumentoRepository;
    }

    public TipoDocumentoGdeba IncorporarRespuestaGdeba(TipoDocumentoGdeba? tipoLocal, string codigoConsultado, GdebaTipoDocumentoDto detalle)
    {
        ArgumentNullException.ThrowIfNull(detalle);
        string codigoNormalizado = string.IsNullOrWhiteSpace(codigoConsultado)
            ? throw new ArgumentException("El codigo del tipo documental es requerido.", nameof(codigoConsultado))
            : codigoConsultado.Trim().ToUpperInvariant();

        // La fila existente conserva su codigo: si GDEBA respondiera con otro acronimo no se abre una segunda fila.
        string codigoCatalogo = tipoLocal?.Codigo
            ?? (string.IsNullOrWhiteSpace(detalle.Acronimo) ? codigoNormalizado : detalle.Acronimo.Trim().ToUpperInvariant());
        string nombre = string.IsNullOrWhiteSpace(detalle.Nombre)
            ? detalle.Descripcion ?? tipoLocal?.Nombre ?? codigoCatalogo
            : detalle.Nombre;
        bool tipoEsNuevo = tipoLocal is null;
        TipoDocumentoGdeba tipoDocumento = tipoLocal ?? new TipoDocumentoGdeba(codigoCatalogo, nombre);
        tipoDocumento.ActualizarMetadata(
            nombre,
            detalle.CodigoTipoDocumentoGdeba,
            detalle.Descripcion,
            detalle.Familia,
            detalle.TipoProduccion,
            detalle.Estado,
            detalle.EsAutomatica,
            detalle.EsComunicable,
            detalle.EsConfidencial,
            detalle.EsEmbebido,
            detalle.EsEspecial,
            detalle.EsFirmaConjunta,
            detalle.EsFirmaExterna,
            detalle.EsManual,
            detalle.EsNotificable,
            detalle.TieneTemplate,
            detalle.TieneToken,
            esResolucion: tipoDocumento.EsResolucion,
            activo: !string.Equals(detalle.Estado, "BAJA", StringComparison.OrdinalIgnoreCase));
        if (tipoEsNuevo)
        {
            _tipoDocumentoRepository.Insert(tipoDocumento);
        }
        else
        {
            _tipoDocumentoRepository.Update(tipoDocumento);
            _tipoDocumentoRepository.ApplyChanges(tipoDocumento);
        }

        return tipoDocumento;
    }
}
