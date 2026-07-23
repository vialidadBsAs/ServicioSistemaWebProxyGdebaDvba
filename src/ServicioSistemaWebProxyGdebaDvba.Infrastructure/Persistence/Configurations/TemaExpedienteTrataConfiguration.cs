using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Configurations;

public sealed class TemaExpedienteTrataConfiguration : IEntityTypeConfiguration<TemaExpedienteTrata>
{
    public void Configure(EntityTypeBuilder<TemaExpedienteTrata> builder)
    {
        builder.ToTable("TemaExpedienteTratas");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.TrataHabilitadaVialidadId);
        builder.HasIndex(x => new { x.TemaExpedienteId, x.TrataHabilitadaVialidadId }).IsUnique();
        builder.HasOne(x => x.TrataHabilitadaVialidad).WithMany(x => x.TemasExpediente).HasForeignKey(x => x.TrataHabilitadaVialidadId).OnDelete(DeleteBehavior.Cascade);
    }
}
