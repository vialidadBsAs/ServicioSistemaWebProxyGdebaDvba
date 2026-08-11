using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities.Worker;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Configurations;

public sealed class EjecucionWorkerDescubrimientoTrataEstadoConfiguration : IEntityTypeConfiguration<EjecucionWorkerDescubrimientoTrataEstado>
{
    public void Configure(EntityTypeBuilder<EjecucionWorkerDescubrimientoTrataEstado> builder)
    {
        builder.ToTable("WorkerDescubrimiento_EjecucionesPorTrataEstado");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.EjecucionWorkerId);
        builder.HasIndex(x => new { x.EjecucionWorkerId, x.TrataHabilitadaVialidadId, x.EstadoExpedienteGdebaId }).IsUnique();
        builder.HasOne(x => x.TrataHabilitadaVialidad)
            .WithMany()
            .HasForeignKey(x => x.TrataHabilitadaVialidadId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.EstadoExpedienteGdeba)
            .WithMany()
            .HasForeignKey(x => x.EstadoExpedienteGdebaId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.ExpedientesDescubiertos)
            .WithOne(x => x.EjecucionWorkerDescubrimientoTrataEstado)
            .HasForeignKey(x => x.EjecucionWorkerDescubrimientoTrataEstadoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
