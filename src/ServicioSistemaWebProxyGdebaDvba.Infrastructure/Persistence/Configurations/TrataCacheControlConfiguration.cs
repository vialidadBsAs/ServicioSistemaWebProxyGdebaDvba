using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Configurations;

public sealed class TrataCacheControlConfiguration : IEntityTypeConfiguration<TrataCacheControl>
{
    public void Configure(EntityTypeBuilder<TrataCacheControl> builder)
    {
        builder.ToTable("TrataCacheControls");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.FuenteUltimaRespuesta)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.UltimoErrorConsulta)
            .HasMaxLength(2000);

        builder.HasIndex(x => x.TrataId)
            .IsUnique();

        builder.HasIndex(x => x.FechaVencimiento);

        builder.HasOne(x => x.Trata)
            .WithOne(x => x.CacheControl)
            .HasForeignKey<TrataCacheControl>(x => x.TrataId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
