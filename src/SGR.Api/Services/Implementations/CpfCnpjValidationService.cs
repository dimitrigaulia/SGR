using System.Text.RegularExpressions;
using SGR.Api.Services.Interfaces;

namespace SGR.Api.Services.Implementations;

/// <summary>
/// ImplementaÃ§Ã£o do serviÃ§o de validaÃ§Ã£o de CPF/CNPJ
/// CPF: validaÃ§Ã£o local (dÃ­gitos verificadores). CNPJ: validaÃ§Ã£o local + BrasilAPI
/// </summary>
public class CpfCnpjValidationService : ICpfCnpjValidationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CpfCnpjValidationService> _logger;

    public CpfCnpjValidationService(HttpClient httpClient, ILogger<CpfCnpjValidationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpClient.BaseAddress = new Uri("https://brasilapi.com.br/api/");
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
    }

    public async Task<bool> ValidarAsync(string cpfCnpj)
    {
        if (string.IsNullOrWhiteSpace(cpfCnpj))
            return false;

        // Remove caracteres nÃ£o numÃ©ricos
        var documento = Regex.Replace(cpfCnpj, @"[^\d]", "");

        // ValidaÃ§Ã£o bÃ¡sica de tamanho
        if (documento.Length != 11 && documento.Length != 14)
            return false;

        // ValidaÃ§Ã£o de dÃ­gitos verificadores (algoritmo local)
        if (!ValidarDigitosVerificadores(documento))
            return false;

        // CPF: validar apenas dÃ­gitos verificadores (BrasilAPI nÃ£o tem endpoint para CPF)
        if (documento.Length == 11)
        {
            _logger.LogInformation("CPF {Documento} validado com sucesso (validaÃ§Ã£o local)", documento);
            return true;
        }

        // CNPJ: validar dÃ­gitos verificadores E consultar BrasilAPI
        try
        {
            var response = await _httpClient.GetAsync($"cnpj/v1/{documento}");

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("CNPJ {Documento} validado com sucesso via BrasilApi", documento);
                return true;
            }

            _logger.LogWarning("CNPJ {Documento} invÃ¡lido segundo BrasilApi", documento);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao validar CNPJ {Documento} via BrasilApi. Usando validaÃ§Ã£o local apenas.", documento);
            // Em caso de erro na API, confiar apenas na validaÃ§Ã£o de dÃ­gitos verificadores
            return ValidarDigitosVerificadores(documento);
        }
    }

    /// <summary>
    /// Valida os dÃ­gitos verificadores de CPF ou CNPJ
    /// </summary>
    private static bool ValidarDigitosVerificadores(string documento)
    {
        if (documento.Length == 11)
            return ValidarCpf(documento);
        else if (documento.Length == 14)
            return ValidarCnpj(documento);
        
        return false;
    }

    /// <summary>
    /// Valida CPF
    /// </summary>
    private static bool ValidarCpf(string cpf)
    {
        // Verifica se todos os dÃ­gitos sÃ£o iguais
        if (cpf.Distinct().Count() == 1)
            return false;

        // Valida primeiro dÃ­gito verificador
        var soma = 0;
        for (int i = 0; i < 9; i++)
            soma += int.Parse(cpf[i].ToString()) * (10 - i);

        var resto = soma % 11;
        var digito1 = resto < 2 ? 0 : 11 - resto;

        if (digito1 != int.Parse(cpf[9].ToString()))
            return false;

        // Valida segundo dÃ­gito verificador
        soma = 0;
        for (int i = 0; i < 10; i++)
            soma += int.Parse(cpf[i].ToString()) * (11 - i);

        resto = soma % 11;
        var digito2 = resto < 2 ? 0 : 11 - resto;

        return digito2 == int.Parse(cpf[10].ToString());
    }

    /// <summary>
    /// Valida CNPJ
    /// </summary>
    private static bool ValidarCnpj(string cnpj)
    {
        // Verifica se todos os dÃ­gitos sÃ£o iguais
        if (cnpj.Distinct().Count() == 1)
            return false;

        // Valida primeiro dÃ­gito verificador
        var multiplicadores1 = new[] { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        var soma = 0;
        for (int i = 0; i < 12; i++)
            soma += int.Parse(cnpj[i].ToString()) * multiplicadores1[i];

        var resto = soma % 11;
        var digito1 = resto < 2 ? 0 : 11 - resto;

        if (digito1 != int.Parse(cnpj[12].ToString()))
            return false;

        // Valida segundo dÃ­gito verificador
        var multiplicadores2 = new[] { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        soma = 0;
        for (int i = 0; i < 13; i++)
            soma += int.Parse(cnpj[i].ToString()) * multiplicadores2[i];

        resto = soma % 11;
        var digito2 = resto < 2 ? 0 : 11 - resto;

        return digito2 == int.Parse(cnpj[13].ToString());
    }
}

