using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SGR.Api.Models.DTOs;
using SGR.Api.Services.Interfaces;

namespace SGR.Api.Controllers.Backoffice;

/// <summary>
/// Controller para gerenciamento de Tenants
/// </summary>
[ApiController]
[Route("api/backoffice/[controller]")]
[Authorize]
public class TenantsController : BaseController<ITenantService, TenantDto, CreateTenantRequest, UpdateTenantRequest>
{
    private readonly ITenantService _tenantService;
    private readonly ICnpjDataService _cnpjDataService;

    public TenantsController(
        ITenantService service, 
        ICnpjDataService cnpjDataService,
        ILogger<TenantsController> logger) : base(service, logger)
    {
        _tenantService = service;
        _cnpjDataService = cnpjDataService;
    }

    /// <summary>
    /// Busca tenant por subdomínio
    /// </summary>
    [HttpGet("subdomain/{subdomain}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBySubdomain(string subdomain)
    {
        var tenant = await _tenantService.GetBySubdomainAsync(subdomain);
        if (tenant == null) return NotFound();
        return Ok(tenant);
    }

    /// <summary>
    /// Busca todos os tenants ativos (para combobox no login)
    /// </summary>
    [HttpGet("active")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActiveTenants()
    {
        var tenants = await _tenantService.GetActiveTenantsAsync();
        return Ok(tenants);
    }

    /// <summary>
    /// Busca dados de uma empresa pelo CNPJ
    /// </summary>
    [HttpGet("cnpj/{cnpj}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCnpjData(string cnpj)
    {
        var dados = await _cnpjDataService.BuscarDadosAsync(cnpj);
        if (dados == null) return NotFound(new { message = "CNPJ não encontrado" });
        return Ok(dados);
    }

    /// <summary>
    /// Alterna o status ativo/inativo do tenant
    /// </summary>
    [HttpPatch("{id:long}/toggle-active")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleActive(long id)
    {
        var ok = await _tenantService.ToggleActiveAsync(id, User?.Identity?.Name);
        if (!ok) return NotFound();
        return Ok(new { message = "Status do tenant alterado com sucesso" });
    }
}

