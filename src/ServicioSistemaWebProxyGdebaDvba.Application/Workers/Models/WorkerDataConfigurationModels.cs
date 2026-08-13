using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Workers.Models;

public sealed record ConfiguracionDatosWorkerDto(
    IReadOnlyCollection<ConfiguracionTemaWorkerDto> Temas,
    IReadOnlyCollection<ConfiguracionTrataDescubrimientoWorkerDto> Tratas);

public sealed record ConfiguracionTemaWorkerDto(Guid TemaExpedienteId, bool Habilitado, int Prioridad);

public sealed record ConfiguracionTrataDescubrimientoWorkerDto(string CodigoTrata, bool Habilitada, int Prioridad);

public sealed record GuardarConfiguracionTemaWorkerRequest(ProcesoWorker Proceso, Guid TemaExpedienteId, bool Habilitado, int Prioridad);

public sealed record GuardarConfiguracionTrataDescubrimientoWorkerRequest(string CodigoTrata, bool Habilitada, int Prioridad);
