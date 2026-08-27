using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregaNovedadesPorColeccionExpediente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FechaUltimaNovedadAdjuntos",
                table: "Cache_ExpedienteControl",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FechaUltimaNovedadCabecera",
                table: "Cache_ExpedienteControl",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FechaUltimaNovedadDocumentos",
                table: "Cache_ExpedienteControl",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FechaUltimaNovedadMovimientos",
                table: "Cache_ExpedienteControl",
                type: "datetimeoffset",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FechaUltimaNovedadAdjuntos",
                table: "Cache_ExpedienteControl");

            migrationBuilder.DropColumn(
                name: "FechaUltimaNovedadCabecera",
                table: "Cache_ExpedienteControl");

            migrationBuilder.DropColumn(
                name: "FechaUltimaNovedadDocumentos",
                table: "Cache_ExpedienteControl");

            migrationBuilder.DropColumn(
                name: "FechaUltimaNovedadMovimientos",
                table: "Cache_ExpedienteControl");
        }
    }
}
