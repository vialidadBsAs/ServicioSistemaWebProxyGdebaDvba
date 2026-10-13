using ServicioSistemaWebProxyGdebaDvba.Worker;
using ServicioSistemaWebProxyGdebaDvba.Application;
using ServicioSistemaWebProxyGdebaDvba.Infrastructure;
using ServicioSistemaWebProxyGdebaDvba.Infrastructure.Gdeba;
using ServicioSistemaWebProxyGdebaDvba.Infrastructure.Messaging;
using Microsoft.Extensions.Hosting.WindowsServices;
using Serilog;

// Un servicio de Windows arranca con System32 como directorio actual: las rutas relativas (p. ej. la del log) se resuelven contra la carpeta del ejecutable.
if (WindowsServiceHelpers.IsWindowsService())
{
    Directory.SetCurrentDirectory(AppContext.BaseDirectory);
}

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
// Log en archivo para el administrador del servidor: se activa con la seccion Serilog (appsettings.Production.json).
if (builder.Configuration.GetSection("Serilog").Exists())
{
    builder.Logging.AddSerilog(new LoggerConfiguration().ReadFrom.Configuration(builder.Configuration).CreateLogger(), dispose: true);
}
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
