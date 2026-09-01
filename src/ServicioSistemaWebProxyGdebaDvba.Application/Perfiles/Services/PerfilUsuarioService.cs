using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using ServicioSistemaWebProxyGdebaDvba.Application.Perfiles.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Perfiles.Models;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities;
using ServicioSistemaWebProxyGdebaDvba.Domain.ValueObjects;
using TrackableEntities.Common.Core;
using URF.Core.Abstractions;
using URF.Core.Abstractions.Trackable;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Perfiles.Services;

public sealed class PerfilUsuarioService : IPerfilUsuarioService
{
    private readonly ITrackableRepository<PerfilUsuario> _perfilRepository;
    private readonly IRepository<SeguimientoExpediente> _seguimientoRepository;
    private readonly IRepository<Expediente> _expedienteRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHostEnvironment _hostEnvironment;

    public PerfilUsuarioService(
        ITrackableRepository<PerfilUsuario> perfilRepository,
        IRepository<SeguimientoExpediente> seguimientoRepository,
        IRepository<Expediente> expedienteRepository,
        IUnitOfWork unitOfWork,
        IHostEnvironment hostEnvironment)
    {
        _perfilRepository = perfilRepository;
        _seguimientoRepository = seguimientoRepository;
        _expedienteRepository = expedienteRepository;
        _unitOfWork = unitOfWork;
        _hostEnvironment = hostEnvironment;
    }

    public async Task<PerfilUsuarioDto> ObtenerAsync(string usuarioInstitucional, CancellationToken cancellationToken)
    {
        string usuario = PerfilUsuarioService.NormalizarUsuario(usuarioInstitucional);
        PerfilUsuario? perfil = await _perfilRepository.Query().FirstOrDefaultAsync(x => x.UsuarioInstitucional == usuario, cancellationToken);
        return new PerfilUsuarioDto(usuario, perfil?.UsuarioGdeba);
    }

    public async Task<string?> ObtenerUsuarioGdebaAsync(string usuarioInstitucional, CancellationToken cancellationToken)
    {
        string usuario = PerfilUsuarioService.NormalizarUsuario(usuarioInstitucional);
        PerfilUsuario? perfil = await _perfilRepository.Query().FirstOrDefaultAsync(x => x.UsuarioInstitucional == usuario, cancellationToken);
        return perfil?.UsuarioGdeba;
    }

    public async Task<PerfilUsuarioDto> GuardarUsuarioGdebaAsync(string usuarioInstitucional, string? usuarioGdeba, CancellationToken cancellationToken)
    {
        string usuario = PerfilUsuarioService.NormalizarUsuario(usuarioInstitucional);
        string? usuarioGdebaNormalizado = string.IsNullOrWhiteSpace(usuarioGdeba) ? null : usuarioGdeba.Trim();
        // La identidad GDEBA pertenece a una sola persona: sin esta exclusividad la trazabilidad de quien consulta pierde sentido.
        // Solo en Development se permite compartirlo entre perfiles de prueba; el ambiente es el unico interruptor, no hay llave de configuracion.
        if (usuarioGdebaNormalizado is not null && !_hostEnvironment.IsDevelopment())
        {
            bool asignadoAOtroPerfil = await _perfilRepository.Query()
                .AnyAsync(x => x.UsuarioGdeba == usuarioGdebaNormalizado && x.UsuarioInstitucional != usuario, cancellationToken);
            if (asignadoAOtroPerfil)
            {
                throw new InvalidOperationException($"El usuario GDEBA '{usuarioGdebaNormalizado}' ya está asignado a otro perfil.");
            }
        }

        PerfilUsuario perfil = await this.CargarOCrearPerfilAsync(usuario, cancellationToken);
        perfil.ActualizarUsuarioGdeba(usuarioGdebaNormalizado);
        await this.ConfirmarPerfilAsync(perfil, cancellationToken);
        return new PerfilUsuarioDto(perfil.UsuarioInstitucional, perfil.UsuarioGdeba);
    }

