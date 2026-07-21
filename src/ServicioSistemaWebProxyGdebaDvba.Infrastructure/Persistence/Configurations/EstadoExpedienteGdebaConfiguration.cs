using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Configurations;

public sealed class EstadoExpedienteGdebaConfiguration : IEntityTypeConfiguration<EstadoExpedienteGdeba>
{
    public void Configure(EntityTypeBuilder<EstadoExpedienteGdeba> builder)
    {
        builder.ToTable("EstadosExpedienteGdeba");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.NombreGdeba)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(x => x.NombreGdeba)
            .IsUnique();

        builder.HasIndex(x => new { x.HabilitadoParaDescubrimiento, x.PrioridadDescubrimiento });

        builder.HasData(
            new { Id = new Guid("20000000-0000-0000-0000-000000000001"), NombreGdeba = "Iniciación", HabilitadoParaDescubrimiento = true, PrioridadDescubrimiento = 10 },
            new { Id = new Guid("20000000-0000-0000-0000-000000000002"), NombreGdeba = "Tramitación", HabilitadoParaDescubrimiento = true, PrioridadDescubrimiento = 20 },
            new { Id = new Guid("20000000-0000-0000-0000-000000000003"), NombreGdeba = "Comunicación", HabilitadoParaDescubrimiento = true, PrioridadDescubrimiento = 30 },
            new { Id = new Guid("20000000-0000-0000-0000-000000000004"), NombreGdeba = "Guarda Temporal", HabilitadoParaDescubrimiento = true, PrioridadDescubrimiento = 40 },
            new { Id = new Guid("20000000-0000-0000-0000-000000000005"), NombreGdeba = "Ejecución", HabilitadoParaDescubrimiento = true, PrioridadDescubrimiento = 50 },
            new { Id = new Guid("20000000-0000-0000-0000-000000000006"), NombreGdeba = "Pendiente Iniciación", HabilitadoParaDescubrimiento = true, PrioridadDescubrimiento = 60 });
    }
}
