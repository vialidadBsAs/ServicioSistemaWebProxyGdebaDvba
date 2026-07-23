using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregaTratasAdicionalesTemaObras : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var temaObrasId = new Guid("30000000-0000-0000-0000-000000000001");

            migrationBuilder.InsertData(
                table: "TemaExpedienteTratas",
                columns: new[] { "Id", "TemaExpedienteId", "CodigoTrata" },
                values: new object[,]
                {
                    { new Guid("32000000-0000-0000-0000-000000000001"), temaObrasId, "OTR0033" },
                    { new Guid("32000000-0000-0000-0000-000000000002"), temaObrasId, "OTR0005" },
                    { new Guid("32000000-0000-0000-0000-000000000003"), temaObrasId, "COMP0098" },
                    { new Guid("32000000-0000-0000-0000-000000000004"), temaObrasId, "COMP0037" },
                    { new Guid("32000000-0000-0000-0000-000000000005"), temaObrasId, "COMP0012" },
                    { new Guid("32000000-0000-0000-0000-000000000006"), temaObrasId, "FIN0252" },
                    { new Guid("32000000-0000-0000-0000-000000000007"), temaObrasId, "CERT0025" },
                    { new Guid("32000000-0000-0000-0000-000000000008"), temaObrasId, "FIN0253" },
                    { new Guid("32000000-0000-0000-0000-000000000009"), temaObrasId, "COMP0063" },
                    { new Guid("32000000-0000-0000-0000-000000000010"), temaObrasId, "COMP0087" },
                    { new Guid("32000000-0000-0000-0000-000000000011"), temaObrasId, "COMP0007" },
                    { new Guid("32000000-0000-0000-0000-000000000012"), temaObrasId, "COMP0002" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "TemaExpedienteTratas", keyColumn: "Id", keyValue: new Guid("32000000-0000-0000-0000-000000000001"));
            migrationBuilder.DeleteData(table: "TemaExpedienteTratas", keyColumn: "Id", keyValue: new Guid("32000000-0000-0000-0000-000000000002"));
            migrationBuilder.DeleteData(table: "TemaExpedienteTratas", keyColumn: "Id", keyValue: new Guid("32000000-0000-0000-0000-000000000003"));
            migrationBuilder.DeleteData(table: "TemaExpedienteTratas", keyColumn: "Id", keyValue: new Guid("32000000-0000-0000-0000-000000000004"));
            migrationBuilder.DeleteData(table: "TemaExpedienteTratas", keyColumn: "Id", keyValue: new Guid("32000000-0000-0000-0000-000000000005"));
            migrationBuilder.DeleteData(table: "TemaExpedienteTratas", keyColumn: "Id", keyValue: new Guid("32000000-0000-0000-0000-000000000006"));
            migrationBuilder.DeleteData(table: "TemaExpedienteTratas", keyColumn: "Id", keyValue: new Guid("32000000-0000-0000-0000-000000000007"));
            migrationBuilder.DeleteData(table: "TemaExpedienteTratas", keyColumn: "Id", keyValue: new Guid("32000000-0000-0000-0000-000000000008"));
            migrationBuilder.DeleteData(table: "TemaExpedienteTratas", keyColumn: "Id", keyValue: new Guid("32000000-0000-0000-0000-000000000009"));
            migrationBuilder.DeleteData(table: "TemaExpedienteTratas", keyColumn: "Id", keyValue: new Guid("32000000-0000-0000-0000-000000000010"));
            migrationBuilder.DeleteData(table: "TemaExpedienteTratas", keyColumn: "Id", keyValue: new Guid("32000000-0000-0000-0000-000000000011"));
            migrationBuilder.DeleteData(table: "TemaExpedienteTratas", keyColumn: "Id", keyValue: new Guid("32000000-0000-0000-0000-000000000012"));
        }
    }
}
