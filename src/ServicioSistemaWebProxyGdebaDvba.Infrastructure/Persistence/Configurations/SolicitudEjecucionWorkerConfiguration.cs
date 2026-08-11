using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities.Worker;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Configurations;

public sealed class SolicitudEjecucionWorkerConfiguration : IEntityTypeConfiguration<SolicitudEjecucionWorker>
{
    public void Configure(EntityTypeBuilder<SolicitudEjecucionWorker> builder)
    {
        builder.ToTable("Worker_SolicitudesEjecucion");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SolicitadaPor).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Mensaje).HasMaxLength(1000);
        builder.HasIndex(x => new { x.Proceso, x.Estado, x.FechaSolicitud });
        builder.HasIndex(x => x.EjecucionWorkerId).IsUnique().HasFilter("[EjecucionWorkerId] IS NOT NULL");
    }
}
