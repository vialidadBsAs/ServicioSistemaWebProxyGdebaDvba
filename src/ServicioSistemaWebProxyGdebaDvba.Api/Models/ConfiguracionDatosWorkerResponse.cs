using ServicioSistemaWebProxyGdebaDvba.Application.Workers.Models;

namespace ServicioSistemaWebProxyGdebaDvba.Api.Models;

public sealed record ConfiguracionDatosWorkerResponse(
    IReadOnlyCollection<ConfiguracionTemaWorkerResponse> Temas,
    IReadOnlyCollection<ConfiguracionTrataDescubrimientoWorkerResponse> Tratas)
{
    public static ConfiguracionDatosWorkerResponse Create(ConfiguracionDatosWorkerDto configuracion)
    {
        return new ConfiguracionDatosWorkerResponse(
            configuracion.Temas.Select(ConfiguracionTemaWorkerResponse.Create).ToArray(),
            configuracion.Tratas.Select(ConfiguracionTrataDescubrimientoWorkerResponse.Create).ToArray());
    }
}

public sealed record ConfiguracionTemaWorkerResponse(Guid TemaExpedienteId, bool Habilitado, int Prioridad)
{
    public static ConfiguracionTemaWorkerResponse Create(ConfiguracionTemaWorkerDto configuracion)
    {
        return new ConfiguracionTemaWorkerResponse(configuracion.TemaExpedienteId, configuracion.Habilitado, configuracion.Prioridad);
    }
}

public sealed record ConfiguracionTrataDescubrimientoWorkerResponse(string CodigoTrata, bool Habilitada, int Prioridad)
{
    public static ConfiguracionTrataDescubrimientoWorkerResponse Create(ConfiguracionTrataDescubrimientoWorkerDto configuracion)
    {
        return new ConfiguracionTrataDescubrimientoWorkerResponse(configuracion.CodigoTrata, configuracion.Habilitada, configuracion.Prioridad);
    }
}

public sealed record GuardarConfiguracionTemaWorkerApiRequest(bool Habilitado, int Prioridad);

public sealed record GuardarConfiguracionTrataDescubrimientoWorkerApiRequest(bool Habilitada, int Prioridad);
