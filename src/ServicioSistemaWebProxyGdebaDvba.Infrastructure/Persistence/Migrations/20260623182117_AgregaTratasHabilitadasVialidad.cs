using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregaTratasHabilitadasVialidad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TratasHabilitadasVialidad",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TrataId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CodigoTrata = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DescripcionTrata = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EstadoTrata = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ReservaTotal = table.Column<bool>(type: "bit", nullable: true),
                    CaratulaVariable = table.Column<bool>(type: "bit", nullable: true),
                    CodigoReparticion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NombreReparticion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CodigoOrganismo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NombreOrganismo = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    PermisoCaratulacion = table.Column<bool>(type: "bit", nullable: true),
                    PermisoReserva = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TratasHabilitadasVialidad", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TratasHabilitadasVialidad_TratasGdeba_TrataId",
                        column: x => x.TrataId,
                        principalTable: "TratasGdeba",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TratasHabilitadasVialidad_CodigoOrganismo",
                table: "TratasHabilitadasVialidad",
                column: "CodigoOrganismo");

            migrationBuilder.CreateIndex(
                name: "IX_TratasHabilitadasVialidad_CodigoReparticion",
                table: "TratasHabilitadasVialidad",
                column: "CodigoReparticion");

            migrationBuilder.CreateIndex(
                name: "IX_TratasHabilitadasVialidad_CodigoTrata",
                table: "TratasHabilitadasVialidad",
                column: "CodigoTrata");

            migrationBuilder.CreateIndex(
                name: "IX_TratasHabilitadasVialidad_CodigoTrata_CodigoOrganismo_CodigoReparticion",
                table: "TratasHabilitadasVialidad",
                columns: new[] { "CodigoTrata", "CodigoOrganismo", "CodigoReparticion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TratasHabilitadasVialidad_TrataId",
                table: "TratasHabilitadasVialidad",
                column: "TrataId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TratasHabilitadasVialidad");
        }
    }
}
