using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Expedientes.Models;

public sealed record DescubrirExpedientesPorTrataRequest(
    string CodigoTrata,
    OrigenInvocacionGdeba OrigenInvocacion = OrigenInvocacionGdeba.Administrativo);
