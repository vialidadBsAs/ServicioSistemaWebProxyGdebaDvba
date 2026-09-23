using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregaIndiceResumenTipoDocumental : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_DocumentosGdeba_ActuacionTipoCodigo",
                table: "DocumentosGdeba",
                column: "ActuacionTipoCodigo")
                .Annotation("SqlServer:Include", new[] { "MetadataCompleta" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DocumentosGdeba_ActuacionTipoCodigo",
                table: "DocumentosGdeba");
        }
    }
}
