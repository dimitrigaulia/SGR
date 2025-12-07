namespace SGR.Api.Services.Interfaces;

/// <summary>
/// ServiÃ§o para validaÃ§Ã£o de CPF e CNPJ
/// </summary>
public interface ICpfCnpjValidationService
{
    /// <summary>
    /// Valida um CPF ou CNPJ usando a API BrasilApi
    /// </summary>
    /// <param name="cpfCnpj">CPF ou CNPJ (com ou sem mÃ¡scara)</param>
    /// <returns>True se vÃ¡lido, False caso contrÃ¡rio</returns>
    Task<bool> ValidarAsync(string cpfCnpj);
}

