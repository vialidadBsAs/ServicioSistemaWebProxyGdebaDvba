using System.Globalization;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ServicioSistemaWebProxyGdebaDvba.Application.Consultas.Models;
using ServicioSistemaWebProxyGdebaDvba.Application.Consultas.ReadStores;
using ServicioSistemaWebProxyGdebaDvba.Application.Perfiles.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Perfiles.Models;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities;
using TrackableEntities.Common.Core;
using URF.Core.Abstractions;
using URF.Core.Abstractions.Trackable;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Perfiles.Services;

public sealed class AlertaDocumentalService : IAlertaDocumentalService
{
    private const int CantidadMaximaNuevasInformadas = 20;
    private const int DiasNovedadesSinRevisarPorDefecto = 5;
    private const int TamanioPaginaMaximoHistorico = 200;

    // Cuantos dias anteriores a hoy sigue en la pestana "Alertas" una coincidencia no revisada antes de pasar al historico con su marca.
    private readonly int _diasNovedadesSinRevisar;
    private readonly ITrackableRepository<PerfilUsuario> _perfilRepository;
    private readonly ITrackableRepository<AlertaDocumental> _alertaRepository;
    private readonly IRepository<CoincidenciaAlertaDocumental> _coincidenciaRepository;
    private readonly IRepository<ExpedienteDocumento> _expedienteDocumentoRepository;
    private readonly IRepository<DocumentoGdeba> _documentoRepository;
    private readonly IRepository<TipoDocumentoGdeba> _tipoDocumentoRepository;
    private readonly IRepository<TrataHabilitadaVialidad> _trataRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AlertaDocumentalService(
        ITrackableRepository<PerfilUsuario> perfilRepository,
        ITrackableRepository<AlertaDocumental> alertaRepository,
        IRepository<CoincidenciaAlertaDocumental> coincidenciaRepository,
        IRepository<ExpedienteDocumento> expedienteDocumentoRepository,
        IRepository<DocumentoGdeba> documentoRepository,
        IRepository<TipoDocumentoGdeba> tipoDocumentoRepository,
        IRepository<TrataHabilitadaVialidad> trataRepository,
        IUnitOfWork unitOfWork,
        IConfiguration configuration)
    {
        _diasNovedadesSinRevisar = int.TryParse(configuration["AlertasDocumentales:DiasNovedadesSinRevisar"], NumberStyles.Integer, CultureInfo.InvariantCulture, out int diasConfigurados) && diasConfigurados >= 0
            ? diasConfigurados
            : AlertaDocumentalService.DiasNovedadesSinRevisarPorDefecto;
        _perfilRepository = perfilRepository;
        _alertaRepository = alertaRepository;
        _coincidenciaRepository = coincidenciaRepository;
        _expedienteDocumentoRepository = expedienteDocumentoRepository;
        _documentoRepository = documentoRepository;
        _tipoDocumentoRepository = tipoDocumentoRepository;
        _trataRepository = trataRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyCollection<AlertaDocumentalDto>> ListarAlertasAsync(string usuarioInstitucional, CancellationToken cancellationToken)
    {
        string usuario = AlertaDocumentalService.NormalizarUsuario(usuarioInstitucional);
        IEnumerable<AlertaDocumental> alertas = await _alertaRepository.Query()
            .Include(nameof(AlertaDocumental.Trata))
            .Where(x => x.PerfilUsuario.UsuarioInstitucional == usuario)
            .SelectAsync(cancellationToken);
        AlertaDocumental[] alertasUsuario = alertas.ToArray();
        if (alertasUsuario.Length == 0)
        {
            return Array.Empty<AlertaDocumentalDto>();
        }

        // El contador de cada alerta sigue al badge del menu: solo lo no revisado que todavia esta en la pestana "Alertas".
        DateTimeOffset limiteSinRevisar = this.CalcularVentanaNovedades().LimiteSinRevisar;
        Guid[] alertaIds = alertasUsuario.Select(x => x.Id).ToArray();
        Dictionary<Guid, int> pendientesPorAlerta = await _coincidenciaRepository.Queryable()
            .Where(x => alertaIds.Contains(x.AlertaDocumentalId) && x.FechaVista == null && x.FechaDeteccion >= limiteSinRevisar)
            .GroupBy(x => x.AlertaDocumentalId)
            .Select(grupo => new { AlertaId = grupo.Key, Cantidad = grupo.Count() })
            .ToDictionaryAsync(x => x.AlertaId, x => x.Cantidad, cancellationToken);
        IReadOnlyDictionary<string, string> nombresTipos = await this.CargarNombresTiposAsync(alertasUsuario.Select(x => x.TipoDocumentoCodigo), cancellationToken);
        IReadOnlyDictionary<string, string> nombresEspecificos = await this.CargarNombresEspecificosAsync(alertasUsuario.Select(x => x.TipoEspecificoCodigo), cancellationToken);

        return alertasUsuario
            .OrderBy(x => x.FechaCreacion)
            .Select(x => AlertaDocumentalService.MapearAlerta(x, nombresTipos, nombresEspecificos, pendientesPorAlerta.GetValueOrDefault(x.Id)))
            .ToArray();
    }

    public async Task<AlertaDocumentalDto> CrearAlertaAsync(string usuarioInstitucional, GuardarAlertaDocumentalRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        TrataHabilitadaVialidad? trata = await this.ValidarTrataAsync(request.TrataId, cancellationToken);
        await this.ConfirmarTipoEspecificoAsync(request.TipoDocumentoCodigo, request.TipoEspecificoCodigo, cancellationToken);
        PerfilUsuario perfil = await this.CargarOCrearPerfilConAlertasAsync(usuarioInstitucional, cancellationToken);
        AlertaDocumental alerta = perfil.CrearAlertaDocumental(
            request.TipoDocumentoCodigo, request.TipoEspecificoCodigo, trata?.Id, request.UsuarioGenerador, request.Nombre, DateTimeOffset.Now);
        if (!request.Activa)
        {
            alerta.CambiarActiva(false);
        }

        await this.ConfirmarPerfilAsync(perfil, cancellationToken);
        return await this.MapearAlertaAsync(alerta, trata, cancellationToken);
    }

    public async Task<AlertaDocumentalDto> ActualizarAlertaAsync(string usuarioInstitucional, Guid alertaId, GuardarAlertaDocumentalRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        TrataHabilitadaVialidad? trata = await this.ValidarTrataAsync(request.TrataId, cancellationToken);
        await this.ConfirmarTipoEspecificoAsync(request.TipoDocumentoCodigo, request.TipoEspecificoCodigo, cancellationToken);
        PerfilUsuario perfil = await this.CargarPerfilConAlertasAsync(usuarioInstitucional, cancellationToken)
            ?? throw new KeyNotFoundException("No existe la alerta que se intenta modificar.");
        AlertaDocumental alerta = perfil.ActualizarAlertaDocumental(
            alertaId, request.TipoDocumentoCodigo, request.TipoEspecificoCodigo, trata?.Id, request.UsuarioGenerador, request.Nombre, request.Activa)
            ?? throw new KeyNotFoundException("No existe la alerta que se intenta modificar.");

        await this.ConfirmarPerfilAsync(perfil, cancellationToken);
        return await this.MapearAlertaAsync(alerta, trata, cancellationToken);
    }

    public async Task EliminarAlertaAsync(string usuarioInstitucional, Guid alertaId, CancellationToken cancellationToken)
    {
        PerfilUsuario? perfil = await this.CargarPerfilConAlertasAsync(usuarioInstitucional, cancellationToken);
        if (perfil is null || !perfil.EliminarAlertaDocumental(alertaId))
        {
            return;
        }

        await this.ConfirmarPerfilAsync(perfil, cancellationToken);
    }

    public async Task<NovedadesAlertasDocumentalesDto> ListarNovedadesAsync(string usuarioInstitucional, CancellationToken cancellationToken)
    {
        string usuario = AlertaDocumentalService.NormalizarUsuario(usuarioInstitucional);
        (DateTimeOffset inicioDeHoy, DateTimeOffset limiteSinRevisar) = this.CalcularVentanaNovedades();
        IEnumerable<CoincidenciaAlertaDocumental> coincidencias = await this.ConsultarCoincidenciasConDatos()
            .Where(x => x.AlertaDocumental.PerfilUsuario.UsuarioInstitucional == usuario &&
                (x.FechaDeteccion >= inicioDeHoy || (x.FechaVista == null && x.FechaDeteccion >= limiteSinRevisar)))
            .SelectAsync(cancellationToken);
        int sinRevisarEnHistorico = await _coincidenciaRepository.Query()
            .Where(x => x.AlertaDocumental.PerfilUsuario.UsuarioInstitucional == usuario && x.FechaVista == null && x.FechaDeteccion < limiteSinRevisar)
            .CountAsync(cancellationToken);
        IReadOnlyCollection<CoincidenciaAlertaDocumentalDto> novedades = await this.MapearCoincidenciasAsync(coincidencias.OrderByDescending(x => x.FechaDeteccion).ToArray(), cancellationToken);
        return new NovedadesAlertasDocumentalesDto(_diasNovedadesSinRevisar, novedades, sinRevisarEnHistorico);
    }

    // Historico agrupado por alerta: una fila por alerta con sus cantidades; el detalle de cada una se pide con ConsultarHistoricoAsync
    // y AlertaId al desplegar la fila (mismo esquema que los expedientes caratulados por trata en estadisticas).
    public async Task<ResumenHistoricoAlertasDto> ResumirHistoricoAsync(string usuarioInstitucional, CancellationToken cancellationToken)
    {
        string usuario = AlertaDocumentalService.NormalizarUsuario(usuarioInstitucional);
        List<ResumenPorAlerta> cantidades = await this.ConsultarHistoricoDelUsuario(usuario)
            .GroupBy(x => x.AlertaDocumentalId)
            .Select(grupo => new ResumenPorAlerta(grupo.Key, grupo.Count(), grupo.Count(x => x.FechaVista == null), grupo.Max(x => x.FechaDeteccion)))
            .ToListAsync(cancellationToken);
        if (cantidades.Count == 0)
        {
            return new ResumenHistoricoAlertasDto(0, Array.Empty<ResumenHistoricoAlertaDto>());
        }

        Dictionary<Guid, AlertaDescrita> alertasPorId = (await this.DescribirAlertasDelUsuarioAsync(usuario, cancellationToken)).ToDictionary(x => x.Alerta.Id);
        ResumenHistoricoAlertaDto[] alertas = cantidades
            .Where(x => alertasPorId.ContainsKey(x.AlertaId))
            .Select(x => new ResumenHistoricoAlertaDto(x.AlertaId, alertasPorId[x.AlertaId].Descripcion, alertasPorId[x.AlertaId].Alerta.TipoDocumentoCodigo, x.Total, x.SinRevisar, x.UltimaDeteccion))
            .OrderByDescending(x => x.UltimaDeteccion)
            .ToArray();
        return new ResumenHistoricoAlertasDto(alertas.Sum(x => x.SinRevisar), alertas);
    }

    public async Task<HistoricoAlertasDocumentalesDto> ConsultarHistoricoAsync(string usuarioInstitucional, ConsultaHistoricoAlertasRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        string usuario = AlertaDocumentalService.NormalizarUsuario(usuarioInstitucional);
        IQueryable<CoincidenciaAlertaDocumental> historico = this.ConsultarHistoricoDelUsuario(usuario);
        int sinRevisar = await historico.Where(x => x.FechaVista == null).CountAsync(cancellationToken);

        IQueryable<CoincidenciaAlertaDocumental> filtrado = await this.AplicarFiltrosHistoricoAsync(historico, usuario, request, cancellationToken);
        int totalRegistros = await filtrado.CountAsync(cancellationToken);
        int tamanioPagina = Math.Clamp(request.TamanioPagina, 1, AlertaDocumentalService.TamanioPaginaMaximoHistorico);
        int pagina = Math.Max(request.Pagina, 1);
        // Paginacion por claves: se ordena y se toma la pagina proyectando solo el Id, y recien despues se cargan las coincidencias
        // completas de esa pagina con sus datos (mismo criterio que la consulta de expedientes por tratas).
        Guid[] idsPagina = await AlertaDocumentalService.OrdenarHistorico(filtrado, request.CampoOrden, request.DireccionOrden)
            .Skip((pagina - 1) * tamanioPagina)
            .Take(tamanioPagina)
            .Select(x => x.Id)
            .ToArrayAsync(cancellationToken);
        if (idsPagina.Length == 0)
        {
            return new HistoricoAlertasDocumentalesDto(totalRegistros, sinRevisar, pagina, tamanioPagina, Array.Empty<CoincidenciaAlertaDocumentalDto>());
        }

        Dictionary<Guid, CoincidenciaAlertaDocumental> coincidenciasPorId = (await this.ConsultarCoincidenciasConDatos()
            .Where(x => idsPagina.Contains(x.Id))
            .SelectAsync(cancellationToken))
            .ToDictionary(x => x.Id);
        CoincidenciaAlertaDocumental[] coincidenciasPagina = idsPagina.Select(id => coincidenciasPorId[id]).ToArray();
        return new HistoricoAlertasDocumentalesDto(totalRegistros, sinRevisar, pagina, tamanioPagina, await this.MapearCoincidenciasAsync(coincidenciasPagina, cancellationToken));
    }

    public async Task<PendientesAlertasDocumentalesDto> ConsultarPendientesAsync(string usuarioInstitucional, DateTimeOffset? detectadasDespuesDe, CancellationToken cancellationToken)
    {
        string usuario = AlertaDocumentalService.NormalizarUsuario(usuarioInstitucional);
        DateTimeOffset consultado = DateTimeOffset.Now;
        // El badge del menu cuenta solo lo no revisado que esta en la pestana "Alertas"; lo que paso al historico sin revisar tiene
        // su propio contador alla, para que el badge no quede encendido para siempre.
        DateTimeOffset limiteSinRevisar = this.CalcularVentanaNovedades().LimiteSinRevisar;
        int total = await _coincidenciaRepository.Query()
            .Where(x => x.AlertaDocumental.PerfilUsuario.UsuarioInstitucional == usuario && x.FechaVista == null && x.FechaDeteccion >= limiteSinRevisar)
            .CountAsync(cancellationToken);

        IReadOnlyCollection<CoincidenciaAlertaDocumentalDto> nuevas = Array.Empty<CoincidenciaAlertaDocumentalDto>();
        if (total > 0 && detectadasDespuesDe is not null)
        {
            // Las detectadas despues de la lectura anterior alimentan el aviso en pantalla; el total alimenta el badge.
            DateTimeOffset desde = detectadasDespuesDe.Value > limiteSinRevisar ? detectadasDespuesDe.Value : limiteSinRevisar;
            IEnumerable<CoincidenciaAlertaDocumental> coincidencias = await this.ConsultarCoincidenciasConDatos()
                .Where(x => x.AlertaDocumental.PerfilUsuario.UsuarioInstitucional == usuario && x.FechaVista == null && x.FechaDeteccion > desde)
                .SelectAsync(cancellationToken);
            nuevas = await this.MapearCoincidenciasAsync(
                coincidencias.OrderByDescending(x => x.FechaDeteccion).Take(CantidadMaximaNuevasInformadas).ToArray(), cancellationToken);
        }

        return new PendientesAlertasDocumentalesDto(total, consultado, nuevas);
    }

    // "Marcar todas como vistas" de la pestana "Alertas": solo lo no revisado que esta ahi; lo del historico se marca desde el historico.
    public async Task MarcarVistasAsync(string usuarioInstitucional, CancellationToken cancellationToken)
    {
        string usuario = AlertaDocumentalService.NormalizarUsuario(usuarioInstitucional);
        DateTimeOffset limiteSinRevisar = this.CalcularVentanaNovedades().LimiteSinRevisar;
        IEnumerable<AlertaDocumental> alertas = await _alertaRepository.Query()
            .Include(nameof(AlertaDocumental.Coincidencias))
            .Where(x => x.PerfilUsuario.UsuarioInstitucional == usuario)
            .SelectAsync(cancellationToken);
        await this.ConfirmarCoincidenciasVistasAsync(alertas, alerta => alerta.MarcarCoincidenciasVistas(DateTimeOffset.Now, limiteSinRevisar, null), cancellationToken);
    }

    public async Task<bool> MarcarVistaAsync(string usuarioInstitucional, Guid coincidenciaId, CancellationToken cancellationToken)
    {
        string usuario = AlertaDocumentalService.NormalizarUsuario(usuarioInstitucional);
        AlertaDocumental[] alertas = (await _alertaRepository.Query()
            .Include(nameof(AlertaDocumental.Coincidencias))
            .Where(x => x.PerfilUsuario.UsuarioInstitucional == usuario && x.Coincidencias.Any(c => c.Id == coincidenciaId))
            .SelectAsync(cancellationToken))
            .ToArray();
        if (alertas.Length == 0)
        {
            return false;
        }

        Guid[] coincidenciaIds = new[] { coincidenciaId };
        await this.ConfirmarCoincidenciasVistasAsync(alertas, alerta => alerta.MarcarCoincidenciasVistas(coincidenciaIds, DateTimeOffset.Now), cancellationToken);
        return true;
    }

    // "Marcar como revisadas" del historico, como en el correo: las filas elegidas; sin filas, todo lo no revisado del historico.
    public async Task<int> MarcarHistoricoRevisadoAsync(string usuarioInstitucional, IReadOnlyCollection<Guid>? coincidenciaIds, CancellationToken cancellationToken)
    {
        string usuario = AlertaDocumentalService.NormalizarUsuario(usuarioInstitucional);
        Guid[] elegidas = coincidenciaIds?.Where(x => x != Guid.Empty).Distinct().ToArray() ?? Array.Empty<Guid>();
        if (elegidas.Length > 0)
        {
            IEnumerable<AlertaDocumental> alertasDeLasElegidas = await _alertaRepository.Query()
                .Include(nameof(AlertaDocumental.Coincidencias))
                .Where(x => x.PerfilUsuario.UsuarioInstitucional == usuario && x.Coincidencias.Any(c => elegidas.Contains(c.Id)))
                .SelectAsync(cancellationToken);
            return await this.ConfirmarCoincidenciasVistasAsync(alertasDeLasElegidas, alerta => alerta.MarcarCoincidenciasVistas(elegidas, DateTimeOffset.Now), cancellationToken);
        }

        DateTimeOffset limiteSinRevisar = this.CalcularVentanaNovedades().LimiteSinRevisar;
        IEnumerable<AlertaDocumental> alertas = await _alertaRepository.Query()
            .Include(nameof(AlertaDocumental.Coincidencias))
            .Where(x => x.PerfilUsuario.UsuarioInstitucional == usuario)
            .SelectAsync(cancellationToken);
        return await this.ConfirmarCoincidenciasVistasAsync(alertas, alerta => alerta.MarcarCoincidenciasVistas(DateTimeOffset.Now, null, limiteSinRevisar), cancellationToken);
    }

    // El historico es el complemento exacto de las novedades: lo detectado antes de hoy que ya se reviso, o que quedo sin revisar mas
    // alla de la ventana. Queryable() (EF crudo) y no Query() (wrapper URF): el ThenBy del wrapper no llega al SQL, el paginado
    // necesita un orden total y el resumen por alerta agrupa en el servidor.
    private IQueryable<CoincidenciaAlertaDocumental> ConsultarHistoricoDelUsuario(string usuario)
    {
        (DateTimeOffset inicioDeHoy, DateTimeOffset limiteSinRevisar) = this.CalcularVentanaNovedades();
        return _coincidenciaRepository.Queryable()
            .Where(x => x.AlertaDocumental.PerfilUsuario.UsuarioInstitucional == usuario)
            .Where(x => x.FechaDeteccion < inicioDeHoy && (x.FechaVista != null || x.FechaDeteccion < limiteSinRevisar));
    }

    // Las alertas del usuario con su descripcion de pantalla (nombre propio, o tipo, trata y generador), que no es una columna.
    private async Task<AlertaDescrita[]> DescribirAlertasDelUsuarioAsync(string usuario, CancellationToken cancellationToken)
    {
        AlertaDocumental[] alertasUsuario = (await _alertaRepository.Query()
            .Include(nameof(AlertaDocumental.Trata))
            .Where(x => x.PerfilUsuario.UsuarioInstitucional == usuario)
            .SelectAsync(cancellationToken))
            .ToArray();
        IReadOnlyDictionary<string, string> nombresEspecificos = await this.CargarNombresEspecificosAsync(alertasUsuario.Select(x => x.TipoEspecificoCodigo), cancellationToken);
        return alertasUsuario
            .Select(alerta => new AlertaDescrita(alerta, AlertaDocumentalService.DescribirAlerta(alerta, AlertaDocumentalService.BuscarNombre(nombresEspecificos, alerta.TipoEspecificoCodigo), alerta.Trata?.CodigoTrata)))
            .ToArray();
    }

    private sealed record AlertaDescrita(AlertaDocumental Alerta, string Descripcion);

    private sealed record ResumenPorAlerta(Guid AlertaId, int Total, int SinRevisar, DateTimeOffset UltimaDeteccion);

    // Le pide a cada alerta que marque sus coincidencias y confirma una sola vez. Devuelve cuantas coincidencias quedaron marcadas.
    private async Task<int> ConfirmarCoincidenciasVistasAsync(IEnumerable<AlertaDocumental> alertas, Func<AlertaDocumental, int> marcar, CancellationToken cancellationToken)
    {
        int marcadas = 0;
        foreach (AlertaDocumental alerta in alertas)
        {
            int marcadasEnAlerta = marcar(alerta);
            if (marcadasEnAlerta == 0)
            {
                continue;
            }

            _alertaRepository.Update(alerta);
            _alertaRepository.ApplyChanges(alerta);
            marcadas += marcadasEnAlerta;
        }

        if (marcadas > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return marcadas;
    }

    // Ventana de novedades de la pestana "Alertas": todo lo detectado desde el inicio de hoy (visto o no) y lo no revisado detectado
    // desde LimiteSinRevisar (hoy menos los dias configurados). El corte es por dia calendario del servidor, asi un dia entra o sale entero.
    private (DateTimeOffset InicioDeHoy, DateTimeOffset LimiteSinRevisar) CalcularVentanaNovedades()
    {
        DateTimeOffset ahora = DateTimeOffset.Now;
        DateTimeOffset inicioDeHoy = new(ahora.Date, ahora.Offset);
        return (inicioDeHoy, inicioDeHoy.AddDays(-_diasNovedadesSinRevisar));
    }

    private async Task<IQueryable<CoincidenciaAlertaDocumental>> AplicarFiltrosHistoricoAsync(IQueryable<CoincidenciaAlertaDocumental> query, string usuario, ConsultaHistoricoAlertasRequest request, CancellationToken cancellationToken)
    {
        if (request.AlertaId is Guid alertaId) query = query.Where(x => x.AlertaDocumentalId == alertaId);

        string[] alertas = AlertaDocumentalService.LimpiarValoresFiltro(request.Alertas);
        if (alertas.Length > 0)
        {
            // La descripcion de la alerta se arma al mapear (nombre propio, o tipo, trata y generador): no es una columna. El texto
            // tipeado se resuelve contra las alertas del usuario, que son pocas, y al SQL viaja solo la lista de alertas que coinciden.
            Guid[] alertaIds = await this.BuscarAlertasPorDescripcionAsync(usuario, alertas, cancellationToken);
            query = query.Where(x => alertaIds.Contains(x.AlertaDocumentalId));
        }

        string[] numerosDocumento = AlertaDocumentalService.LimpiarValoresFiltro(request.NumerosDocumento);
        if (numerosDocumento.Length > 0) query = query.Where(ExpresionesFiltroGrilla.ContieneAlguno<CoincidenciaAlertaDocumental>(numerosDocumento, x => x.ExpedienteDocumento.Documento.NumeroActuacionCompleto));

        string[] referencias = AlertaDocumentalService.LimpiarValoresFiltro(request.Referencias);
        if (referencias.Length > 0) query = query.Where(ExpresionesFiltroGrilla.ContieneAlguno<CoincidenciaAlertaDocumental>(referencias, x => x.ExpedienteDocumento.Documento.Referencia));

        // Expediente, trata y generador se buscan en todos los vinculos del documento: la fila muestra el expediente de origen y,
        // debajo, los demas donde esta vinculado, y cualquiera de ellos tiene que encontrar la fila.
        string[] numerosExpediente = AlertaDocumentalService.LimpiarValoresFiltro(request.NumerosExpediente);
        if (numerosExpediente.Length > 0)
        {
            query = query.Where(ExpresionesFiltroGrilla.AlgunoCumple<CoincidenciaAlertaDocumental>(numerosExpediente,
                valor => x => x.ExpedienteDocumento.Documento.Expedientes.Any(vinculo => vinculo.Expediente.GdebaNumeroCompleto.Contains(valor))));
        }

        string[] codigosTrata = AlertaDocumentalService.LimpiarValoresFiltro(request.CodigosTrata);
        if (codigosTrata.Length > 0)
        {
            query = query.Where(ExpresionesFiltroGrilla.AlgunoCumple<CoincidenciaAlertaDocumental>(codigosTrata,
                valor => x => x.ExpedienteDocumento.Documento.Expedientes.Any(vinculo => vinculo.Expediente.Trata != null && vinculo.Expediente.Trata.CodigoTrata.Contains(valor))));
        }

        string[] generadores = AlertaDocumentalService.LimpiarValoresFiltro(request.Generadores);
        if (generadores.Length > 0)
        {
            query = query.Where(ExpresionesFiltroGrilla.AlgunoCumple<CoincidenciaAlertaDocumental>(generadores,
                valor => x => x.ExpedienteDocumento.Documento.Expedientes.Any(vinculo => vinculo.UsuarioGenerador != null && vinculo.UsuarioGenerador.Contains(valor))));
        }

        if (request.FiltroFechaDocumento is FiltroFecha filtroFechaDocumento) query = query.Where(ExpresionesFiltroGrilla.CumpleFiltroFecha<CoincidenciaAlertaDocumental>(filtroFechaDocumento, x => x.ExpedienteDocumento.Documento.FechaCreacion));
        if (request.FiltroFechaDeteccion is FiltroFecha filtroFechaDeteccion) query = query.Where(ExpresionesFiltroGrilla.CumpleFiltroFecha<CoincidenciaAlertaDocumental>(filtroFechaDeteccion, x => (DateTimeOffset?)x.FechaDeteccion));
        if (request.SoloSinRevisar is bool soloSinRevisar) query = soloSinRevisar ? query.Where(x => x.FechaVista == null) : query.Where(x => x.FechaVista != null);
        return query;
    }

    private async Task<Guid[]> BuscarAlertasPorDescripcionAsync(string usuario, IReadOnlyCollection<string> textos, CancellationToken cancellationToken)
    {
        return (await this.DescribirAlertasDelUsuarioAsync(usuario, cancellationToken))
            .Where(x => textos.Any(texto => CultureInfo.InvariantCulture.CompareInfo.IndexOf(x.Descripcion, texto, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0))
            .Select(x => x.Alerta.Id)
            .ToArray();
    }

    // El desempate final (deteccion mas reciente primero, luego Id) da un orden total: sin el, dos paginas podrian repetir o saltear filas.
    private static IQueryable<CoincidenciaAlertaDocumental> OrdenarHistorico(IQueryable<CoincidenciaAlertaDocumental> query, string? campoOrden, string? direccionOrden)
    {
        bool descendente = !string.Equals(direccionOrden, "asc", StringComparison.OrdinalIgnoreCase);
        IOrderedQueryable<CoincidenciaAlertaDocumental> ordenada = campoOrden switch
        {
            // La descripcion no es una columna: se ordena por lo que la compone, que deja juntas las coincidencias de una misma alerta.
            "descripcionAlerta" => AlertaDocumentalService.Ordenar(query, x => x.AlertaDocumental.Nombre ?? x.AlertaDocumental.TipoEspecificoCodigo ?? x.AlertaDocumental.TipoDocumentoCodigo, descendente),
            "numeroDocumento" => AlertaDocumentalService.Ordenar(query, x => x.ExpedienteDocumento.Documento.NumeroActuacionCompleto, descendente),
            "referencia" => AlertaDocumentalService.Ordenar(query, x => x.ExpedienteDocumento.Documento.Referencia, descendente),
            "fechaDocumento" => AlertaDocumentalService.Ordenar(query, x => x.ExpedienteDocumento.Documento.FechaCreacion, descendente),
            // Descendente = las no revisadas primero.
            "pendiente" => AlertaDocumentalService.Ordenar(query, x => x.FechaVista == null, descendente),
            "fechaDeteccion" => AlertaDocumentalService.Ordenar(query, x => x.FechaDeteccion, descendente),
            _ => AlertaDocumentalService.Ordenar(query, x => x.FechaDeteccion, descendente: true)
        };
        return ordenada.ThenByDescending(x => x.FechaDeteccion).ThenBy(x => x.Id);
    }

    private static IOrderedQueryable<CoincidenciaAlertaDocumental> Ordenar<TClave>(IQueryable<CoincidenciaAlertaDocumental> query, Expression<Func<CoincidenciaAlertaDocumental, TClave>> clave, bool descendente)
    {
        return descendente ? query.OrderByDescending(clave) : query.OrderBy(clave);
    }

    private static string[] LimpiarValoresFiltro(IReadOnlyCollection<string>? valores)
    {
        return valores?.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray() ?? Array.Empty<string>();
    }

    public async Task<IReadOnlyCollection<TipoDocumentoAlertaDto>> ListarTiposDocumentoAsync(CancellationToken cancellationToken)
    {
        // Dos fuentes. Lo observado en los documentos conocidos, por sigla y tipo especifico: el detalle del expediente deja como tipo la
        // misma sigla del numero (ACTA); el historial la reemplaza por el especifico (ACMED); un documento cuyo tipo sigue siendo la sigla
        // suma a la sigla entera pero no define un tipo especifico. Y el catalogo con sigla confirmada por GDEBA, para poder elegir un tipo
        // que todavia no circulo (cuelga de su sigla con 0 documentos). Un tipo del catalogo sin sigla no se ofrece: la sigla se completa
        // con la operacion administrativa de completado, no adivinando.
        var observados = await _documentoRepository.Queryable()
            .Where(x => x.ActuacionTipoCodigo != "")
            .GroupBy(x => new { x.ActuacionTipoCodigo, x.TipoDocumentoCodigo })
            .Select(grupo => new { Acronimo = grupo.Key.ActuacionTipoCodigo, Especifico = grupo.Key.TipoDocumentoCodigo, Cantidad = grupo.Count() })
            .ToListAsync(cancellationToken);
        List<(string Sigla, string? Especifico, int Cantidad)> normalizados = observados
            .Select(x =>
            {
                string sigla = x.Acronimo.Trim().ToUpperInvariant();
                string? especifico = string.IsNullOrWhiteSpace(x.Especifico) ? null : x.Especifico.Trim().ToUpperInvariant();
                return (sigla, especifico == sigla ? null : especifico, x.Cantidad);
            })
            .ToList();
        List<TipoDocumentoGdeba> catalogo = await _tipoDocumentoRepository.Queryable().ToListAsync(cancellationToken);

        HashSet<string> siglasObservadas = new(normalizados.Select(x => x.Sigla), StringComparer.OrdinalIgnoreCase);
        Dictionary<string, string> nombresCatalogo = new(StringComparer.OrdinalIgnoreCase);
        foreach (TipoDocumentoGdeba tipo in catalogo)
        {
            nombresCatalogo[tipo.Codigo] = tipo.Nombre;
        }

        // Especificos bajo cada sigla: los observados con su cantidad y los del catalogo con sigla confirmada, con 0 si no circularon.
        Dictionary<string, Dictionary<string, int>> especificosPorSigla = new(StringComparer.OrdinalIgnoreCase);
        foreach ((string sigla, string? especifico, int cantidad) in normalizados.Where(x => x.Especifico is not null))
        {
            Dictionary<string, int> especificos = especificosPorSigla.TryGetValue(sigla, out Dictionary<string, int>? existentes)
                ? existentes
                : especificosPorSigla[sigla] = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            especificos[especifico!] = especificos.GetValueOrDefault(especifico!) + cantidad;
        }

        foreach (TipoDocumentoGdeba tipo in catalogo.Where(x => x.Activo && !string.IsNullOrWhiteSpace(x.CodigoTipoDocumentoGdeba)))
        {
            string sigla = tipo.CodigoTipoDocumentoGdeba!.Trim().ToUpperInvariant();
            if (string.Equals(sigla, tipo.Codigo, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            Dictionary<string, int> especificos = especificosPorSigla.TryGetValue(sigla, out Dictionary<string, int>? existentes)
                ? existentes
                : especificosPorSigla[sigla] = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            especificos.TryAdd(tipo.Codigo, 0);
        }

        HashSet<string> siglas = new(siglasObservadas.Concat(especificosPorSigla.Keys), StringComparer.OrdinalIgnoreCase);
        IReadOnlyDictionary<string, string> nombresTipos = await this.CargarNombresTiposAsync(siglas, cancellationToken);
        Dictionary<string, int> cantidadPorSigla = normalizados
            .GroupBy(x => x.Sigla, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(grupo => grupo.Key, grupo => grupo.Sum(x => x.Cantidad), StringComparer.OrdinalIgnoreCase);

        List<TipoDocumentoAlertaDto> opciones = new();
        foreach (string sigla in siglas.OrderByDescending(x => cantidadPorSigla.GetValueOrDefault(x)).ThenBy(x => x, StringComparer.OrdinalIgnoreCase))
        {
            opciones.Add(new TipoDocumentoAlertaDto(sigla, null, nombresTipos.GetValueOrDefault(sigla), cantidadPorSigla.GetValueOrDefault(sigla)));
            if (especificosPorSigla.TryGetValue(sigla, out Dictionary<string, int>? especificos))
            {
                opciones.AddRange(especificos
                    .OrderByDescending(x => x.Value)
                    .ThenBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                    .Select(x => new TipoDocumentoAlertaDto(sigla, x.Key, nombresCatalogo.GetValueOrDefault(x.Key), x.Value)));
            }
        }

        return opciones;
    }

    // El detector compara sigla y tipo especifico, asi que el especifico de la alerta tiene que pertenecer a la sigla elegida. Se valida
    // contra el catalogo local (completado con GDEBA por la operacion administrativa); es una red de seguridad para pedidos que no vienen
    // del selector. Un codigo que el catalogo no tiene (observado en documentos pero nunca consultado) se acepta como hasta ahora.
    private async Task ConfirmarTipoEspecificoAsync(string tipoDocumentoCodigo, string? tipoEspecificoCodigo, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(tipoEspecificoCodigo) || string.IsNullOrWhiteSpace(tipoDocumentoCodigo))
        {
            return;
        }

        string sigla = tipoDocumentoCodigo.Trim().ToUpperInvariant();
        string codigoEspecifico = tipoEspecificoCodigo.Trim().ToUpperInvariant();
        if (string.Equals(sigla, codigoEspecifico, StringComparison.Ordinal))
        {
            return;
        }

        TipoDocumentoGdeba? tipo = await _tipoDocumentoRepository.Queryable()
            .FirstOrDefaultAsync(x => x.Codigo == codigoEspecifico, cancellationToken);
        if (tipo is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(tipo.CodigoTipoDocumentoGdeba))
        {
            throw new InvalidOperationException($"El tipo {codigoEspecifico} ({tipo.Nombre}) no tiene sigla confirmada en el catálogo; hay que completar el catálogo de tipos documentales.");
        }

        if (!string.Equals(tipo.CodigoTipoDocumentoGdeba.Trim(), sigla, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"El tipo {codigoEspecifico} ({tipo.Nombre}) pertenece a la sigla {tipo.CodigoTipoDocumentoGdeba}, no a {sigla}.");
        }
    }

    private IQuery<CoincidenciaAlertaDocumental> ConsultarCoincidenciasConDatos()
    {
        return _coincidenciaRepository.Query()
            .Include(nameof(CoincidenciaAlertaDocumental.AlertaDocumental))
            .Include($"{nameof(CoincidenciaAlertaDocumental.ExpedienteDocumento)}.{nameof(ExpedienteDocumento.Documento)}")
            .Include($"{nameof(CoincidenciaAlertaDocumental.ExpedienteDocumento)}.{nameof(ExpedienteDocumento.Expediente)}.{nameof(Expediente.Trata)}");
    }

    private async Task<IReadOnlyCollection<CoincidenciaAlertaDocumentalDto>> MapearCoincidenciasAsync(IReadOnlyCollection<CoincidenciaAlertaDocumental> coincidencias, CancellationToken cancellationToken)
    {
        if (coincidencias.Count == 0)
        {
            return Array.Empty<CoincidenciaAlertaDocumentalDto>();
        }

        IReadOnlyDictionary<string, string> nombresTipos = await this.CargarNombresTiposAsync(
            coincidencias.Select(x => x.ExpedienteDocumento.Documento.ActuacionTipoCodigo), cancellationToken);
        // Nombres de los tipos especificos: los del criterio de cada alerta y los de los documentos coincidentes.
        IReadOnlyDictionary<string, string> nombresEspecificos = await this.CargarNombresEspecificosAsync(
            coincidencias.Select(x => x.AlertaDocumental.TipoEspecificoCodigo).Concat(coincidencias.Select(x => x.ExpedienteDocumento.Documento.TipoDocumentoCodigo)),
            cancellationToken);

        // La coincidencia es una por documento. El expediente principal que se muestra es el de origen: el de la vinculacion mas antigua
        // entre los que el proxy conoce (un documento se vincula primero donde se produce y despues a los demas). El vinculo donde se
        // detecto la coincidencia solo define su identidad; si el proxy conoce despues el expediente de origen, pasa a ser el principal solo.
        Guid[] documentoIds = coincidencias.Select(x => x.ExpedienteDocumento.DocumentoId).Distinct().ToArray();
        IEnumerable<ExpedienteDocumento> vinculosDelDocumento = await _expedienteDocumentoRepository.Query()
            .Include($"{nameof(ExpedienteDocumento.Expediente)}.{nameof(Expediente.Trata)}")
            .Where(x => documentoIds.Contains(x.DocumentoId))
            .SelectAsync(cancellationToken);
        ILookup<Guid, ExpedienteDocumento> vinculosPorDocumento = vinculosDelDocumento.ToLookup(x => x.DocumentoId);

        return coincidencias.Select(x => AlertaDocumentalService.MapearCoincidencia(x, nombresTipos, nombresEspecificos, vinculosPorDocumento)).ToArray();
    }

    private static CoincidenciaAlertaDocumentalDto MapearCoincidencia(
        CoincidenciaAlertaDocumental coincidencia, IReadOnlyDictionary<string, string> nombresTipos, IReadOnlyDictionary<string, string> nombresEspecificos,
        ILookup<Guid, ExpedienteDocumento> vinculosPorDocumento)
    {
        ExpedienteDocumento vinculoDetectado = coincidencia.ExpedienteDocumento;
        DocumentoGdeba documento = vinculoDetectado.Documento;
        ExpedienteDocumento[] vinculos = vinculosPorDocumento[documento.Id]
            .OrderBy(x => x.FechaVinculacion ?? DateTimeOffset.MaxValue)
            .ThenBy(x => x.Id == vinculoDetectado.Id ? 0 : 1)
            .ToArray();
        ExpedienteDocumento vinculo = vinculos.Length > 0 ? vinculos[0] : vinculoDetectado;
        Expediente expediente = vinculo.Expediente;
        ExpedienteVinculadoDto[] otrosExpedientes = vinculos
            .Where(x => x.ExpedienteId != expediente.Id)
            .Select(x => new ExpedienteVinculadoDto(x.ExpedienteId, x.Expediente.GdebaNumeroCompleto))
            .ToArray();
        // El nombre del documento es el de su tipo especifico cuando el catalogo lo conoce; si no, el nombre unico del acronimo.
        string? nombreTipoDocumento = AlertaDocumentalService.BuscarNombre(nombresEspecificos, documento.TipoDocumentoCodigo)
            ?? nombresTipos.GetValueOrDefault(documento.ActuacionTipoCodigo);
        AlertaDocumental alerta = coincidencia.AlertaDocumental;
        return new CoincidenciaAlertaDocumentalDto(
            coincidencia.Id,
            coincidencia.AlertaDocumentalId,
            AlertaDocumentalService.DescribirAlerta(alerta, AlertaDocumentalService.BuscarNombre(nombresEspecificos, alerta.TipoEspecificoCodigo), expediente.Trata?.CodigoTrata),
            documento.Id,
            documento.NumeroActuacionCompleto,
            documento.PuedeVerDocumento,
            documento.ActuacionTipoCodigo,
            nombreTipoDocumento,
            documento.Referencia,
            documento.FechaCreacion ?? vinculo.FechaVinculacion,
            vinculo.FechaVinculacion,
            vinculo.UsuarioGenerador,
            expediente.Id,
            expediente.GdebaNumeroCompleto,
            expediente.Trata?.CodigoTrata,
            expediente.Trata?.DescripcionTrata,
            expediente.Motivo ?? expediente.DescripcionAdicional,
            coincidencia.FechaDeteccion,
            coincidencia.EstaPendiente,
            otrosExpedientes);
    }

    private async Task<AlertaDocumentalDto> MapearAlertaAsync(AlertaDocumental alerta, TrataHabilitadaVialidad? trata, CancellationToken cancellationToken)
    {
        IReadOnlyDictionary<string, string> nombresTipos = await this.CargarNombresTiposAsync(new[] { alerta.TipoDocumentoCodigo }, cancellationToken);
        IReadOnlyDictionary<string, string> nombresEspecificos = await this.CargarNombresEspecificosAsync(new[] { alerta.TipoEspecificoCodigo }, cancellationToken);
        DateTimeOffset limiteSinRevisar = this.CalcularVentanaNovedades().LimiteSinRevisar;
        int pendientes = await _coincidenciaRepository.Query().Where(x => x.AlertaDocumentalId == alerta.Id && x.FechaVista == null && x.FechaDeteccion >= limiteSinRevisar).CountAsync(cancellationToken);
        return AlertaDocumentalService.MapearAlerta(alerta, nombresTipos, nombresEspecificos, pendientes, trata);
    }

    private static AlertaDocumentalDto MapearAlerta(
        AlertaDocumental alerta, IReadOnlyDictionary<string, string> nombresTipos, IReadOnlyDictionary<string, string> nombresEspecificos,
        int pendientes, TrataHabilitadaVialidad? trata = null)
    {
        TrataHabilitadaVialidad? trataAlerta = trata ?? alerta.Trata;
        string? nombreEspecifico = AlertaDocumentalService.BuscarNombre(nombresEspecificos, alerta.TipoEspecificoCodigo);
        return new AlertaDocumentalDto(
            alerta.Id,
            alerta.Nombre,
            AlertaDocumentalService.DescribirAlerta(alerta, nombreEspecifico, trataAlerta?.CodigoTrata),
            alerta.TipoDocumentoCodigo,
            nombresTipos.GetValueOrDefault(alerta.TipoDocumentoCodigo),
            alerta.TipoEspecificoCodigo,
            nombreEspecifico,
            alerta.TrataId,
            trataAlerta?.CodigoTrata,
            trataAlerta?.DescripcionTrata,
            alerta.UsuarioGenerador,
            alerta.Activa,
            alerta.FechaCreacion,
            pendientes);
    }

    // Descripcion legible: el nombre que puso el usuario o, si no, los criterios ("RS · trata ABC · generador PEREZ"; con tipo especifico,
    // su nombre de catalogo: "Acta de Medicion · trata ABC", o "ACTA CVTDO" cuando el catalogo no lo nombra).
    private static string DescribirAlerta(AlertaDocumental alerta, string? nombreTipoEspecifico, string? codigoTrata)
    {
        if (alerta.Nombre is not null)
        {
            return alerta.Nombre;
        }

        string tipo = alerta.TipoEspecificoCodigo is null
            ? alerta.TipoDocumentoCodigo
            : nombreTipoEspecifico ?? $"{alerta.TipoDocumentoCodigo} {alerta.TipoEspecificoCodigo}";
        List<string> partes = new() { tipo };
        if (alerta.TrataId is not null)
        {
            partes.Add($"trata {codigoTrata ?? "?"}");
        }

        if (alerta.UsuarioGenerador is not null)
        {
            partes.Add($"generador {alerta.UsuarioGenerador}");
        }

        return string.Join(" · ", partes);
    }

    private static string? BuscarNombre(IReadOnlyDictionary<string, string> nombres, string? codigo)
    {
        return codigo is null ? null : nombres.GetValueOrDefault(codigo);
    }

    // Nombre del catalogo de cada tipo especifico (ACMED: "Acta de Medicion"), por su codigo, que es unico. El catalogo esta incompleto:
    // solo tiene los tipos que el enriquecimiento documental cargo, asi que un codigo observado puede no tener nombre.
    private async Task<IReadOnlyDictionary<string, string>> CargarNombresEspecificosAsync(IEnumerable<string?> codigos, CancellationToken cancellationToken)
    {
        string[] codigosDistintos = codigos
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        Dictionary<string, string> nombres = new(StringComparer.OrdinalIgnoreCase);
        if (codigosDistintos.Length == 0)
        {
            return nombres;
        }

        List<TipoDocumentoGdeba> tipos = await _tipoDocumentoRepository.Queryable()
            .Where(x => codigosDistintos.Contains(x.Codigo))
            .ToListAsync(cancellationToken);
        foreach (TipoDocumentoGdeba tipo in tipos)
        {
            string nombre = tipo.Nombre.Trim();
            if (nombre.Length > 0)
            {
                nombres[tipo.Codigo.Trim()] = nombre;
            }
        }

        return nombres;
    }

    // Nombre unico de cada acronimo: solo cuando el catalogo le da un unico nombre distinto (varias filas con el mismo nombre cuentan como uno).
    private async Task<IReadOnlyDictionary<string, string>> CargarNombresTiposAsync(IEnumerable<string?> codigos, CancellationToken cancellationToken)
    {
        IReadOnlyDictionary<string, string[]> nombresPorAcronimo = await this.CargarNombresPorAcronimoAsync(codigos, cancellationToken);
        Dictionary<string, string> nombres = new(StringComparer.OrdinalIgnoreCase);
        foreach ((string acronimo, string[] nombresDelAcronimo) in nombresPorAcronimo)
        {
            if (nombresDelAcronimo.Length == 1)
            {
                nombres[acronimo] = nombresDelAcronimo[0];
            }
        }

        return nombres;
    }

    // Todos los nombres distintos que el catalogo da a cada acronimo. El acronimo del numero de actuacion (RS, ACTA, PV...) puede ser el
    // codigo del catalogo o su "codigo tipo documento GDEBA", y varias filas pueden compartirlo (ACTA: acta de apertura, de medicion,
    // dictamen...; RS: varias filas "Resolucion"). Las alertas trabajan por acronimo; el nombre es orientacion y texto de busqueda.
    private async Task<IReadOnlyDictionary<string, string[]>> CargarNombresPorAcronimoAsync(IEnumerable<string?> codigos, CancellationToken cancellationToken)
    {
        string[] codigosDistintos = codigos
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        Dictionary<string, string[]> nombresPorAcronimo = new(StringComparer.OrdinalIgnoreCase);
        if (codigosDistintos.Length == 0)
        {
            return nombresPorAcronimo;
        }

        List<TipoDocumentoGdeba> tipos = await _tipoDocumentoRepository.Queryable()
            .Where(x => codigosDistintos.Contains(x.Codigo) || (x.CodigoTipoDocumentoGdeba != null && codigosDistintos.Contains(x.CodigoTipoDocumentoGdeba)))
            .ToListAsync(cancellationToken);
        foreach (string acronimo in codigosDistintos)
        {
            string[] nombres = tipos
                .Where(x => string.Equals(x.Codigo, acronimo, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(x.CodigoTipoDocumentoGdeba, acronimo, StringComparison.OrdinalIgnoreCase))
                .Select(x => x.Nombre.Trim())
                .Where(x => x.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (nombres.Length > 0)
            {
                nombresPorAcronimo[acronimo] = nombres;
            }
        }

        return nombresPorAcronimo;
    }

    private async Task<TrataHabilitadaVialidad?> ValidarTrataAsync(Guid? trataId, CancellationToken cancellationToken)
    {
        if (trataId is null || trataId == Guid.Empty)
        {
            return null;
        }

        return await _trataRepository.Query().FirstOrDefaultAsync(x => x.Id == trataId, cancellationToken)
            ?? throw new KeyNotFoundException("No existe la trata indicada para la alerta.");
    }

    private async Task<PerfilUsuario> CargarOCrearPerfilConAlertasAsync(string usuarioInstitucional, CancellationToken cancellationToken)
    {
        string usuario = AlertaDocumentalService.NormalizarUsuario(usuarioInstitucional);
        PerfilUsuario? perfil = await this.CargarPerfilConAlertasAsync(usuario, cancellationToken);
        if (perfil is not null)
        {
            return perfil;
        }

        PerfilUsuario nuevo = new(usuario) { TrackingState = TrackingState.Added };
        _perfilRepository.Insert(nuevo);
        return nuevo;
    }

    private async Task<PerfilUsuario?> CargarPerfilConAlertasAsync(string usuarioInstitucional, CancellationToken cancellationToken)
    {
        string usuario = AlertaDocumentalService.NormalizarUsuario(usuarioInstitucional);
        return await _perfilRepository.Query()
            .Include(nameof(PerfilUsuario.AlertasDocumentales))
            .FirstOrDefaultAsync(x => x.UsuarioInstitucional == usuario, cancellationToken);
    }

    private async Task ConfirmarPerfilAsync(PerfilUsuario perfil, CancellationToken cancellationToken)
    {
        if (perfil.TrackingState != TrackingState.Added)
        {
            _perfilRepository.Update(perfil);
        }

        _perfilRepository.ApplyChanges(perfil);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _perfilRepository.AcceptChanges(perfil);
    }

    private static string NormalizarUsuario(string usuarioInstitucional)
    {
        return string.IsNullOrWhiteSpace(usuarioInstitucional)
            ? throw new ArgumentException("El usuario institucional es requerido.", nameof(usuarioInstitucional))
            : usuarioInstitucional.Trim();
    }
}
