namespace SGR.Api.Exceptions;

/// <summary>
/// ExceÃ§Ã£o para erros de regra de negÃ³cio
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

