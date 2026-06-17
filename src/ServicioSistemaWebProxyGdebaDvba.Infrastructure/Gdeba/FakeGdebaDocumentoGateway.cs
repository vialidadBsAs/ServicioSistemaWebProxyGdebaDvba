using ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Gdeba;
using ServicioSistemaWebProxyGdebaDvba.Application.Documentos.Models;
using ServicioSistemaWebProxyGdebaDvba.Application.Transversales.ControlCuotas.Models;

namespace ServicioSistemaWebProxyGdebaDvba.Infrastructure.Gdeba;

public sealed class FakeGdebaDocumentoGateway : IGdebaDocumentoGateway
{
    public Task<GdebaDocumentoDetalleDto?> BuscarDetallePorNumeroAsync(string numeroDocumento, ContextoInvocacionGdeba contextoInvocacion, CancellationToken cancellationToken)
    {
        var detalle = new GdebaDocumentoDetalleDto(
            numeroDocumento,
            NumeroEspecial: "RESO-2023-1743-GDEBA-DVMIYSPGP",
            TipoDocumentoCodigo: "RESO",
            TipoDocumentoNombre: "Resolucion",
            TipoDocumentoDescripcion: "Resolucion administrativa",
            Referencia: "Documento enriquecido en segundo plano.",
            FechaCreacion: new DateTimeOffset(2023, 8, 7, 15, 26, 25, TimeSpan.FromHours(-3)),
            ListaFirmantes: "RCOLABIANCHI;VVERA",
            UrlArchivo: "https://gedo.fake.gba.gob.ar/documentos/RESO-2023-1743.pdf",
            PuedeVerDocumento: true,
            Historial: new[]
            {
                new GdebaHistorialDocumentoDto(
                    1001,
                    "Generacion",
                    new DateTimeOffset(2023, 8, 7, 15, 20, 0, TimeSpan.FromHours(-3)),
                    new DateTimeOffset(2023, 8, 7, 15, 22, 0, TimeSpan.FromHours(-3)),
                    "RCOLABIANCHI",
                    "Roberto Colabianchi",
                    "GEDO"),
                new GdebaHistorialDocumentoDto(
                    1002,
                    "Firma",
                    new DateTimeOffset(2023, 8, 7, 15, 23, 0, TimeSpan.FromHours(-3)),
                    new DateTimeOffset(2023, 8, 7, 15, 26, 0, TimeSpan.FromHours(-3)),
                    "VVERA",
                    "Valeria Vera",
                    "PORTAFIRMAS")
            });

        return Task.FromResult<GdebaDocumentoDetalleDto?>(detalle);
    }
}
