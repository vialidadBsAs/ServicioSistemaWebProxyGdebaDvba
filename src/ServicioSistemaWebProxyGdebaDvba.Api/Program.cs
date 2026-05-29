using ServicioSistemaWebProxyGdebaDvba.Api.Middleware;
using ServicioSistemaWebProxyGdebaDvba.Application;
using ServicioSistemaWebProxyGdebaDvba.Infrastructure;
using ServicioSistemaWebProxyGdebaDvba.Infrastructure.Gdeba;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
//inyeccion de dependencias
builder.Services.AddApplication();
builder.Services.AddInfrastructure();
builder.Services.AddGdebaIntegration(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseMiddleware<ApplicationIdentificationMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.Run();
