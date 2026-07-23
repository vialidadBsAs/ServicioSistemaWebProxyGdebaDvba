using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CargaTemaObrasYTratas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var temaObrasId = new Guid("30000000-0000-0000-0000-000000000001");

            migrationBuilder.InsertData(
                table: "TemasExpediente",
                columns: new[] { "Id", "Codigo", "Nombre", "Descripcion" },
                values: new object[] { temaObrasId, "OBRAS", "Obras", "Tratas relacionadas con obras viales." });

            migrationBuilder.InsertData(
                table: "TemaExpedienteTratas",
                columns: new[] { "Id", "TemaExpedienteId", "CodigoTrata" },
                values: new object[,]
                {
                    { new Guid("31000000-0000-0000-0000-000000000001"), temaObrasId, "AUT0016" },
                    { new Guid("31000000-0000-0000-0000-000000000002"), temaObrasId, "CERT0024" },
                    { new Guid("31000000-0000-0000-0000-000000000003"), temaObrasId, "COMP0003" },
                    { new Guid("31000000-0000-0000-0000-000000000004"), temaObrasId, "COMP0014" },
                    { new Guid("31000000-0000-0000-0000-000000000005"), temaObrasId, "COMP0020" },
                    { new Guid("31000000-0000-0000-0000-000000000006"), temaObrasId, "COMP0027" },
                    { new Guid("31000000-0000-0000-0000-000000000007"), temaObrasId, "COMP0028" },
                    { new Guid("31000000-0000-0000-0000-000000000008"), temaObrasId, "COMP0033" },
                    { new Guid("31000000-0000-0000-0000-000000000009"), temaObrasId, "COMP0035" },
                    { new Guid("31000000-0000-0000-0000-000000000010"), temaObrasId, "COMP0042" },
                    { new Guid("31000000-0000-0000-0000-000000000011"), temaObrasId, "COMP0047" },
                    { new Guid("31000000-0000-0000-0000-000000000012"), temaObrasId, "COMP0049" },
                    { new Guid("31000000-0000-0000-0000-000000000013"), temaObrasId, "COMP0052" },
                    { new Guid("31000000-0000-0000-0000-000000000014"), temaObrasId, "COMP0053" },
                    { new Guid("31000000-0000-0000-0000-000000000015"), temaObrasId, "COMP0055" },
                    { new Guid("31000000-0000-0000-0000-000000000016"), temaObrasId, "COMP0069" },
                    { new Guid("31000000-0000-0000-0000-000000000017"), temaObrasId, "COMP0070" },
                    { new Guid("31000000-0000-0000-0000-000000000018"), temaObrasId, "COMP0071" },
                    { new Guid("31000000-0000-0000-0000-000000000019"), temaObrasId, "COMP0077" },
                    { new Guid("31000000-0000-0000-0000-000000000020"), temaObrasId, "COMP0078" },
                    { new Guid("31000000-0000-0000-0000-000000000021"), temaObrasId, "COMP0081" },
                    { new Guid("31000000-0000-0000-0000-000000000022"), temaObrasId, "COMP0082" },
                    { new Guid("31000000-0000-0000-0000-000000000023"), temaObrasId, "CONT0002" },
                    { new Guid("31000000-0000-0000-0000-000000000024"), temaObrasId, "FIN0000" },
                    { new Guid("31000000-0000-0000-0000-000000000025"), temaObrasId, "OTR0313" },
                    { new Guid("31000000-0000-0000-0000-000000000026"), temaObrasId, "OTR0452" },
                    { new Guid("31000000-0000-0000-0000-000000000027"), temaObrasId, "OTR0741" },
                    { new Guid("31000000-0000-0000-0000-000000000028"), temaObrasId, "PER0306" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "TemasExpediente",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000001"));
        }
    }
}
