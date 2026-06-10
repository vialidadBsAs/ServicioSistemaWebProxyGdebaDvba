using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MovimientoIdentidadPorFecha : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MovimientosExpediente_ExpedienteId_Orden",
                table: "MovimientosExpediente");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosExpediente_ExpedienteId_Orden",
                table: "MovimientosExpediente",
                columns: new[] { "ExpedienteId", "Orden" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MovimientosExpediente_ExpedienteId_Orden",
                table: "MovimientosExpediente");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosExpediente_ExpedienteId_Orden",
                table: "MovimientosExpediente",
                columns: new[] { "ExpedienteId", "Orden" },
                unique: true);
        }
    }
}
