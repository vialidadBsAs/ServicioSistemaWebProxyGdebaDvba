using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence;

#nullable disable

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ProxyGdebaDbContext))]
[Migration("20260731170000_CorrigeOrdenCronologicoMovimientos")]
public partial class CorrigeOrdenCronologicoMovimientos : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE dbo.MovimientosExpediente
            SET Orden = -Orden;

            WITH MovimientosOrdenados AS
            (
                SELECT Id,
                    ROW_NUMBER() OVER
                    (
                        PARTITION BY ExpedienteId
                        ORDER BY FechaOperacion ASC, Id ASC
                    ) AS OrdenCronologico
                FROM dbo.MovimientosExpediente
            )
            UPDATE movimiento
            SET Orden = ordenado.OrdenCronologico
            FROM dbo.MovimientosExpediente AS movimiento
            INNER JOIN MovimientosOrdenados AS ordenado ON ordenado.Id = movimiento.Id;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE dbo.MovimientosExpediente
            SET Orden = -Orden;

            WITH MovimientosOrdenados AS
            (
                SELECT Id,
                    ROW_NUMBER() OVER
                    (
                        PARTITION BY ExpedienteId
                        ORDER BY FechaOperacion DESC, Id DESC
                    ) AS OrdenRespuesta
                FROM dbo.MovimientosExpediente
            )
            UPDATE movimiento
            SET Orden = ordenado.OrdenRespuesta
            FROM dbo.MovimientosExpediente AS movimiento
            INNER JOIN MovimientosOrdenados AS ordenado ON ordenado.Id = movimiento.Id;
            """);
    }
}
