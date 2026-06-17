using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.Auditoria.Contracts;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.Auditoria.Models;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities;
using URF.Core.Abstractions;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Transversales.Auditoria.Services;

public sealed class PersistedAuditoriaService : IAuditoriaService
{
    private readonly IRepository<AplicacionConsumidora> _aplicacionConsumidoraRepository;
    private readonly IRepository<RegistroAuditoria> _auditoriaRepository;

    public PersistedAuditoriaService(
        IRepository<AplicacionConsumidora> aplicacionConsumidoraRepository,
        IRepository<RegistroAuditoria> auditoriaRepository)
    {
        _aplicacionConsumidoraRepository = aplicacionConsumidoraRepository;
        _auditoriaRepository = auditoriaRepository;
    }

    public Task RegistrarAsync(RegistrarAuditoriaRequest request, CancellationToken cancellationToken)
    {
        var aplicacion = this.ResolverAplicacionConsumidora(request.AplicacionConsumidoraCodigo);
        var registro = new RegistroAuditoria(aplicacion.Id, request.OperacionSolicitada, request.OperacionGdeba, request.Recurso, request.Ambiente, request.Fuente, request.Exitoso, request.Mensaje, request.Fecha);

        _auditoriaRepository.Insert(registro);
        return Task.CompletedTask;
    }

    private AplicacionConsumidora ResolverAplicacionConsumidora(string codigo)
    {
        var codigoNormalizado = string.IsNullOrWhiteSpace(codigo) ? "desconocida" : codigo.Trim();
        var aplicacion = _aplicacionConsumidoraRepository
            .Queryable()
            .FirstOrDefault(x => x.Codigo == codigoNormalizado);

        if (aplicacion is not null)
        {
            return aplicacion;
        }

        aplicacion = new AplicacionConsumidora(codigoNormalizado, codigoNormalizado);
        _aplicacionConsumidoraRepository.Insert(aplicacion);
        return aplicacion;
    }
}
