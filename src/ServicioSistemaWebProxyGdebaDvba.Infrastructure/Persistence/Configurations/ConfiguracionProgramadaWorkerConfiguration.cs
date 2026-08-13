using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities.Worker;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Configurations;

public sealed class ConfiguracionProgramadaWorkerConfiguration : IEntityTypeConfiguration<ConfiguracionProgramadaWorker>
{
    public void Configure(EntityTypeBuilder<ConfiguracionProgramadaWorker> builder)
    {
        builder.ToTable("Worker_ConfiguracionesProgramadas");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.Proceso).IsUnique();
        builder.Property(x => x.HoraInicioLocal).HasColumnType("time");
        builder.Property(x => x.HoraFinLocal).HasColumnType("time");
    }
}
