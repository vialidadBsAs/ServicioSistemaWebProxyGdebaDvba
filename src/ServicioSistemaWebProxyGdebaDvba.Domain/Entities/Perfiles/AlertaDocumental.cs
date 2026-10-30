using ServicioSistemaWebProxyGdebaDvba.Domain.Common;

namespace ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

// Alerta personal sobre documentos: "avisame cuando aparezca un documento de este tipo", acotable por trata del expediente y por
// usuario generador. La definicion es del usuario; las coincidencias las produce la deteccion institucional, sin importar quien
// trajo el documento (consulta interactiva, refresco manual o worker).
public sealed partial class AlertaDocumental : DomainEntity
{
    private readonly List<CoincidenciaAlertaDocumental> _coincidencias = new();

    private AlertaDocumental()
    {
    }

    public AlertaDocumental(Guid perfilUsuarioId, string tipoDocumentoCodigo, Guid? trataId, string? usuarioGenerador, string? nombre, DateTimeOffset fechaCreacion)
    {
        PerfilUsuarioId = perfilUsuarioId == Guid.Empty
            ? throw new ArgumentException("El perfil es requerido.", nameof(perfilUsuarioId))
            : perfilUsuarioId;
        FechaCreacion = fechaCreacion;
        Activa = true;
        this.ActualizarCriterios(tipoDocumentoCodigo, trataId, usuarioGenerador, nombre);
    }

    public Guid PerfilUsuarioId { get; private set; }

    public PerfilUsuario PerfilUsuario { get; private set; } = null!;

    // Nombre libre que el usuario le da a la alerta; si no lo carga, la pantalla la describe por sus criterios.
    public string? Nombre { get; private set; }

    // Acronimo GDEBA del tipo documental (RESOL, DI, IF...), tal como figura en el numero de actuacion. Es el unico criterio obligatorio.
    public string TipoDocumentoCodigo { get; private set; } = string.Empty;

    public Guid? TrataId { get; private set; }

    public TrataHabilitadaVialidad? Trata { get; private set; }

    public string? UsuarioGenerador { get; private set; }

    public bool Activa { get; private set; }

    // Una alerta coincide solo con documentos posteriores a su creacion: enterarse de lo que ocurre, no auditar el pasado.
    public DateTimeOffset FechaCreacion { get; private set; }

    public IReadOnlyCollection<CoincidenciaAlertaDocumental> Coincidencias => _coincidencias;

    private static string? Normalizar(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
