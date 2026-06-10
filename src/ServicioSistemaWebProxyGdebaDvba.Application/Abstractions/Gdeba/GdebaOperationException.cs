namespace ServicioSistemaWebProxyGdebaDvba.Application.Abstractions.Gdeba;

public sealed class GdebaOperationException : Exception
{
    public GdebaOperationException(
        string operation,
        string message,
        int? statusCode = null,
        string? soapFaultCode = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Operation = operation;
        StatusCode = statusCode;
        SoapFaultCode = soapFaultCode;
    }

    public string Operation { get; }

    public int? StatusCode { get; }

    public string? SoapFaultCode { get; }
}
