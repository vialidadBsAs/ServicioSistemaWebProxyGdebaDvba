using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Configurations;

public sealed class TrataHabilitadaVialidadConfiguration : IEntityTypeConfiguration<TrataHabilitadaVialidad>
{
    public void Configure(EntityTypeBuilder<TrataHabilitadaVialidad> builder)
    {
        builder.ToTable("TratasHabilitadasVialidad");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.CodigoTrata)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.DescripcionTrata).HasMaxLength(500);
        builder.Property(x => x.EstadoTrata).HasMaxLength(100);
        builder.Property(x => x.AcronimoGedo).HasMaxLength(50);
        builder.Property(x => x.IdTrataGdeba).HasMaxLength(100);
        builder.Property(x => x.TipoReservaDescripcion).HasMaxLength(500);
        builder.Property(x => x.TipoReservaId).HasMaxLength(100);
        builder.Property(x => x.TipoReservaDescripcionTipoReserva).HasMaxLength(500);

        builder.Property(x => x.CodigoReparticion)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.NombreReparticion).HasMaxLength(300);

        builder.Property(x => x.CodigoOrganismo)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.NombreOrganismo).HasMaxLength(300);

        builder.HasIndex(x => new { x.CodigoTrata, x.CodigoOrganismo, x.CodigoReparticion })
            .IsUnique();

        builder.HasIndex(x => x.CodigoTrata);
        builder.HasIndex(x => x.IdTrataGdeba);
        builder.HasIndex(x => x.CodigoOrganismo);
        builder.HasIndex(x => x.CodigoReparticion);
    }
}
