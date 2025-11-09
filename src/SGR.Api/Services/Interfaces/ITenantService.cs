using SGR.Api.Models.DTOs;
using SGR.Api.Models.Entities;

namespace SGR.Api.Services.Interfaces;

public interface ITenantService : IBaseService<Tenant, TenantDto, CreateTenantRequest, UpdateTenantRequest>,
    IBaseServiceController<TenantDto, CreateTenantRequest, UpdateTenantRequest>
{
    /// <summary>
    /// Cria um novo tenant com toda a infraestrutura necessária
    /// </summary>
    Task<TenantDto> CreateTenantAsync(CreateTenantRequest request, string? usuarioCriacao);

    /// <summary>
    /// Busca tenant por subdomínio
    /// </summary>
    Task<TenantDto?> GetBySubdomainAsync(string subdomain);

    /// <summary>
    /// Busca todos os tenants ativos (para combobox no login)
    /// </summary>
    Task<List<TenantDto>> GetActiveTenantsAsync();

    /// <summary>
    /// Alterna o status ativo/inativo do tenant
    /// </summary>
    Task<bool> ToggleActiveAsync(long id, string? usuarioAtualizacao = null);
}

