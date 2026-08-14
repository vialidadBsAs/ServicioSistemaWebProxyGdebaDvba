using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities.Worker;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Configurations;

public sealed class OmisionCorridaProgramadaWorkerConfiguration : IEntityTypeConfiguration<OmisionCorridaProgramadaWorker>
{
    public void Configure(EntityTypeBuilder<OmisionCorridaProgramadaWorker> builder)
    {
        builder.ToTable("Worker_OmisionesCorridaProgramada");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OmitidaPor).HasMaxLength(200).IsRequired();
        builder.HasIndex(x => new { x.Proceso, x.FechaLocal }).IsUnique();
    }
}
