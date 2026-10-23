using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregaCatalogoReparticionesYSectoresGdeba : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReparticionesGdeba",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReparticionesGdeba", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SectoresGdeba",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CodigoReparticion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SectoresGdeba", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReparticionesGdeba_Codigo",
                table: "ReparticionesGdeba",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SectoresGdeba_CodigoReparticion_Codigo",
                table: "SectoresGdeba",
                columns: new[] { "CodigoReparticion", "Codigo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReparticionesGdeba");

            migrationBuilder.DropTable(
                name: "SectoresGdeba");
        }
    }
}
