namespace SGR.Api.Exceptions;

/// <summary>
/// ExceÃ§Ã£o para recursos nÃ£o encontrados
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }

    public NotFoundException(string resourceName, object key) 
        : base($"Recurso '{resourceName}' com chave '{key}' nÃ£o foi encontrado.")
    {
    }
}

