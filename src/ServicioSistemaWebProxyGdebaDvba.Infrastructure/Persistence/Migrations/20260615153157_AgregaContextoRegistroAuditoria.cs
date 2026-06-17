using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregaContextoRegistroAuditoria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Operacion",
                table: "RegistrosAuditoria",
                newName: "OperacionSolicitada");

            migrationBuilder.RenameIndex(
                name: "IX_RegistrosAuditoria_AplicacionConsumidoraId_Operacion_Fecha",
                table: "RegistrosAuditoria",
                newName: "IX_RegistrosAuditoria_AplicacionConsumidoraId_OperacionSolicitada_Fecha");

            migrationBuilder.AddColumn<string>(
                name: "Mensaje",
                table: "RegistrosAuditoria",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OperacionGdeba",
                table: "RegistrosAuditoria",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Mensaje",
                table: "RegistrosAuditoria");

            migrationBuilder.DropColumn(
                name: "OperacionGdeba",
                table: "RegistrosAuditoria");

            migrationBuilder.RenameColumn(
                name: "OperacionSolicitada",
                table: "RegistrosAuditoria",
                newName: "Operacion");

            migrationBuilder.RenameIndex(
                name: "IX_RegistrosAuditoria_AplicacionConsumidoraId_OperacionSolicitada_Fecha",
                table: "RegistrosAuditoria",
                newName: "IX_RegistrosAuditoria_AplicacionConsumidoraId_Operacion_Fecha");
        }
    }
}
