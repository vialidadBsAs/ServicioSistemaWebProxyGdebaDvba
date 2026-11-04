using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence;

#nullable disable

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Siembra el catalogo local <c>TiposDocumentoGdeba</c> con los tipos documentales del arbol
    /// "Iniciar Produccion de Documento" de GEDO (docs/gedo-tipos-documento.tsv). Las sentencias SQL
    /// viven en la parte generada <c>CargaCatalogoTiposDocumentoGedo.Datos.cs</c>; se regeneran con
    /// docs/gedo-tipos-documento.migracion.js. Los tipos sembrados nacen sin sigla
    /// (<c>CodigoTipoDocumentoGdeba</c>), que se completa despues con consultarTipoDocumento.
    /// </summary>
    [DbContext(typeof(ProxyGdebaDbContext))]
    [Migration("20261104120000_CargaCatalogoTiposDocumentoGedo")]
    public partial class CargaCatalogoTiposDocumentoGedo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (string sentenciaCarga in CargaCatalogoTiposDocumentoGedo.SentenciasCarga)
            {
                migrationBuilder.Sql(sentenciaCarga);
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(CargaCatalogoTiposDocumentoGedo.SentenciaReversion);
        }
    }
}
