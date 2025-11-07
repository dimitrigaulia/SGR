using SGR.Api.Models.DTOs;

namespace SGR.Api.Services.Interfaces;

/// <summary>
/// Serviço para buscar dados de empresa via CNPJ
/// </summary>
public interface ICnpjDataService
{
    /// <summary>
    /// Busca dados de uma empresa pelo CNPJ usando BrasilApi
    /// </summary>
    /// <param name="cnpj">CNPJ (com ou sem máscara)</param>
    /// <returns>Dados da empresa ou null se não encontrado</returns>
    Task<CnpjDataResponse?> BuscarDadosAsync(string cnpj);
}

