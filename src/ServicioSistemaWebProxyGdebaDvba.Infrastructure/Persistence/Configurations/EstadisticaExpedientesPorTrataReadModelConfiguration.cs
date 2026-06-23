using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServicioSistemaWebProxyGdebaDvba.Application.Estadisticas.Models;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Configurations;

public sealed class EstadisticaExpedientesPorTrataReadModelConfiguration
    : IEntityTypeConfiguration<EstadisticaExpedientesPorTrataReadModel>
{
    public void Configure(EntityTypeBuilder<EstadisticaExpedientesPorTrataReadModel> builder)
    {
        builder.HasNoKey();
        builder.ToView(null);

        builder.Property(x => x.CodigoTrata)
            .HasMaxLength(50);

        builder.Property(x => x.DescripcionTrata)
            .HasMaxLength(500);
    }
}
