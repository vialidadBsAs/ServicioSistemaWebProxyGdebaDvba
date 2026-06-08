using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AplicacionesConsumidoras",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Activa = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AplicacionesConsumidoras", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentosGdeba",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NumeroActuacionCompleto = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ActuacionTipoCodigo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ActuacionAnio = table.Column<int>(type: "int", nullable: false),
                    ActuacionNumero = table.Column<long>(type: "bigint", nullable: false),
                    ActuacionSistema = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ActuacionReparticion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NumeroEspecialCompleto = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EspecialTipoCodigo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    EspecialAnio = table.Column<int>(type: "int", nullable: true),
                    EspecialNumero = table.Column<long>(type: "bigint", nullable: true),
                    EspecialSistema = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EspecialReparticion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TipoDocumentoCodigo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TipoDocumentoNombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TipoDocumentoDescripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Referencia = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    MetadataCompleta = table.Column<bool>(type: "bit", nullable: false),
                    FechaUltimoEnriquecimiento = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UrlArchivo = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PuedeVerDocumento = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentosGdeba", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TiposDocumentoGdeba",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CodigoTipoDocumentoGdeba = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Familia = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TipoProduccion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EsAutomatica = table.Column<bool>(type: "bit", nullable: true),
                    EsComunicable = table.Column<bool>(type: "bit", nullable: true),
                    EsConfidencial = table.Column<bool>(type: "bit", nullable: true),
                    EsEmbebido = table.Column<bool>(type: "bit", nullable: true),
                    EsEspecial = table.Column<bool>(type: "bit", nullable: true),
                    EsFirmaConjunta = table.Column<bool>(type: "bit", nullable: true),
                    EsFirmaExterna = table.Column<bool>(type: "bit", nullable: true),
                    EsManual = table.Column<bool>(type: "bit", nullable: true),
                    EsNotificable = table.Column<bool>(type: "bit", nullable: true),
                    TieneTemplate = table.Column<bool>(type: "bit", nullable: true),
                    TieneToken = table.Column<bool>(type: "bit", nullable: true),
                    EsResolucion = table.Column<bool>(type: "bit", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiposDocumentoGdeba", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TratasGdeba",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AcronimoGedo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EsAutomatica = table.Column<bool>(type: "bit", nullable: true),
                    EsTrataManual = table.Column<bool>(type: "bit", nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IdTrataGdeba = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TipoReservaDescripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TipoReservaId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TipoReservaDescripcionTipoReserva = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TratasGdeba", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RegistrosAuditoria",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AplicacionConsumidoraId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Operacion = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Recurso = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Ambiente = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Fuente = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Exitoso = table.Column<bool>(type: "bit", nullable: false),
                    Fecha = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrosAuditoria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegistrosAuditoria_AplicacionesConsumidoras_AplicacionConsumidoraId",
                        column: x => x.AplicacionConsumidoraId,
                        principalTable: "AplicacionesConsumidoras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DocumentoArchivosLocales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StorageProvider = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    StorageKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RutaRelativa = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ContentType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ExtensionArchivo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    HashContenido = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    LongitudBytes = table.Column<long>(type: "bigint", nullable: true),
                    FechaDescarga = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FechaUltimaVerificacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentoArchivosLocales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentoArchivosLocales_DocumentosGdeba_DocumentoId",
                        column: x => x.DocumentoId,
                        principalTable: "DocumentosGdeba",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DocumentoCacheControls",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FechaPrimeraDeteccion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FechaUltimaConsultaGdeba = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FechaUltimaActualizacionLocal = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FechaVencimiento = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FuenteUltimaRespuesta = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EstaCompleto = table.Column<bool>(type: "bit", nullable: false),
                    TieneDatosParciales = table.Column<bool>(type: "bit", nullable: false),
                    UltimoErrorConsulta = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentoCacheControls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentoCacheControls_DocumentosGdeba_DocumentoId",
                        column: x => x.DocumentoId,
                        principalTable: "DocumentosGdeba",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Expedientes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GdebaNumeroCompleto = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    GdebaTipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    GdebaAnio = table.Column<int>(type: "int", nullable: false),
                    GdebaNumero = table.Column<long>(type: "bigint", nullable: false),
                    GdebaSistema = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    GdebaReparticion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TrataId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EstadoActual = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SistemaOrigen = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DescripcionTramite = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FechaCaratulacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UsuarioCaratulador = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    UsuarioDestino = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    SectorDestino = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ReparticionActual = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Expedientes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Expedientes_TratasGdeba_TrataId",
                        column: x => x.TrataId,
                        principalTable: "TratasGdeba",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TrataCacheControls",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TrataId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FechaPrimeraDeteccion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FechaUltimaConsultaGdeba = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FechaUltimaActualizacionLocal = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FechaVencimiento = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FuenteUltimaRespuesta = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EstaCompleta = table.Column<bool>(type: "bit", nullable: false),
                    UltimoErrorConsulta = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrataCacheControls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrataCacheControls_TratasGdeba_TrataId",
                        column: x => x.TrataId,
                        principalTable: "TratasGdeba",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ArchivosAdjuntosExpediente",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpedienteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NombreArchivo = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    FuenteDeteccion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FechaPrimeraDeteccion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FechaUltimaDeteccion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArchivosAdjuntosExpediente", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArchivosAdjuntosExpediente_Expedientes_ExpedienteId",
                        column: x => x.ExpedienteId,
                        principalTable: "Expedientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExpedienteCacheControls",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpedienteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FechaPrimeraDeteccion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FechaUltimaConsultaGdeba = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FechaUltimaActualizacionLocal = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FechaVencimiento = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FuenteUltimaRespuesta = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EstaCompleto = table.Column<bool>(type: "bit", nullable: false),
                    TieneDatosParciales = table.Column<bool>(type: "bit", nullable: false),
                    UltimoErrorConsulta = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpedienteCacheControls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExpedienteCacheControls_Expedientes_ExpedienteId",
                        column: x => x.ExpedienteId,
                        principalTable: "Expedientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExpedienteDocumentos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpedienteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FechaVinculacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    OrdenRespuesta = table.Column<int>(type: "int", nullable: true),
                    UsuarioAsociacion = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    UsuarioGenerador = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    FuenteDeteccion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FechaPrimeraDeteccion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FechaUltimaDeteccion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpedienteDocumentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExpedienteDocumentos_DocumentosGdeba_DocumentoId",
                        column: x => x.DocumentoId,
                        principalTable: "DocumentosGdeba",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExpedienteDocumentos_Expedientes_ExpedienteId",
                        column: x => x.ExpedienteId,
                        principalTable: "Expedientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExpedienteRelaciones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpedienteOrigenId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpedienteRelacionadoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NumeroExpedienteRelacionado = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TipoRelacion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CodigoTrataRelacionado = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DescripcionTrataRelacionado = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FechaRelacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UsuarioRelacion = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    EsCabecera = table.Column<bool>(type: "bit", nullable: true),
                    FuenteDeteccion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FechaPrimeraDeteccion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FechaUltimaDeteccion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpedienteRelaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExpedienteRelaciones_Expedientes_ExpedienteOrigenId",
                        column: x => x.ExpedienteOrigenId,
                        principalTable: "Expedientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExpedienteRelaciones_Expedientes_ExpedienteRelacionadoId",
                        column: x => x.ExpedienteRelacionadoId,
                        principalTable: "Expedientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MovimientosExpediente",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpedienteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Orden = table.Column<int>(type: "int", nullable: false),
                    FechaOperacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EstadoOrigen = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EstadoDestino = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UsuarioOrigen = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    UsuarioDestino = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Motivo = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ReparticionOrigen = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ReparticionDestino = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EsUltimoConocido = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovimientosExpediente", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MovimientosExpediente_Expedientes_ExpedienteId",
                        column: x => x.ExpedienteId,
                        principalTable: "Expedientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HistorialExpedienteCacheControls",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpedienteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UltimoMovimientoDetectadoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FechaPrimeraDeteccion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FechaUltimaConsultaGdeba = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FechaUltimaActualizacionLocal = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FechaVencimiento = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FuenteUltimaRespuesta = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EstaCompleto = table.Column<bool>(type: "bit", nullable: false),
                    TieneDatosParciales = table.Column<bool>(type: "bit", nullable: false),
                    UltimoErrorConsulta = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistorialExpedienteCacheControls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistorialExpedienteCacheControls_Expedientes_ExpedienteId",
                        column: x => x.ExpedienteId,
                        principalTable: "Expedientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HistorialExpedienteCacheControls_MovimientosExpediente_UltimoMovimientoDetectadoId",
                        column: x => x.UltimoMovimientoDetectadoId,
                        principalTable: "MovimientosExpediente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AplicacionesConsumidoras_Codigo",
                table: "AplicacionesConsumidoras",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ArchivosAdjuntosExpediente_ExpedienteId_NombreArchivo",
                table: "ArchivosAdjuntosExpediente",
                columns: new[] { "ExpedienteId", "NombreArchivo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentoArchivosLocales_DocumentoId",
                table: "DocumentoArchivosLocales",
                column: "DocumentoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentoArchivosLocales_StorageKey",
                table: "DocumentoArchivosLocales",
                column: "StorageKey");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentoCacheControls_DocumentoId",
                table: "DocumentoCacheControls",
                column: "DocumentoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentoCacheControls_FechaVencimiento",
                table: "DocumentoCacheControls",
                column: "FechaVencimiento");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosGdeba_ActuacionTipoCodigo_ActuacionAnio_ActuacionNumero_ActuacionSistema_ActuacionReparticion",
                table: "DocumentosGdeba",
                columns: new[] { "ActuacionTipoCodigo", "ActuacionAnio", "ActuacionNumero", "ActuacionSistema", "ActuacionReparticion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosGdeba_NumeroActuacionCompleto",
                table: "DocumentosGdeba",
                column: "NumeroActuacionCompleto",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosGdeba_NumeroEspecialCompleto",
                table: "DocumentosGdeba",
                column: "NumeroEspecialCompleto",
                unique: true,
                filter: "[NumeroEspecialCompleto] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosGdeba_TipoDocumentoCodigo_ActuacionReparticion",
                table: "DocumentosGdeba",
                columns: new[] { "TipoDocumentoCodigo", "ActuacionReparticion" });

            migrationBuilder.CreateIndex(
                name: "IX_ExpedienteCacheControls_ExpedienteId",
                table: "ExpedienteCacheControls",
                column: "ExpedienteId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExpedienteCacheControls_FechaVencimiento",
                table: "ExpedienteCacheControls",
                column: "FechaVencimiento");

            migrationBuilder.CreateIndex(
                name: "IX_ExpedienteDocumentos_DocumentoId",
                table: "ExpedienteDocumentos",
                column: "DocumentoId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpedienteDocumentos_ExpedienteId_DocumentoId",
                table: "ExpedienteDocumentos",
                columns: new[] { "ExpedienteId", "DocumentoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExpedienteRelaciones_ExpedienteOrigenId_TipoRelacion_NumeroExpedienteRelacionado",
                table: "ExpedienteRelaciones",
                columns: new[] { "ExpedienteOrigenId", "TipoRelacion", "NumeroExpedienteRelacionado" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExpedienteRelaciones_ExpedienteRelacionadoId",
                table: "ExpedienteRelaciones",
                column: "ExpedienteRelacionadoId");

            migrationBuilder.CreateIndex(
                name: "IX_Expedientes_GdebaAnio_GdebaReparticion",
                table: "Expedientes",
                columns: new[] { "GdebaAnio", "GdebaReparticion" });

            migrationBuilder.CreateIndex(
                name: "IX_Expedientes_GdebaNumeroCompleto",
                table: "Expedientes",
                column: "GdebaNumeroCompleto",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Expedientes_GdebaTipo_GdebaAnio_GdebaNumero_GdebaSistema_GdebaReparticion",
                table: "Expedientes",
                columns: new[] { "GdebaTipo", "GdebaAnio", "GdebaNumero", "GdebaSistema", "GdebaReparticion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Expedientes_TrataId",
                table: "Expedientes",
                column: "TrataId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialExpedienteCacheControls_ExpedienteId",
                table: "HistorialExpedienteCacheControls",
                column: "ExpedienteId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HistorialExpedienteCacheControls_FechaVencimiento",
                table: "HistorialExpedienteCacheControls",
                column: "FechaVencimiento");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialExpedienteCacheControls_UltimoMovimientoDetectadoId",
                table: "HistorialExpedienteCacheControls",
                column: "UltimoMovimientoDetectadoId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosExpediente_ExpedienteId_EsUltimoConocido",
                table: "MovimientosExpediente",
                columns: new[] { "ExpedienteId", "EsUltimoConocido" });

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosExpediente_ExpedienteId_Orden",
                table: "MovimientosExpediente",
                columns: new[] { "ExpedienteId", "Orden" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosAuditoria_AplicacionConsumidoraId_Operacion_Fecha",
                table: "RegistrosAuditoria",
                columns: new[] { "AplicacionConsumidoraId", "Operacion", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosAuditoria_Fecha",
                table: "RegistrosAuditoria",
                column: "Fecha");

            migrationBuilder.CreateIndex(
                name: "IX_TiposDocumentoGdeba_Codigo",
                table: "TiposDocumentoGdeba",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TiposDocumentoGdeba_CodigoTipoDocumentoGdeba",
                table: "TiposDocumentoGdeba",
                column: "CodigoTipoDocumentoGdeba");

            migrationBuilder.CreateIndex(
                name: "IX_TiposDocumentoGdeba_EsResolucion_Activo",
                table: "TiposDocumentoGdeba",
                columns: new[] { "EsResolucion", "Activo" });

            migrationBuilder.CreateIndex(
                name: "IX_TiposDocumentoGdeba_Familia_Activo",
                table: "TiposDocumentoGdeba",
                columns: new[] { "Familia", "Activo" });

            migrationBuilder.CreateIndex(
                name: "IX_TrataCacheControls_FechaVencimiento",
                table: "TrataCacheControls",
                column: "FechaVencimiento");

            migrationBuilder.CreateIndex(
                name: "IX_TrataCacheControls_TrataId",
                table: "TrataCacheControls",
                column: "TrataId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TratasGdeba_Codigo",
                table: "TratasGdeba",
                column: "Codigo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArchivosAdjuntosExpediente");

            migrationBuilder.DropTable(
                name: "DocumentoArchivosLocales");

            migrationBuilder.DropTable(
                name: "DocumentoCacheControls");

            migrationBuilder.DropTable(
                name: "ExpedienteCacheControls");

            migrationBuilder.DropTable(
                name: "ExpedienteDocumentos");

            migrationBuilder.DropTable(
                name: "ExpedienteRelaciones");

            migrationBuilder.DropTable(
                name: "HistorialExpedienteCacheControls");

            migrationBuilder.DropTable(
                name: "RegistrosAuditoria");

            migrationBuilder.DropTable(
                name: "TiposDocumentoGdeba");

            migrationBuilder.DropTable(
                name: "TrataCacheControls");

            migrationBuilder.DropTable(
                name: "DocumentosGdeba");

            migrationBuilder.DropTable(
                name: "MovimientosExpediente");

            migrationBuilder.DropTable(
                name: "AplicacionesConsumidoras");

            migrationBuilder.DropTable(
                name: "Expedientes");

            migrationBuilder.DropTable(
                name: "TratasGdeba");
        }
    }
}
