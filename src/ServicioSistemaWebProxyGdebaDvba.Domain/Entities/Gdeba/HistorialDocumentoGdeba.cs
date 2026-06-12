using ServicioSistemaWebProxyGdebaDvba.Domain.Common;

namespace ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

public sealed class HistorialDocumentoGdeba : DomainEntity
{
    private HistorialDocumentoGdeba()
    {
    }

    public HistorialDocumentoGdeba(Guid documentoId, long idGdeba)
    {
        DocumentoId = documentoId == Guid.Empty
            ? throw new ArgumentException("El documento es requerido.", nameof(documentoId))
            : documentoId;
        IdGdeba = idGdeba;
    }

    public Guid DocumentoId { get; private set; }

    public DocumentoGdeba Documento { get; private set; } = null!;

    public long IdGdeba { get; private set; }

    public string? Actividad { get; private set; }

    public DateTimeOffset? FechaInicio { get; private set; }

    public DateTimeOffset? FechaFin { get; private set; }

    public string? Usuario { get; private set; }

    public string? NombreUsuario { get; private set; }

    public string? WorkflowOrigen { get; private set; }

    public void Actualizar(string? actividad, DateTimeOffset? fechaInicio, DateTimeOffset? fechaFin, string? usuario, string? nombreUsuario, string? workflowOrigen)
    {
        MarcarComoModificada();
        Actividad = Normalizar(actividad);
        FechaInicio = fechaInicio;
        FechaFin = fechaFin;
        Usuario = Normalizar(usuario);
        NombreUsuario = Normalizar(nombreUsuario);
        WorkflowOrigen = Normalizar(workflowOrigen);
    }

    private static string? Normalizar(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
