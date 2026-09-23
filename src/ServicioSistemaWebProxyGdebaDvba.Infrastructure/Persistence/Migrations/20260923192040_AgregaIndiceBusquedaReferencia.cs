using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregaIndiceBusquedaReferencia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_DocumentosGdeba_FechaCreacion",
                table: "DocumentosGdeba",
                column: "FechaCreacion")
                .Annotation("SqlServer:Include", new[] { "Referencia", "NumeroActuacionCompleto", "ActuacionTipoCodigo", "TipoDocumentoCodigo" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DocumentosGdeba_FechaCreacion",
                table: "DocumentosGdeba");
        }
    }
}
