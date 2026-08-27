using ServicioSistemaWebProxyGdebaDvba.Api.Filters;
using ServicioSistemaWebProxyGdebaDvba.Api.Middleware;
using ServicioSistemaWebProxyGdebaDvba.Application;
using ServicioSistemaWebProxyGdebaDvba.Infrastructure;
using ServicioSistemaWebProxyGdebaDvba.Infrastructure.Gdeba;
using ServicioSistemaWebProxyGdebaDvba.Infrastructure.Transversales.Seguridad;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.MapType<DateOnly>(() => new OpenApiSchema { Type = "string", Format = "date" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Token JWT emitido por DVBA-Auth."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }] = Array.Empty<string>()
    });
});
//inyeccion de dependencias
builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddGdebaIntegration(builder.Configuration);
builder.Services.AddSeguridadJwt(builder.Configuration);
builder.Services.AddScoped<RequiereUsuarioGdebaFilter>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseMiddleware<GdebaExceptionMiddleware>();
app.UseMiddleware<ApplicationIdentificationMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
