using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ServicioSistemaWebProxyGdebaDvba.Application.Perfiles.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities;
using TrackableEntities.Common.Core;
using URF.Core.Abstractions;
using URF.Core.Abstractions.Trackable;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Perfiles.Services;

public sealed class DetectorCoincidenciasAlertaDocumental : IDetectorCoincidenciasAlertaDocumental
{
    private readonly ITrackableRepository<AlertaDocumental> _alertaRepository;
    private readonly IRepository<CoincidenciaAlertaDocumental> _coincidenciaRepository;
    private readonly ILogger<DetectorCoincidenciasAlertaDocumental> _logger;

    public DetectorCoincidenciasAlertaDocumental(
        ITrackableRepository<AlertaDocumental> alertaRepository,
        IRepository<CoincidenciaAlertaDocumental> coincidenciaRepository,
        ILogger<DetectorCoincidenciasAlertaDocumental> logger)
    {
        _alertaRepository = alertaRepository;
        _coincidenciaRepository = coincidenciaRepository;
        _logger = logger;
    }

    public async Task RegistrarCoincidenciasAsync(Expediente expediente, DateTimeOffset fechaDeteccion, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(expediente);

        // Candidatos: vinculos que nacen en esta consolidacion y vinculos cuyo documento o vinculacion recien ahora tienen fecha. El detalle
        // de GDEBA crea el vinculo con el documento solo por numero; el historial de pases, una consulta despues, trae la fecha de creacion
        // del documento, la de vinculacion y el generador. La alerta decide por la fecha de creacion (la firma): una resolucion vieja que
        // alguien vincula hoy no es una resolucion nueva. Sin fecha de creacion no se decide, y el vinculo se evalua cuando aparece.
        ExpedienteDocumento[] candidatos = expediente.Documentos
            .Where(x => x.TrackingState == TrackingState.Added || x.FechaVinculacionRecienConocida || x.Documento.FechaCreacionRecienConocida)
            .Where(x => x.Documento.FechaCreacion is not null)
            .ToArray();
        _logger.LogDebug(
            "Alertas documentales: {Expediente} consolido {Vinculos} vinculos ({Nuevos} nuevos, {SinFecha} sin fecha de creacion); {Candidatos} candidatos a evaluar.",
            expediente.GdebaNumeroCompleto, expediente.Documentos.Count, expediente.Documentos.Count(x => x.TrackingState == TrackingState.Added),
            expediente.Documentos.Count(x => x.Documento.FechaCreacion is null), candidatos.Length);
        if (candidatos.Length == 0)
        {
            return;
        }

        string[] tiposDetectados = candidatos
            .Select(x => x.Documento.ActuacionTipoCodigo)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.ToUpperInvariant())
            .Distinct()
            .ToArray();
        if (tiposDetectados.Length == 0)
        {
            return;
        }

        // Query() de URF (sin seguimiento de cambios), no Queryable(): ApplyChanges recorre el grafo con TrackGraph, y EF no entra en una
        // entidad que ya esta rastreada, asi que con la alerta rastreada la coincidencia agregada a la coleccion nunca llegaba al INSERT.
        List<AlertaDocumental> alertas = (await _alertaRepository.Query()
            .Where(x => x.Activa && tiposDetectados.Contains(x.TipoDocumentoCodigo))
            .SelectAsync(cancellationToken))
            .ToList();
        _logger.LogDebug(
            "Alertas documentales: tipos detectados {Tipos}; {Alertas} alertas activas para esos tipos.", string.Join(",", tiposDetectados), alertas.Count);
        if (alertas.Count == 0)
        {
            return;
        }

        // La coincidencia es por documento, no por vinculo: una resolucion vinculada a tres expedientes es una sola alerta, la del
        // expediente donde se detecto primero (la pantalla muestra los otros expedientes donde esta vinculada). Ademas un vinculo puede
        // volver a evaluarse (coincidio por la fecha de creacion y luego recibio fecha de vinculacion); lo ya registrado no se repite.
        Guid[] alertaIds = alertas.Select(x => x.Id).ToArray();
        Guid[] documentoIds = candidatos.Select(x => x.DocumentoId).Distinct().ToArray();
        HashSet<(Guid AlertaId, Guid DocumentoId)> existentes = (await _coincidenciaRepository.Queryable()
            .Where(x => alertaIds.Contains(x.AlertaDocumentalId) && documentoIds.Contains(x.ExpedienteDocumento.DocumentoId))
            .Select(x => new { x.AlertaDocumentalId, x.ExpedienteDocumento.DocumentoId })
            .ToListAsync(cancellationToken))
            .Select(x => (x.AlertaDocumentalId, x.DocumentoId))
            .ToHashSet();

        int registradas = 0;
        foreach (AlertaDocumental alerta in alertas)
        {
            foreach (ExpedienteDocumento vinculo in candidatos)
            {
                if (existentes.Contains((alerta.Id, vinculo.DocumentoId)))
                {
                    continue;
                }

                // El tipo especifico (ACMED, DICTA...) lo trae el historial junto con la fecha de creacion; el detalle solo conoce el acronimo.
                bool coincide = alerta.Coincide(
                    vinculo.Documento.ActuacionTipoCodigo, vinculo.Documento.TipoDocumentoCodigo, expediente.TrataId, vinculo.UsuarioGenerador, vinculo.Documento.FechaCreacion);
                _logger.LogDebug(
                    "Alertas documentales: alerta {Alerta} ({Tipo}) vs {Documento} (creado {FechaCreacion:yyyy-MM-dd HH:mm}, generador {Generador}, trata {Trata}): {Resultado}.",
                    alerta.Id, alerta.TipoDocumentoCodigo, vinculo.Documento.NumeroActuacionCompleto, vinculo.Documento.FechaCreacion, vinculo.UsuarioGenerador,
                    expediente.TrataId, coincide ? "coincide" : "no coincide");
                if (!coincide)
                {
                    continue;
                }

                // La alerta crea la coincidencia (es su aggregate) y el repositorio de coincidencias la registra como alta explicita; la
                // alerta en si no cambia, asi que no pasa por Update/ApplyChanges. Solo la clave foranea: la coincidencia no arrastra el
                // grafo del vinculo, que ya trackea el repositorio de expedientes. IMPORTANTE: este detector debe correr despues del
                // ApplyChanges del expediente, porque ese recorrido desacopla y vuelve a acoplar cada vinculo, y al desacoplarlo EF suelta
                // la coincidencia Added que depende de el.
                CoincidenciaAlertaDocumental coincidencia = alerta.RegistrarCoincidencia(vinculo.Id, fechaDeteccion);
                _coincidenciaRepository.Insert(coincidencia);
                existentes.Add((alerta.Id, vinculo.DocumentoId));
                registradas++;
            }
        }

        if (registradas > 0)
        {
            _logger.LogInformation("Alertas documentales: {Coincidencias} coincidencias nuevas registradas para {Expediente}.", registradas, expediente.GdebaNumeroCompleto);
        }
    }
}
