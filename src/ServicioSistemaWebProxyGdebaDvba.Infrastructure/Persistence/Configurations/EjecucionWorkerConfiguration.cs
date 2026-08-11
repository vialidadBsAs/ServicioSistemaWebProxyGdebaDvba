using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities.Worker;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Configurations;

public sealed class EjecucionWorkerConfiguration : IEntityTypeConfiguration<EjecucionWorker>
{
    public void Configure(EntityTypeBuilder<EjecucionWorker> builder)
    {
        builder.ToTable("Worker_Ejecuciones");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Resumen).HasMaxLength(1000);
        builder.HasIndex(x => new { x.Proceso, x.FechaInicio });
        builder.HasIndex(x => x.SolicitudEjecucionWorkerId).IsUnique().HasFilter("[SolicitudEjecucionWorkerId] IS NOT NULL");
        builder.HasMany(x => x.ResultadosDescubrimientoTrataEstado)
            .WithOne(x => x.EjecucionWorker)
            .HasForeignKey(x => x.EjecucionWorkerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
