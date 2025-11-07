namespace SGR.Api.Services.Interfaces;

/// <summary>
/// Serviço para validação de CPF e CNPJ
/// </summary>
public interface ICpfCnpjValidationService
{
    /// <summary>
    /// Valida um CPF ou CNPJ usando a API BrasilApi
    /// </summary>
    /// <param name="cpfCnpj">CPF ou CNPJ (com ou sem máscara)</param>
    /// <returns>True se válido, False caso contrário</returns>
    Task<bool> ValidarAsync(string cpfCnpj);
}

