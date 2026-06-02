using ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Gdeba;
using ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Models;
using ServicioSistemaWebProxyGdebaDvba.Domain.ValueObjects;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Gdeba;

public sealed class FakeGdebaExpedienteGateway : IGdebaExpedienteGateway
{
    public Task<ExpedienteGdebaDto?> BuscarExpedienteAsync(NumeroGdebaCompleto numeroGdebaCompleto, CancellationToken cancellationToken)
    {
        var expediente = new ExpedienteGdebaDto(
            numeroGdebaCompleto.Valor,
            CodigoTrata: "FIN0057",
            DescripcionTrata: "Elevacion de Consultas",
            Estado: "Tramitacion");

        return Task.FromResult<ExpedienteGdebaDto?>(expediente);
    }

    public Task<GdebaExpedienteDetalladoDto?> ConsultarExpedienteDetalladoAsync(
        NumeroGdebaCompleto numeroGdebaCompleto,
        CancellationToken cancellationToken)
    {
        var expediente = new GdebaExpedienteDetalladoDto(
            numeroGdebaCompleto.Valor,
            CodigoTrata: "COMP0003",
            DescripcionTrata: "Licitacion de Obra",
            Estado: "Ejecucion",
            SistemaOrigen: "EE",
            DescripcionTramite: "Rehabilitacion y Conservacion de Rutas Provinciales.",
            FechaCaratulacion: new DateTimeOffset(2020, 7, 3, 18, 50, 43, TimeSpan.FromHours(-3)),
            UsuarioCaratulador: "RCOLABIANCHI",
            UsuarioDestino: "VVERA",
            Documentos: new[]
            {
                new GdebaDocumentoExpedienteDto(
                    "RS-2023-33144875-GDEBA-DVMIYSPGP",
                    TipoDocumentoCodigo: null,
                    Referencia: "EX-2021-31948536-GDEBA-DPTLMIYSPGP Redeterminacion definitiva de precios.",
                    FechaCreacion: new DateTimeOffset(2023, 8, 7, 15, 26, 25, TimeSpan.FromHours(-3)),
                    FechaVinculacion: new DateTimeOffset(2025, 5, 20, 13, 30, 33, TimeSpan.FromHours(-3)),
                    UsuarioAsociacion: null,
                    UsuarioGenerador: null),
                new GdebaDocumentoExpedienteDto(
                    "PV-2025-01492173-GDEBA-DVMIYSPGP",
                    TipoDocumentoCodigo: "PV",
                    Referencia: "Pase",
                    FechaCreacion: new DateTimeOffset(2025, 1, 14, 10, 4, 54, TimeSpan.FromHours(-3)),
                    FechaVinculacion: new DateTimeOffset(2025, 1, 14, 10, 4, 54, TimeSpan.FromHours(-3)),
                    UsuarioAsociacion: null,
                    UsuarioGenerador: null)
            },
            ArchivosAdjuntos: new[]
            {
                "EX-2020-14232989-GDEBA-DVMIYSPGP dba Aprueba pliego y autoriza llamado a lic. publica.docx"
            },
            Relaciones: new[]
            {
                new GdebaRelacionExpedienteDto(
                    "EX-2021-4231314-   -GDEBA-DVMIYSPGP",
                    "TramitacionConjunta",
                    EsCabecera: false),
                new GdebaRelacionExpedienteDto(
                    "EX-2020-22936237-   -GDEBA-DVMIYSPGP",
                    "Asociado",
                    EsCabecera: false)
            });

        return Task.FromResult<GdebaExpedienteDetalladoDto?>(expediente);
    }
}
