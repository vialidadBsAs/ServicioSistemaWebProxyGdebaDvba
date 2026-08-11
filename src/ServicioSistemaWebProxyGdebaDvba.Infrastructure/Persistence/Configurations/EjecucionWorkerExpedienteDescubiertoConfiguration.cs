using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities.Worker;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Configurations;

public sealed class EjecucionWorkerExpedienteDescubiertoConfiguration : IEntityTypeConfiguration<EjecucionWorkerExpedienteDescubierto>
{
    public void Configure(EntityTypeBuilder<EjecucionWorkerExpedienteDescubierto> builder)
    {
        builder.ToTable("WorkerDescubrimiento_ExpedientesDescubiertosPorEjecucionTrataEstado");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.ExpedienteId);
        builder.HasIndex(x => new { x.EjecucionWorkerDescubrimientoTrataEstadoId, x.ExpedienteId }).IsUnique();
        builder.HasOne(x => x.Expediente)
            .WithMany()
            .HasForeignKey(x => x.ExpedienteId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
