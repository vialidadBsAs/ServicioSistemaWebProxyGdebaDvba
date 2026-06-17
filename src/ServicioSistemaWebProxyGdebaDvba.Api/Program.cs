using ServicioSistemaWebProxyGdebaDvba.Api.Middleware;
using ServicioSistemaWebProxyGdebaDvba.Application;
using ServicioSistemaWebProxyGdebaDvba.Infrastructure;
using ServicioSistemaWebProxyGdebaDvba.Infrastructure.Gdeba;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => options.MapType<DateOnly>(() => new OpenApiSchema { Type = "string", Format = "date" }));
//inyeccion de dependencias
builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddGdebaIntegration(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseMiddleware<GdebaExceptionMiddleware>();
app.UseMiddleware<ApplicationIdentificationMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.Run();
