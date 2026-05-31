using Microsoft.EntityFrameworkCore;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence;

public sealed class ProxyGdebaDbContext : DbContext
{
    public ProxyGdebaDbContext(DbContextOptions<ProxyGdebaDbContext> options)
        : base(options)
    {
    }

    public DbSet<AplicacionConsumidora> AplicacionesConsumidoras => Set<AplicacionConsumidora>();

    public DbSet<DocumentoGdeba> Documentos => Set<DocumentoGdeba>();

    public DbSet<DocumentoCacheControl> DocumentoCacheControls => Set<DocumentoCacheControl>();

    public DbSet<DocumentoArchivoLocal> DocumentoArchivosLocales => Set<DocumentoArchivoLocal>();

    public DbSet<Expediente> Expedientes => Set<Expediente>();

    public DbSet<ExpedienteCacheControl> ExpedienteCacheControls => Set<ExpedienteCacheControl>();

    public DbSet<ExpedienteDocumento> ExpedienteDocumentos => Set<ExpedienteDocumento>();

    public DbSet<HistorialExpedienteCacheControl> HistorialExpedienteCacheControls => Set<HistorialExpedienteCacheControl>();

    public DbSet<MovimientoExpediente> MovimientosExpediente => Set<MovimientoExpediente>();

    public DbSet<RegistroAuditoria> RegistrosAuditoria => Set<RegistroAuditoria>();

    public DbSet<TrataGdeba> Tratas => Set<TrataGdeba>();

    public DbSet<TrataCacheControl> TrataCacheControls => Set<TrataCacheControl>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProxyGdebaDbContext).Assembly);
    }
}
