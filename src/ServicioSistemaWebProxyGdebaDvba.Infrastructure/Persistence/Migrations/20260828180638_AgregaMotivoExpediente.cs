using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregaMotivoExpediente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DescripcionTramite",
                table: "Expedientes",
                newName: "DescripcionAdicional");

            migrationBuilder.AddColumn<string>(
                name: "Motivo",
                table: "Expedientes",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            // Backfill: el motivo de la operacion de caratulacion ("Iniciar Expediente") ya esta guardado en los movimientos consultados.
            migrationBuilder.Sql(@"
UPDATE e
SET Motivo = caratulacion.Motivo
FROM Expedientes e
CROSS APPLY (
    SELECT TOP 1 m.Motivo
    FROM MovimientosExpediente m
    WHERE m.ExpedienteId = e.Id
        AND m.EstadoDestino = 'Iniciar Expediente'
        AND m.Motivo IS NOT NULL
        AND LTRIM(RTRIM(m.Motivo)) <> ''
    ORDER BY m.Orden
) caratulacion;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Motivo",
                table: "Expedientes");

            migrationBuilder.RenameColumn(
                name: "DescripcionAdicional",
                table: "Expedientes",
                newName: "DescripcionTramite");
        }
    }
}
