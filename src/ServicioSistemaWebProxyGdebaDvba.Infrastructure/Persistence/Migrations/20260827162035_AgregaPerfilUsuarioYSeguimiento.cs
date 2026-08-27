using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregaPerfilUsuarioYSeguimiento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FechaUltimaNovedad",
                table: "Cache_ExpedienteControl",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Perfil_Usuarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioInstitucional = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UsuarioGdeba = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Perfil_Usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Perfil_SeguimientosExpediente",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PerfilUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpedienteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FechaAgregado = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FechaUltimaVista = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Perfil_SeguimientosExpediente", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Perfil_SeguimientosExpediente_Expedientes_ExpedienteId",
                        column: x => x.ExpedienteId,
                        principalTable: "Expedientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Perfil_SeguimientosExpediente_Perfil_Usuarios_PerfilUsuarioId",
                        column: x => x.PerfilUsuarioId,
                        principalTable: "Perfil_Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Perfil_SeguimientosExpediente_ExpedienteId",
                table: "Perfil_SeguimientosExpediente",
                column: "ExpedienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Perfil_SeguimientosExpediente_PerfilUsuarioId_ExpedienteId",
                table: "Perfil_SeguimientosExpediente",
                columns: new[] { "PerfilUsuarioId", "ExpedienteId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Perfil_Usuarios_UsuarioInstitucional",
                table: "Perfil_Usuarios",
                column: "UsuarioInstitucional",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Perfil_SeguimientosExpediente");

            migrationBuilder.DropTable(
                name: "Perfil_Usuarios");

            migrationBuilder.DropColumn(
                name: "FechaUltimaNovedad",
                table: "Cache_ExpedienteControl");
        }
    }
}
