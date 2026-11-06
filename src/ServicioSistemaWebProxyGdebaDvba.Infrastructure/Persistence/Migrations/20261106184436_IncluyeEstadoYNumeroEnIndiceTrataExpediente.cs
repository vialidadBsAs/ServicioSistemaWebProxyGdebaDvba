using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class IncluyeEstadoYNumeroEnIndiceTrataExpediente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Expedientes_TrataId",
                table: "Expedientes");

            migrationBuilder.CreateIndex(
                name: "IX_Expedientes_TrataId",
                table: "Expedientes",
                column: "TrataId")
                .Annotation("SqlServer:Include", new[] { "EstadoActual", "GdebaNumeroCompleto" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Expedientes_TrataId",
                table: "Expedientes");

            migrationBuilder.CreateIndex(
                name: "IX_Expedientes_TrataId",
                table: "Expedientes",
                column: "TrataId");
        }
    }
}
