using Microsoft.Extensions.Logging;
using ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Gdeba;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities;
using URF.Core.Abstractions;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Gdeba;

internal sealed class GdebaInvocacionRecorder : IGdebaInvocacionRecorder
{
    private readonly IRepository<OperacionGdeba> _operacionRepository;
    private readonly IRepository<InvocacionGdeba> _invocacionRepository;
    private readonly IGdebaExecutionContext _executionContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GdebaInvocacionRecorder> _logger;

    public GdebaInvocacionRecorder(IRepository<OperacionGdeba> operacionRepository, IRepository<InvocacionGdeba> invocacionRepository,
        IGdebaExecutionContext executionContext, IUnitOfWork unitOfWork, ILogger<GdebaInvocacionRecorder> logger)
    {
        _operacionRepository = operacionRepository;
        _invocacionRepository = invocacionRepository;
        _executionContext = executionContext;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task RegistrarAsync(string servicio, string metodo, bool exitosa, int? estadoHttp, CancellationToken cancellationToken)
    {
        try
        {
            var operacion = await _operacionRepository.Query()
                .FirstOrDefaultAsync(x => x.Servicio == servicio && x.Metodo == metodo, cancellationToken);

            if (operacion is null)
            {
                _logger.LogWarning("No existe configuracion de cuota para la operacion GDEBA {Servicio}.{Metodo}.", servicio, metodo);
                return;
            }

            _invocacionRepository.Insert(new InvocacionGdeba(operacion.Id, _executionContext.Ambiente, DateTimeOffset.Now, exitosa, estadoHttp));
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo registrar el consumo de cuota GDEBA para {Servicio}.{Metodo}.", servicio, metodo);
        }
    }
}