    public async Task<IReadOnlyCollection<SeguimientoExpedienteDto>> ListarSeguimientosAsync(string usuarioInstitucional, CancellationToken cancellationToken)
    {
        string usuario = PerfilUsuarioService.NormalizarUsuario(usuarioInstitucional);
        IEnumerable<SeguimientoExpediente> seguimientos = await _seguimientoRepository.Query()
            .Include($"{nameof(SeguimientoExpediente.Expediente)}.{nameof(Expediente.CacheControl)}")
            .Include($"{nameof(SeguimientoExpediente.Expediente)}.{nameof(Expediente.Trata)}")
            .Where(x => x.PerfilUsuario.UsuarioInstitucional == usuario)
            .SelectAsync(cancellationToken);

        return seguimientos
            .Select(x => new SeguimientoExpedienteDto(
                x.ExpedienteId,
                x.Expediente.GdebaNumeroCompleto,
                x.Expediente.Trata?.CodigoTrata,
                x.Expediente.Trata?.DescripcionTrata,
                x.Expediente.Motivo ?? x.Expediente.DescripcionAdicional,
                x.Expediente.EstadoActual,
                x.FechaAgregado,
                x.Expediente.CacheControl?.FechaUltimaNovedad,
                TieneNovedades: x.Expediente.CacheControl?.FechaUltimaNovedad > x.FechaUltimaVista))
            .OrderByDescending(x => x.TieneNovedades)
            .ThenByDescending(x => x.FechaUltimaNovedad ?? DateTimeOffset.MinValue)
            .ThenByDescending(x => x.FechaAgregado)
            .ToArray();
    }

    public async Task SeguirExpedienteAsync(string usuarioInstitucional, Guid expedienteId, CancellationToken cancellationToken)
    {
        bool expedienteExiste = await _expedienteRepository.Query().AnyAsync(x => x.Id == expedienteId, cancellationToken);
        if (!expedienteExiste)
        {
            throw new KeyNotFoundException("No existe el expediente que se intenta seguir.");
        }

        PerfilUsuario perfil = await this.CargarOCrearPerfilAsync(usuarioInstitucional, cancellationToken);
        perfil.SeguirExpediente(expedienteId, DateTimeOffset.Now);
        await this.ConfirmarPerfilAsync(perfil, cancellationToken);
    }

    public async Task DejarDeSeguirAsync(string usuarioInstitucional, Guid expedienteId, CancellationToken cancellationToken)
    {
        PerfilUsuario? perfil = await this.CargarPerfilConSeguimientosAsync(usuarioInstitucional, cancellationToken);
        if (perfil is null || !perfil.DejarDeSeguir(expedienteId))
        {
            return;
        }

        await this.ConfirmarPerfilAsync(perfil, cancellationToken);
    }

    public async Task MarcarVistoAsync(string usuarioInstitucional, Guid expedienteId, CancellationToken cancellationToken)
    {
        PerfilUsuario? perfil = await this.CargarPerfilConSeguimientosAsync(usuarioInstitucional, cancellationToken);
        if (perfil is null || !perfil.MarcarVisto(expedienteId, DateTimeOffset.Now))
        {
            return;
        }

        await this.ConfirmarPerfilAsync(perfil, cancellationToken);
    }

    public async Task<bool> EstaSiguiendoAsync(string usuarioInstitucional, string numeroGdebaCompleto, CancellationToken cancellationToken)
    {
        string usuario = PerfilUsuarioService.NormalizarUsuario(usuarioInstitucional);
        if (PerfilUsuarioService.NormalizarNumeroCompleto(numeroGdebaCompleto) is not string numero)
        {
            return false;
        }

        return await _seguimientoRepository.Query()
            .AnyAsync(x => x.PerfilUsuario.UsuarioInstitucional == usuario && x.Expediente.GdebaNumeroCompleto == numero, cancellationToken);
    }

    public async Task SeguirExpedientePorNumeroAsync(string usuarioInstitucional, string numeroGdebaCompleto, CancellationToken cancellationToken)
    {
        Guid expedienteId = await this.ResolverExpedienteIdAsync(numeroGdebaCompleto, cancellationToken);
        await this.SeguirExpedienteAsync(usuarioInstitucional, expedienteId, cancellationToken);
    }

    public async Task DejarDeSeguirPorNumeroAsync(string usuarioInstitucional, string numeroGdebaCompleto, CancellationToken cancellationToken)
    {
        Guid? expedienteId = await this.BuscarExpedienteIdAsync(numeroGdebaCompleto, cancellationToken);
        if (expedienteId is null)
        {
            return;
        }

        await this.DejarDeSeguirAsync(usuarioInstitucional, expedienteId.Value, cancellationToken);
    }

