using ServicioSistemaWebProxyGdebaDvba.Worker;
using ServicioSistemaWebProxyGdebaDvba.Application;
using ServicioSistemaWebProxyGdebaDvba.Infrastructure;
using ServicioSistemaWebProxyGdebaDvba.Infrastructure.Gdeba;
using ServicioSistemaWebProxyGdebaDvba.Infrastructure.Messaging;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Services.Configure<DocumentoDetailEnrichmentWorkerOptions>(
    builder.Configuration.GetSection(DocumentoDetailEnrichmentWorkerOptions.SectionName));
builder.Services.Configure<DescubrimientoExpedientesWorkerOptions>(
    builder.Configuration.GetSection(DescubrimientoExpedientesWorkerOptions.SectionName));
builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddGdebaIntegration(builder.Configuration);
if (builder.Configuration.GetValue<bool>("Messaging:RabbitMq:Enabled"))
{
    builder.Services.AddRabbitMqMessaging(builder.Configuration, includeConsumers: true);
}
builder.Services.AddHostedService<DocumentoDetailEnrichmentWorker>();
builder.Services.AddHostedService<DescubrimientoExpedientesWorker>();

var host = builder.Build();
host.Run();
