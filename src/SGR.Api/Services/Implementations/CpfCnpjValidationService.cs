using System.Text.RegularExpressions;
using SGR.Api.Services.Interfaces;

namespace SGR.Api.Services.Implementations;

/// <summary>
/// Implementação do serviço de validação de CPF/CNPJ usando BrasilApi
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

        // Remove caracteres não numéricos
        var documento = Regex.Replace(cpfCnpj, @"[^\d]", "");

        // Validação básica de tamanho
        if (documento.Length != 11 && documento.Length != 14)
            return false;

        // Validação de dígitos verificadores (algoritmo local)
        if (!ValidarDigitosVerificadores(documento))
            return false;

        // Validação via BrasilApi
        try
        {
            var tipo = documento.Length == 11 ? "cpf" : "cnpj";
            var response = await _httpClient.GetAsync($"{tipo}/v1/{documento}");

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("CPF/CNPJ {Documento} validado com sucesso via BrasilApi", documento);
                return true;
            }

            _logger.LogWarning("CPF/CNPJ {Documento} inválido segundo BrasilApi", documento);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao validar CPF/CNPJ {Documento} via BrasilApi. Usando validação local apenas.", documento);
            // Em caso de erro na API, confiar apenas na validação de dígitos verificadores
            return ValidarDigitosVerificadores(documento);
        }
    }

    /// <summary>
    /// Valida os dígitos verificadores de CPF ou CNPJ
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
        // Verifica se todos os dígitos são iguais
        if (cpf.Distinct().Count() == 1)
            return false;

        // Valida primeiro dígito verificador
        var soma = 0;
        for (int i = 0; i < 9; i++)
            soma += int.Parse(cpf[i].ToString()) * (10 - i);

        var resto = soma % 11;
        var digito1 = resto < 2 ? 0 : 11 - resto;

        if (digito1 != int.Parse(cpf[9].ToString()))
            return false;

        // Valida segundo dígito verificador
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
        // Verifica se todos os dígitos são iguais
        if (cnpj.Distinct().Count() == 1)
            return false;

        // Valida primeiro dígito verificador
        var multiplicadores1 = new[] { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        var soma = 0;
        for (int i = 0; i < 12; i++)
            soma += int.Parse(cnpj[i].ToString()) * multiplicadores1[i];

        var resto = soma % 11;
        var digito1 = resto < 2 ? 0 : 11 - resto;

        if (digito1 != int.Parse(cnpj[12].ToString()))
            return false;

        // Valida segundo dígito verificador
        var multiplicadores2 = new[] { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        soma = 0;
        for (int i = 0; i < 13; i++)
            soma += int.Parse(cnpj[i].ToString()) * multiplicadores2[i];

        resto = soma % 11;
        var digito2 = resto < 2 ? 0 : 11 - resto;

        return digito2 == int.Parse(cnpj[13].ToString());
    }
}

