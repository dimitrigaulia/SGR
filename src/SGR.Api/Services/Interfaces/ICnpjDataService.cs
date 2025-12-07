using SGR.Api.Models.DTOs;

namespace SGR.Api.Services.Interfaces;

/// <summary>
/// ServiÃ§o para buscar dados de empresa via CNPJ
/// </summary>
public interface ICnpjDataService
{
    /// <summary>
    /// Busca dados de uma empresa pelo CNPJ usando BrasilApi
    /// </summary>
    /// <param name="cnpj">CNPJ (com ou sem mÃ¡scara)</param>
    /// <returns>Dados da empresa ou null se nÃ£o encontrado</returns>
    Task<CnpjDataResponse?> BuscarDadosAsync(string cnpj);
}

