using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using SGR.Api.Models.DTOs;
using SGR.Api.Services.Interfaces;

namespace SGR.Api.Services.Implementations;

/// <summary>
/// ImplementaÃ§Ã£o do serviÃ§o para buscar dados de empresa via CNPJ usando BrasilApi
/// </summary>
public class CnpjDataService : ICnpjDataService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CnpjDataService> _logger;

    public CnpjDataService(HttpClient httpClient, ILogger<CnpjDataService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpClient.BaseAddress = new Uri("https://brasilapi.com.br/api/");
        _httpClient.Timeout = TimeSpan.FromSeconds(15);
    }

    public async Task<CnpjDataResponse?> BuscarDadosAsync(string cnpj)
    {
        if (string.IsNullOrWhiteSpace(cnpj))
            return null;

        // Remove caracteres nÃ£o numÃ©ricos
        var cnpjLimpo = Regex.Replace(cnpj, @"[^\d]", "");

        // ValidaÃ§Ã£o bÃ¡sica
        if (cnpjLimpo.Length != 14)
        {
            _logger.LogWarning("CNPJ invÃ¡lido: {Cnpj} (deve ter 14 dÃ­gitos)", cnpj);
            return null;
        }

        try
        {
            _logger.LogInformation("Buscando dados do CNPJ {Cnpj} na BrasilApi", cnpjLimpo);
            
            var response = await _httpClient.GetAsync($"cnpj/v1/{cnpjLimpo}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("CNPJ {Cnpj} nÃ£o encontrado na BrasilApi. Status: {Status}", cnpjLimpo, response.StatusCode);
                return null;
            }

            var jsonContent = await response.Content.ReadAsStringAsync();
            var dados = JsonSerializer.Deserialize<CnpjDataResponse>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (dados != null)
            {
                _logger.LogInformation("Dados do CNPJ {Cnpj} recuperados com sucesso. RazÃ£o Social: {RazaoSocial}", 
                    cnpjLimpo, dados.RazaoSocial);
            }

            return dados;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar dados do CNPJ {Cnpj} na BrasilApi", cnpjLimpo);
            return null;
        }
    }
}

