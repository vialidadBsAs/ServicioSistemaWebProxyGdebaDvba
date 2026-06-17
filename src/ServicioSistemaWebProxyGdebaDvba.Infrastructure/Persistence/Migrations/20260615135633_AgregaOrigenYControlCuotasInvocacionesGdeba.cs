using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregaOrigenYControlCuotasInvocacionesGdeba : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InvocacionesGdeba_OperacionId_Ambiente_Fecha",
                table: "InvocacionesGdeba");

            migrationBuilder.AddColumn<decimal>(
                name: "UmbralAdvertenciaPorcentaje",
                table: "OperacionesGdeba",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 80m);

            migrationBuilder.AddColumn<decimal>(
                name: "UmbralCriticoPorcentaje",
                table: "OperacionesGdeba",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 90m);

            migrationBuilder.AddColumn<long>(
                name: "DuracionMilisegundos",
                table: "InvocacionesGdeba",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NumeroIntento",
                table: "InvocacionesGdeba",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "Origen",
                table: "InvocacionesGdeba",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "NoDeterminado");

            migrationBuilder.AddColumn<bool>(
                name: "ServidorRespondio",
                table: "InvocacionesGdeba",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "SolicitudId",
                table: "InvocacionesGdeba",
                type: "uniqueidentifier",
                nullable: false,
                defaultValueSql: "NEWID()");

            migrationBuilder.Sql(
                """
                UPDATE InvocacionesGdeba
                SET ServidorRespondio = CASE
                    WHEN EstadoHttp IS NULL THEN 0
                    ELSE 1
                END;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_InvocacionesGdeba_Ambiente_Fecha_OperacionId_Origen",
                table: "InvocacionesGdeba",
                columns: new[] { "Ambiente", "Fecha", "OperacionId", "Origen" });

            migrationBuilder.CreateIndex(
                name: "IX_InvocacionesGdeba_OperacionId",
                table: "InvocacionesGdeba",
                column: "OperacionId");

            migrationBuilder.CreateIndex(
                name: "IX_InvocacionesGdeba_SolicitudId_NumeroIntento",
                table: "InvocacionesGdeba",
                columns: new[] { "SolicitudId", "NumeroIntento" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InvocacionesGdeba_Ambiente_Fecha_OperacionId_Origen",
                table: "InvocacionesGdeba");

            migrationBuilder.DropIndex(
                name: "IX_InvocacionesGdeba_OperacionId",
                table: "InvocacionesGdeba");

            migrationBuilder.DropIndex(
                name: "IX_InvocacionesGdeba_SolicitudId_NumeroIntento",
                table: "InvocacionesGdeba");

            migrationBuilder.DropColumn(
                name: "UmbralAdvertenciaPorcentaje",
                table: "OperacionesGdeba");

            migrationBuilder.DropColumn(
                name: "UmbralCriticoPorcentaje",
                table: "OperacionesGdeba");

            migrationBuilder.DropColumn(
                name: "DuracionMilisegundos",
                table: "InvocacionesGdeba");

            migrationBuilder.DropColumn(
                name: "NumeroIntento",
                table: "InvocacionesGdeba");

            migrationBuilder.DropColumn(
                name: "Origen",
                table: "InvocacionesGdeba");

            migrationBuilder.DropColumn(
                name: "ServidorRespondio",
                table: "InvocacionesGdeba");

            migrationBuilder.DropColumn(
                name: "SolicitudId",
                table: "InvocacionesGdeba");

            migrationBuilder.CreateIndex(
                name: "IX_InvocacionesGdeba_OperacionId_Ambiente_Fecha",
                table: "InvocacionesGdeba",
                columns: new[] { "OperacionId", "Ambiente", "Fecha" });
        }
    }
}
