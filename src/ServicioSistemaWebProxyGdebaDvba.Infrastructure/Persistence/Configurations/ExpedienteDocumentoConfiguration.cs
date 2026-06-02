using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServicioSistemaWebProxyGdebaDvba.Domain.Entities;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Configurations;

public sealed class ExpedienteDocumentoConfiguration : IEntityTypeConfiguration<ExpedienteDocumento>
{
    public void Configure(EntityTypeBuilder<ExpedienteDocumento> builder)
    {
        builder.ToTable("ExpedienteDocumentos");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.ExpedienteId, x.DocumentoId })
            .IsUnique();
    }
}