    public async Task MarcarVistoPorNumeroAsync(string usuarioInstitucional, string numeroGdebaCompleto, CancellationToken cancellationToken)
    {
        Guid? expedienteId = await this.BuscarExpedienteIdAsync(numeroGdebaCompleto, cancellationToken);
        if (expedienteId is null)
        {
            return;
        }

        await this.MarcarVistoAsync(usuarioInstitucional, expedienteId.Value, cancellationToken);
    }

    public async Task<AperturaSeguimientoDto> AbrirExpedientePorNumeroAsync(string usuarioInstitucional, string numeroGdebaCompleto, CancellationToken cancellationToken)
    {
        AperturaSeguimientoDto sinSeguimiento = new(false, false, false, false, false);
        string usuario = PerfilUsuarioService.NormalizarUsuario(usuarioInstitucional);
        if (PerfilUsuarioService.NormalizarNumeroCompleto(numeroGdebaCompleto) is not string numero)
        {
            return sinSeguimiento;
        }

        Expediente? expediente = await _expedienteRepository.Query()
            .Include(nameof(Expediente.CacheControl))
            .FirstOrDefaultAsync(x => x.GdebaNumeroCompleto == numero, cancellationToken);
        PerfilUsuario? perfil = await this.CargarPerfilConSeguimientosAsync(usuario, cancellationToken);
        SeguimientoExpediente? seguimiento = expediente is null ? null : perfil?.Seguimientos.FirstOrDefault(x => x.ExpedienteId == expediente.Id);
        if (expediente is null || perfil is null || seguimiento is null)
        {
            return sinSeguimiento;
        }

        // Las novedades por coleccion se evaluan contra la vista anterior, y recien despues se sella la nueva vista.
        DateTimeOffset vistaAnterior = seguimiento.FechaUltimaVista;
        ExpedienteCacheControl? control = expediente.CacheControl;
        AperturaSeguimientoDto resultado = new(
            Siguiendo: true,
            NovedadCabecera: control?.FechaUltimaNovedadCabecera > vistaAnterior,
            NovedadMovimientos: control?.FechaUltimaNovedadMovimientos > vistaAnterior,
            NovedadDocumentos: control?.FechaUltimaNovedadDocumentos > vistaAnterior,
            NovedadAdjuntos: control?.FechaUltimaNovedadAdjuntos > vistaAnterior);

        perfil.MarcarVisto(expediente.Id, DateTimeOffset.Now);
        await this.ConfirmarPerfilAsync(perfil, cancellationToken);
        return resultado;
    }

    private async Task<Guid> ResolverExpedienteIdAsync(string numeroGdebaCompleto, CancellationToken cancellationToken)
    {
        return await this.BuscarExpedienteIdAsync(numeroGdebaCompleto, cancellationToken)
            ?? throw new KeyNotFoundException("No existe el expediente que se intenta seguir.");
    }

    private async Task<Guid?> BuscarExpedienteIdAsync(string numeroGdebaCompleto, CancellationToken cancellationToken)
    {
        if (PerfilUsuarioService.NormalizarNumeroCompleto(numeroGdebaCompleto) is not string numero)
        {
            return null;
        }

        Expediente? expediente = await _expedienteRepository.Query().FirstOrDefaultAsync(x => x.GdebaNumeroCompleto == numero, cancellationToken);
        return expediente?.Id;
    }

    private static string? NormalizarNumeroCompleto(string numeroGdebaCompleto)
    {
        // El front puede enviar el numero tal como lo tipeo la persona; el guardado siempre usa la forma canonica GDEBA.
        try
        {
            return NumeroGdebaCompleto.Create(numeroGdebaCompleto).Valor;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private async Task<PerfilUsuario> CargarOCrearPerfilAsync(string usuarioInstitucional, CancellationToken cancellationToken)
    {
        string usuario = PerfilUsuarioService.NormalizarUsuario(usuarioInstitucional);
        PerfilUsuario? perfil = await this.CargarPerfilConSeguimientosAsync(usuario, cancellationToken);
        if (perfil is not null)
        {
            return perfil;
        }

        PerfilUsuario nuevo = new(usuario)
        {
            TrackingState = TrackingState.Added
        };
        _perfilRepository.Insert(nuevo);
        return nuevo;
    }

    private async Task<PerfilUsuario?> CargarPerfilConSeguimientosAsync(string usuarioInstitucional, CancellationToken cancellationToken)
    {
        string usuario = PerfilUsuarioService.NormalizarUsuario(usuarioInstitucional);
        return await _perfilRepository.Query()
            .Include(nameof(PerfilUsuario.Seguimientos))
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
