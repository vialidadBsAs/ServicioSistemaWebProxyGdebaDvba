using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class IncluyeFechaOperacionEnIndiceUltimoPase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MovimientosExpediente_ExpedienteId_EsUltimoConocido",
                table: "MovimientosExpediente");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosExpediente_ExpedienteId_EsUltimoConocido",
                table: "MovimientosExpediente",
                columns: new[] { "ExpedienteId", "EsUltimoConocido" })
                .Annotation("SqlServer:Include", new[] { "FechaOperacion" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MovimientosExpediente_ExpedienteId_EsUltimoConocido",
                table: "MovimientosExpediente");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosExpediente_ExpedienteId_EsUltimoConocido",
                table: "MovimientosExpediente",
                columns: new[] { "ExpedienteId", "EsUltimoConocido" });
        }
    }
}
