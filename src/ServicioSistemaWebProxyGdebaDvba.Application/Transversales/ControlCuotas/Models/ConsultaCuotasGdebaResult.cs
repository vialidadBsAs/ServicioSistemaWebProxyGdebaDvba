using ServicioSistemaWebProxyGdebaDvba.Domain.Enums;

namespace ServicioSistemaWebProxyGdebaDvba.Application.Transversales.ControlCuotas.Models;

public sealed record ConsultaCuotasGdebaResult(
    DateOnly Fecha,
    AmbienteGdeba Ambiente,
    IReadOnlyCollection<ConsumoCuotaOperacionGdebaDto> Operaciones,
    TotalesConsumoCuotasGdebaDto Totales,
    int TotalHistorico);

public sealed record ConsumoCuotaOperacionGdebaDto(
    string Servicio,
    string Operacion,
    int Interactiva,
    int RefrescoManual,
    int WorkerProgramado,
    int Mensajeria,
    int Administrativo,
    int NoDeterminado,
    int Total,
    int? LimiteDiario,
    decimal? PorcentajeConsumido,
    string Estado);

public sealed record TotalesConsumoCuotasGdebaDto(
    int Interactiva,
    int RefrescoManual,
    int WorkerProgramado,
    int Mensajeria,
    int Administrativo,
    int NoDeterminado,
    int Total,
    int? LimiteDiario,
    decimal? PorcentajeConsumido);
