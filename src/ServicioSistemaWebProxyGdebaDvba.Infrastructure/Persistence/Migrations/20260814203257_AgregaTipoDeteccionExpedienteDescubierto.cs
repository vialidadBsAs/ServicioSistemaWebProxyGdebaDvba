using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregaTipoDeteccionExpedienteDescubierto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TipoDeteccion",
                table: "WorkerDescubrimiento_ExpedientesDescubiertosPorEjecucionTrataEstado",
                type: "int",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TipoDeteccion",
                table: "WorkerDescubrimiento_ExpedientesDescubiertosPorEjecucionTrataEstado");
        }
    }
}
