using Microsoft.EntityFrameworkCore;
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

    private readonly ITrackableRepository<PerfilUsuario> _perfilRepository;
    private readonly ITrackableRepository<AlertaDocumental> _alertaRepository;
    private readonly IRepository<CoincidenciaAlertaDocumental> _coincidenciaRepository;
    private readonly IRepository<DocumentoGdeba> _documentoRepository;
    private readonly IRepository<TipoDocumentoGdeba> _tipoDocumentoRepository;
    private readonly IRepository<TrataHabilitadaVialidad> _trataRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AlertaDocumentalService(
        ITrackableRepository<PerfilUsuario> perfilRepository,
        ITrackableRepository<AlertaDocumental> alertaRepository,
        IRepository<CoincidenciaAlertaDocumental> coincidenciaRepository,
        IRepository<DocumentoGdeba> documentoRepository,
        IRepository<TipoDocumentoGdeba> tipoDocumentoRepository,
        IRepository<TrataHabilitadaVialidad> trataRepository,
        IUnitOfWork unitOfWork)
    {
        _perfilRepository = perfilRepository;
        _alertaRepository = alertaRepository;
        _coincidenciaRepository = coincidenciaRepository;
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

        Guid[] alertaIds = alertasUsuario.Select(x => x.Id).ToArray();
        Dictionary<Guid, int> pendientesPorAlerta = await _coincidenciaRepository.Queryable()
            .Where(x => alertaIds.Contains(x.AlertaDocumentalId) && x.FechaVista == null)
            .GroupBy(x => x.AlertaDocumentalId)
            .Select(grupo => new { AlertaId = grupo.Key, Cantidad = grupo.Count() })
            .ToDictionaryAsync(x => x.AlertaId, x => x.Cantidad, cancellationToken);
        IReadOnlyDictionary<string, string> nombresTipos = await this.CargarNombresTiposAsync(alertasUsuario.Select(x => x.TipoDocumentoCodigo), cancellationToken);

        return alertasUsuario
            .OrderBy(x => x.FechaCreacion)
            .Select(x => AlertaDocumentalService.MapearAlerta(x, nombresTipos, pendientesPorAlerta.GetValueOrDefault(x.Id)))
            .ToArray();
    }

    public async Task<AlertaDocumentalDto> CrearAlertaAsync(string usuarioInstitucional, GuardarAlertaDocumentalRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        TrataHabilitadaVialidad? trata = await this.ValidarTrataAsync(request.TrataId, cancellationToken);
        PerfilUsuario perfil = await this.CargarOCrearPerfilConAlertasAsync(usuarioInstitucional, cancellationToken);
        AlertaDocumental alerta = perfil.CrearAlertaDocumental(request.TipoDocumentoCodigo, trata?.Id, request.UsuarioGenerador, request.Nombre, DateTimeOffset.Now);
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
        PerfilUsuario perfil = await this.CargarPerfilConAlertasAsync(usuarioInstitucional, cancellationToken)
            ?? throw new KeyNotFoundException("No existe la alerta que se intenta modificar.");
        AlertaDocumental alerta = perfil.ActualizarAlertaDocumental(alertaId, request.TipoDocumentoCodigo, trata?.Id, request.UsuarioGenerador, request.Nombre, request.Activa)
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

    public async Task<IReadOnlyCollection<CoincidenciaAlertaDocumentalDto>> ListarCoincidenciasAsync(string usuarioInstitucional, CancellationToken cancellationToken)
    {
        string usuario = AlertaDocumentalService.NormalizarUsuario(usuarioInstitucional);
        IEnumerable<CoincidenciaAlertaDocumental> coincidencias = await this.ConsultarCoincidenciasConDatos()
            .Where(x => x.AlertaDocumental.PerfilUsuario.UsuarioInstitucional == usuario)
            .SelectAsync(cancellationToken);
        return await this.MapearCoincidenciasAsync(coincidencias.OrderByDescending(x => x.FechaDeteccion).ToArray(), cancellationToken);
    }

    public async Task<PendientesAlertasDocumentalesDto> ConsultarPendientesAsync(string usuarioInstitucional, DateTimeOffset? detectadasDespuesDe, CancellationToken cancellationToken)
    {
        string usuario = AlertaDocumentalService.NormalizarUsuario(usuarioInstitucional);
        DateTimeOffset consultado = DateTimeOffset.Now;
        int total = await _coincidenciaRepository.Query()
            .Where(x => x.AlertaDocumental.PerfilUsuario.UsuarioInstitucional == usuario && x.FechaVista == null)
            .CountAsync(cancellationToken);

        IReadOnlyCollection<CoincidenciaAlertaDocumentalDto> nuevas = Array.Empty<CoincidenciaAlertaDocumentalDto>();
        if (total > 0 && detectadasDespuesDe is not null)
        {
            // Las detectadas despues de la lectura anterior alimentan el aviso en pantalla; el total alimenta el badge.
            DateTimeOffset desde = detectadasDespuesDe.Value;
            IEnumerable<CoincidenciaAlertaDocumental> coincidencias = await this.ConsultarCoincidenciasConDatos()
                .Where(x => x.AlertaDocumental.PerfilUsuario.UsuarioInstitucional == usuario && x.FechaVista == null && x.FechaDeteccion > desde)
                .SelectAsync(cancellationToken);
            nuevas = await this.MapearCoincidenciasAsync(
                coincidencias.OrderByDescending(x => x.FechaDeteccion).Take(CantidadMaximaNuevasInformadas).ToArray(), cancellationToken);
        }

        return new PendientesAlertasDocumentalesDto(total, consultado, nuevas);
    }

    public async Task MarcarVistasAsync(string usuarioInstitucional, CancellationToken cancellationToken)
    {
        string usuario = AlertaDocumentalService.NormalizarUsuario(usuarioInstitucional);
        IEnumerable<AlertaDocumental> alertas = await _alertaRepository.Query()
            .Include(nameof(AlertaDocumental.Coincidencias))
            .Where(x => x.PerfilUsuario.UsuarioInstitucional == usuario)
            .SelectAsync(cancellationToken);

        DateTimeOffset fecha = DateTimeOffset.Now;
        bool hayCambios = false;
        foreach (AlertaDocumental alerta in alertas)
        {
            bool teniaPendientes = alerta.Coincidencias.Any(x => x.EstaPendiente);
            IReadOnlyCollection<CoincidenciaAlertaDocumental> podadas = alerta.MarcarCoincidenciasVistas(fecha);
            if (!teniaPendientes && podadas.Count == 0)
            {
                continue;
            }

            _alertaRepository.Update(alerta);
            _alertaRepository.ApplyChanges(alerta);
            hayCambios = true;
        }

        if (hayCambios)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<IReadOnlyCollection<TipoDocumentoAlertaDto>> ListarTiposDocumentoAsync(CancellationToken cancellationToken)
    {
        var tiposObservados = await _documentoRepository.Queryable()
            .GroupBy(x => x.ActuacionTipoCodigo)
            .Select(grupo => new { Codigo = grupo.Key, Cantidad = grupo.Count() })
            .ToListAsync(cancellationToken);
        IReadOnlyDictionary<string, string> nombres = await this.CargarNombresTiposAsync(tiposObservados.Select(x => x.Codigo), cancellationToken);

        return tiposObservados
            .Where(x => !string.IsNullOrWhiteSpace(x.Codigo))
            .Select(x => new TipoDocumentoAlertaDto(x.Codigo.ToUpperInvariant(), nombres.GetValueOrDefault(x.Codigo), x.Cantidad))
            .OrderByDescending(x => x.CantidadDocumentos)
            .ThenBy(x => x.Codigo)
            .ToArray();
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
        return coincidencias.Select(x => AlertaDocumentalService.MapearCoincidencia(x, nombresTipos)).ToArray();
    }

    private static CoincidenciaAlertaDocumentalDto MapearCoincidencia(CoincidenciaAlertaDocumental coincidencia, IReadOnlyDictionary<string, string> nombresTipos)
    {
        ExpedienteDocumento vinculo = coincidencia.ExpedienteDocumento;
        DocumentoGdeba documento = vinculo.Documento;
        Expediente expediente = vinculo.Expediente;
        return new CoincidenciaAlertaDocumentalDto(
            coincidencia.Id,
            coincidencia.AlertaDocumentalId,
            AlertaDocumentalService.DescribirAlerta(coincidencia.AlertaDocumental, expediente.Trata?.CodigoTrata),
            documento.Id,
            documento.NumeroActuacionCompleto,
            documento.PuedeVerDocumento,
            documento.ActuacionTipoCodigo,
            nombresTipos.GetValueOrDefault(documento.ActuacionTipoCodigo),
            documento.Referencia,
            documento.FechaCreacion ?? vinculo.FechaVinculacion,
            vinculo.UsuarioGenerador,
            expediente.Id,
            expediente.GdebaNumeroCompleto,
            expediente.Trata?.CodigoTrata,
            expediente.Trata?.DescripcionTrata,
            expediente.Motivo ?? expediente.DescripcionAdicional,
            coincidencia.FechaDeteccion,
            coincidencia.EstaPendiente);
    }

    private async Task<AlertaDocumentalDto> MapearAlertaAsync(AlertaDocumental alerta, TrataHabilitadaVialidad? trata, CancellationToken cancellationToken)
    {
        IReadOnlyDictionary<string, string> nombresTipos = await this.CargarNombresTiposAsync(new[] { alerta.TipoDocumentoCodigo }, cancellationToken);
        int pendientes = await _coincidenciaRepository.Query().Where(x => x.AlertaDocumentalId == alerta.Id && x.FechaVista == null).CountAsync(cancellationToken);
        return AlertaDocumentalService.MapearAlerta(alerta, nombresTipos, pendientes, trata);
    }

    private static AlertaDocumentalDto MapearAlerta(AlertaDocumental alerta, IReadOnlyDictionary<string, string> nombresTipos, int pendientes, TrataHabilitadaVialidad? trata = null)
    {
        TrataHabilitadaVialidad? trataAlerta = trata ?? alerta.Trata;
        return new AlertaDocumentalDto(
            alerta.Id,
            alerta.Nombre,
            AlertaDocumentalService.DescribirAlerta(alerta, trataAlerta?.CodigoTrata),
            alerta.TipoDocumentoCodigo,
            nombresTipos.GetValueOrDefault(alerta.TipoDocumentoCodigo),
            alerta.TrataId,
            trataAlerta?.CodigoTrata,
            trataAlerta?.DescripcionTrata,
            alerta.UsuarioGenerador,
            alerta.Activa,
            alerta.FechaCreacion,
            pendientes);
    }

    // Descripcion legible: el nombre que puso el usuario o, si no, los criterios ("RESOL · trata ABC · generador PEREZ").
    private static string DescribirAlerta(AlertaDocumental alerta, string? codigoTrata)
    {
        if (alerta.Nombre is not null)
        {
            return alerta.Nombre;
        }

        List<string> partes = new() { alerta.TipoDocumentoCodigo };
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

    private async Task<IReadOnlyDictionary<string, string>> CargarNombresTiposAsync(IEnumerable<string?> codigos, CancellationToken cancellationToken)
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
            .Where(x => codigosDistintos.Contains(x.Codigo) || (x.CodigoTipoDocumentoGdeba != null && codigosDistintos.Contains(x.CodigoTipoDocumentoGdeba)))
            .ToListAsync(cancellationToken);

        // Varios tipos del catalogo comparten acronimo (PV es providencia, notificacion, certificado de obra...). Manda el tipo cuyo
        // codigo de catalogo es el acronimo mismo; si no existe, el nombre se usa solo cuando el acronimo tiene un unico tipo.
        foreach (TipoDocumentoGdeba tipo in tipos.Where(x => codigosDistintos.Contains(x.Codigo, StringComparer.OrdinalIgnoreCase)))
        {
            nombres.TryAdd(tipo.Codigo, tipo.Nombre);
        }

        foreach (IGrouping<string, TipoDocumentoGdeba> grupo in tipos
            .Where(x => x.CodigoTipoDocumentoGdeba is not null)
            .GroupBy(x => x.CodigoTipoDocumentoGdeba!, StringComparer.OrdinalIgnoreCase))
        {
            if (!nombres.ContainsKey(grupo.Key) && grupo.Count() == 1)
            {
                nombres[grupo.Key] = grupo.Single().Nombre;
            }
        }

        return nombres;
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
