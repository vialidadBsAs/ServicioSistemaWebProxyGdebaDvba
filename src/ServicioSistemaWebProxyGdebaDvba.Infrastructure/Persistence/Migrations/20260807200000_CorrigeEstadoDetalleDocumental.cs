using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence;

#nullable disable

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ProxyGdebaDbContext))]
[Migration("20260807200000_CorrigeEstadoDetalleDocumental")]
public partial class CorrigeEstadoDetalleDocumental : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE documento
            SET MetadataCompleta = 1
            FROM dbo.DocumentosGdeba AS documento
            WHERE documento.MetadataCompleta = 0
                AND (
                    documento.FechaUltimoEnriquecimiento IS NOT NULL
                    OR documento.UrlArchivo IS NOT NULL
                    OR EXISTS
                    (
                        SELECT 1
                        FROM dbo.HistorialDocumentosGdeba AS historial
                        WHERE historial.DocumentoId = documento.Id
                    )
                );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
