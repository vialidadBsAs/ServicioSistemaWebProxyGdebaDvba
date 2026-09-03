using ServicioSistemaWebProxyGdebaDvba.Application.Clasificacion.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Clasificacion.Models;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities;
using URF.Core.Abstractions;
using URF.Core.Abstractions.Trackable;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Clasificacion.Services;

public sealed class TemaExpedienteAdminService : ITemaExpedienteAdminService
{
    private readonly ITrackableRepository<TemaExpediente> _temaRepository;
    private readonly ITrackableRepository<TemaExpedienteTrata> _temaTrataRepository;
    private readonly IRepository<TrataHabilitadaVialidad> _trataRepository;
    private readonly IUnitOfWork _unitOfWork;

    public TemaExpedienteAdminService(ITrackableRepository<TemaExpediente> temaRepository, ITrackableRepository<TemaExpedienteTrata> temaTrataRepository, IRepository<TrataHabilitadaVialidad> trataRepository, IUnitOfWork unitOfWork)
    {
        _temaRepository = temaRepository;
        _temaTrataRepository = temaTrataRepository;
        _trataRepository = trataRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyCollection<TemaExpedienteDto>> ObtenerTemasAsync(string usuarioPropietario, CancellationToken cancellationToken)
    {
        string usuario = TemaExpedienteAdminService.NormalizarUsuario(usuarioPropietario);
        var temas = await _temaRepository.Query()
            .Include($"{nameof(TemaExpediente.Tratas)}.{nameof(TemaExpedienteTrata.TrataHabilitadaVialidad)}")
            .Where(x => x.UsuarioPropietario == usuario)
            .OrderBy(x => x.Nombre)
            .SelectAsync(cancellationToken);

        return temas.Select(TemaExpedienteAdminService.MapearTema).ToArray();
    }

    public async Task<IReadOnlyCollection<TrataHabilitadaVialidadDto>> ObtenerTratasHabilitadasAsync(CancellationToken cancellationToken)
    {
        var tratas = await _trataRepository.Query()
            .SelectAsync(cancellationToken);

        // El mismo codigo puede estar habilitado desde varias reparticiones; para asignar a temas se ofrece una sola vez, con su fila representante.
        return tratas
            .GroupBy(x => x.CodigoTrata.Trim().ToUpperInvariant(), StringComparer.OrdinalIgnoreCase)
            .Select(x => TrataHabilitadaVialidad.ElegirRepresentantePorCodigo(x))
            .OrderBy(x => x.CodigoTrata)
            .Select(TemaExpedienteAdminService.MapearTrata)
            .ToArray();
    }

    public async Task<TemaExpedienteDto> CrearTemaAsync(GuardarTemaExpedienteRequest request, string usuarioPropietario, CancellationToken cancellationToken)
    {
        string usuario = TemaExpedienteAdminService.NormalizarUsuario(usuarioPropietario);
        await this.ValidarCodigoUnicoAsync(request.Codigo, usuario, temaIdExcluido: null, cancellationToken);
        var tema = new TemaExpediente(request.Codigo, request.Nombre, usuario, request.Descripcion);
        await this.AsignarTratasAsync(tema, request.TratasHabilitadasVialidadIds, cancellationToken);

        _temaRepository.Insert(tema);
        _temaRepository.ApplyChanges(tema);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return TemaExpedienteAdminService.MapearTema(tema);
    }

    public async Task<TemaExpedienteDto> ActualizarTemaAsync(Guid temaId, GuardarTemaExpedienteRequest request, string usuarioPropietario, CancellationToken cancellationToken)
    {
        string usuario = TemaExpedienteAdminService.NormalizarUsuario(usuarioPropietario);
        // El tema de otro dueno se trata como inexistente: no se revela que existe.
        var tema = await _temaRepository.Query()
            .Include($"{nameof(TemaExpediente.Tratas)}.{nameof(TemaExpedienteTrata.TrataHabilitadaVialidad)}")
            .FirstOrDefaultAsync(x => x.Id == temaId && x.UsuarioPropietario == usuario, cancellationToken);
        if (tema is null) throw new KeyNotFoundException("No existe el tema solicitado.");

        await this.ValidarCodigoUnicoAsync(request.Codigo, usuario, tema.Id, cancellationToken);
        tema.Actualizar(request.Codigo, request.Nombre, request.Descripcion);
        await this.ReemplazarTratasAsync(tema, request.TratasHabilitadasVialidadIds, cancellationToken);

        _temaRepository.Update(tema);
        _temaRepository.ApplyChanges(tema);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return TemaExpedienteAdminService.MapearTema(tema);
    }

    public async Task EliminarTemaAsync(Guid temaId, string usuarioPropietario, CancellationToken cancellationToken)
    {
        string usuario = TemaExpedienteAdminService.NormalizarUsuario(usuarioPropietario);
        var tema = await _temaRepository.Query().FirstOrDefaultAsync(x => x.Id == temaId && x.UsuarioPropietario == usuario, cancellationToken);
        if (tema is null) return;

        _temaRepository.Delete(tema);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task AsignarTratasAsync(TemaExpediente tema, IReadOnlyCollection<Guid>? tratasIds, CancellationToken cancellationToken)
    {
        var tratas = await this.CargarTratasSeleccionadasAsync(tratasIds, cancellationToken);
        foreach (var trata in tratas)
        {
            tema.AsignarTrata(trata);
        }
    }

    private async Task ReemplazarTratasAsync(TemaExpediente tema, IReadOnlyCollection<Guid>? tratasIds, CancellationToken cancellationToken)
    {
        var idsSeleccionados = (tratasIds ?? Array.Empty<Guid>()).Where(x => x != Guid.Empty).Distinct().ToHashSet();
        var asignacionesARemover = tema.Tratas.Where(x => !idsSeleccionados.Contains(x.TrataHabilitadaVialidadId)).ToArray();
        foreach (var asignacion in asignacionesARemover)
        {
            tema.QuitarTrata(asignacion.TrataHabilitadaVialidadId);
            _temaTrataRepository.Delete(asignacion);
        }

        await this.AsignarTratasAsync(tema, idsSeleccionados.ToArray(), cancellationToken);
    }

    private async Task<IReadOnlyCollection<TrataHabilitadaVialidad>> CargarTratasSeleccionadasAsync(IEnumerable<Guid>? tratasIds, CancellationToken cancellationToken)
    {
        var ids = (tratasIds ?? Array.Empty<Guid>()).Where(x => x != Guid.Empty).Distinct().ToArray();
        if (ids.Length == 0) return Array.Empty<TrataHabilitadaVialidad>();

        var tratas = await _trataRepository.Query().Where(x => ids.Contains(x.Id)).SelectAsync(cancellationToken);
        if (tratas.Count() != ids.Length) throw new InvalidOperationException("Una o más tratas seleccionadas no están habilitadas.");

        return tratas.ToArray();
    }

    private async Task ValidarCodigoUnicoAsync(string codigo, string usuarioPropietario, Guid? temaIdExcluido, CancellationToken cancellationToken)
    {
        var codigoNormalizado = string.IsNullOrWhiteSpace(codigo) ? string.Empty : codigo.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(codigoNormalizado)) return;

        var existe = await _temaRepository.Query()
            .AnyAsync(x => x.UsuarioPropietario == usuarioPropietario && x.Codigo == codigoNormalizado && (!temaIdExcluido.HasValue || x.Id != temaIdExcluido.Value), cancellationToken);
        if (existe) throw new InvalidOperationException($"Ya tenés un tema con el código '{codigoNormalizado}'.");
    }

    private static string NormalizarUsuario(string usuarioPropietario)
    {
        return string.IsNullOrWhiteSpace(usuarioPropietario)
            ? throw new ArgumentException("Se requiere el usuario propietario del tema.", nameof(usuarioPropietario))
            : usuarioPropietario.Trim();
    }

    private static TemaExpedienteDto MapearTema(TemaExpediente tema)
    {
        return new TemaExpedienteDto(tema.Id, tema.Codigo, tema.Nombre, tema.Descripcion, tema.Tratas
            .OrderBy(x => x.TrataHabilitadaVialidad.CodigoTrata)
            .ThenBy(x => x.TrataHabilitadaVialidad.CodigoReparticion)
            .Select(x => TemaExpedienteAdminService.MapearTrata(x.TrataHabilitadaVialidad))
            .ToArray());
    }

    private static TrataHabilitadaVialidadDto MapearTrata(TrataHabilitadaVialidad trata)
    {
        return new TrataHabilitadaVialidadDto(trata.Id, trata.CodigoTrata, trata.DescripcionTrata, trata.CodigoReparticion, trata.NombreReparticion);
    }
}
