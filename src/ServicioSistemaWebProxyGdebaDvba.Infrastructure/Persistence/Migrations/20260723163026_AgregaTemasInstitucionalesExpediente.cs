using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregaTemasInstitucionalesExpediente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TemasExpediente",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemasExpediente", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TemaExpedienteTratas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemaExpedienteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CodigoTrata = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemaExpedienteTratas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemaExpedienteTratas_TemasExpediente_TemaExpedienteId",
                        column: x => x.TemaExpedienteId,
                        principalTable: "TemasExpediente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TemaExpedienteTratas_CodigoTrata",
                table: "TemaExpedienteTratas",
                column: "CodigoTrata");

            migrationBuilder.CreateIndex(
                name: "IX_TemaExpedienteTratas_TemaExpedienteId_CodigoTrata",
                table: "TemaExpedienteTratas",
                columns: new[] { "TemaExpedienteId", "CodigoTrata" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TemasExpediente_Codigo",
                table: "TemasExpediente",
                column: "Codigo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TemaExpedienteTratas");

            migrationBuilder.DropTable(
                name: "TemasExpediente");
        }
    }
}
