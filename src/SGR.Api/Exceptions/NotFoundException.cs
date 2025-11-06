namespace SGR.Api.Exceptions;

/// <summary>
/// Exceção para recursos não encontrados
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }

    public NotFoundException(string resourceName, object key) 
        : base($"Recurso '{resourceName}' com chave '{key}' não foi encontrado.")
    {
    }
}

