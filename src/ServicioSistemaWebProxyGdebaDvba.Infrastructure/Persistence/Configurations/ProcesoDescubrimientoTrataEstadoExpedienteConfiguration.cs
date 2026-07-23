using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities.Configuracion;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Configurations;

public sealed class ProcesoDescubrimientoTrataEstadoExpedienteConfiguration : IEntityTypeConfiguration<ProcesoDescubrimientoTrataEstadoExpediente>
{
    public void Configure(EntityTypeBuilder<ProcesoDescubrimientoTrataEstadoExpediente> builder)
    {
        builder.ToTable("WorkerDescubrimiento_ExpedientesSegunTratasEstados");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CodigoTrata).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => new { x.CodigoTrata, x.EstadoExpedienteGdebaId }).IsUnique();
        builder.HasIndex(x => x.OmitirHasta);
    }
}
