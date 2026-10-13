using ServicioSistemaWebProxyGdebaDvba.Worker;
using ServicioSistemaWebProxyGdebaDvba.Application;
using ServicioSistemaWebProxyGdebaDvba.Infrastructure;
using ServicioSistemaWebProxyGdebaDvba.Infrastructure.Gdeba;
using ServicioSistemaWebProxyGdebaDvba.Infrastructure.Messaging;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
// Fuera de un servicio de Windows no tiene efecto; como servicio fija el content root
// en la carpeta del ejecutable y agrega el registro en el visor de eventos.
builder.Services.AddWindowsService(options => options.ServiceName = "ProxyGdebaDvbaWorker");
builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddGdebaIntegration(builder.Configuration);
if (builder.Configuration.GetValue<bool>("Messaging:RabbitMq:Enabled"))
{
    builder.Services.AddRabbitMqMessaging(builder.Configuration, includeConsumers: true);
}
builder.Services.AddHostedService<DocumentoDetailEnrichmentWorker>();
builder.Services.AddHostedService<DescubrimientoExpedientesWorker>();
builder.Services.AddHostedService<ExpedienteDetalladoWorker>();

var host = builder.Build();
host.Run();
