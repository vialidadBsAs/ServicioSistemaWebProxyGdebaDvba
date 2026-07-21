using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregaEstadosExpedienteGdeba : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EstadosExpedienteGdeba",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NombreGdeba = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    HabilitadoParaDescubrimiento = table.Column<bool>(type: "bit", nullable: false),
                    PrioridadDescubrimiento = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstadosExpedienteGdeba", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "EstadosExpedienteGdeba",
                columns: new[] { "Id", "HabilitadoParaDescubrimiento", "NombreGdeba", "PrioridadDescubrimiento" },
                values: new object[,]
                {
                    { new Guid("20000000-0000-0000-0000-000000000001"), true, "Iniciación", 10 },
                    { new Guid("20000000-0000-0000-0000-000000000002"), true, "Tramitación", 20 },
                    { new Guid("20000000-0000-0000-0000-000000000003"), true, "Comunicación", 30 },
                    { new Guid("20000000-0000-0000-0000-000000000004"), true, "Guarda Temporal", 40 },
                    { new Guid("20000000-0000-0000-0000-000000000005"), true, "Ejecución", 50 },
                    { new Guid("20000000-0000-0000-0000-000000000006"), true, "Pendiente Iniciación", 60 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_EstadosExpedienteGdeba_HabilitadoParaDescubrimiento_PrioridadDescubrimiento",
                table: "EstadosExpedienteGdeba",
                columns: new[] { "HabilitadoParaDescubrimiento", "PrioridadDescubrimiento" });

            migrationBuilder.CreateIndex(
                name: "IX_EstadosExpedienteGdeba_NombreGdeba",
                table: "EstadosExpedienteGdeba",
                column: "NombreGdeba",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EstadosExpedienteGdeba");
        }
    }
}
