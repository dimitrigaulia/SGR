namespace SGR.Api.Exceptions;

/// <summary>
/// Exceção para erros de regra de negócio
/// </summary>
public class BusinessException : Exception
{
    public BusinessException(string message) : base(message)
    {
    }

    public BusinessException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

