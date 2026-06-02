using ServicioSistemaWebProxyGdebaDvba.Domain.Common;

namespace ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

public sealed class ExpedienteDocumento : DomainEntity
{
    private ExpedienteDocumento()
    {
    }

    public ExpedienteDocumento(Guid expedienteId, Guid documentoId)
    {
        ExpedienteId = expedienteId == Guid.Empty
            ? throw new ArgumentException("El expediente es requerido.", nameof(expedienteId))
            : expedienteId;
        DocumentoId = documentoId == Guid.Empty
            ? throw new ArgumentException("El documento es requerido.", nameof(documentoId))
            : documentoId;
    }

    public Guid ExpedienteId { get; private set; }

    public Expediente Expediente { get; private set; } = null!;

    public Guid DocumentoId { get; private set; }

    public DocumentoGdeba Documento { get; private set; } = null!;

    public DateTimeOffset? FechaVinculacion { get; private set; }

    public void ActualizarFechaVinculacion(DateTimeOffset? fechaVinculacion)
    {
        FechaVinculacion = fechaVinculacion;
    }
}
