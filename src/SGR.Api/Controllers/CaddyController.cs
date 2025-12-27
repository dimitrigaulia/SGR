using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SGR.Api.Services.Interfaces;

namespace SGR.Api.Controllers;

/// <summary>
/// Controller para integração com Caddy (validação de domínios para On-Demand TLS)
/// </summary>
[ApiController]
[Route("api/caddy")]
[AllowAnonymous]
public class CaddyController : ControllerBase
{
    private readonly ITenantService _tenantService;
    private readonly ILogger<CaddyController> _logger;

    public CaddyController(
        ITenantService tenantService,
        ILogger<CaddyController> logger)
    {
        _tenantService = tenantService;
        _logger = logger;
    }

    /// <summary>
    /// Valida se um domínio pode receber certificado TLS (On-Demand TLS ask endpoint)
    /// O Caddy chama este endpoint via GET com query string ?domain= antes de emitir certificados
    /// </summary>
    // Caddy chama: GET /api/caddy/validate-domain?domain=vangogh.fichapro.ia.br
    [HttpGet("validate-domain")]
    public async Task<IActionResult> ValidateDomain([FromQuery] string? domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
        {
            _logger.LogWarning("Requisição de validação de domínio sem parâmetro 'domain'");
            return NotFound();
        }

        domain = domain.Trim().ToLowerInvariant();

        // Permitir apenas subdomínios de fichapro.ia.br
        if (!domain.EndsWith(".fichapro.ia.br"))
        {
            _logger.LogWarning("Domínio não é subdomínio de fichapro.ia.br: {Domain}", domain);
            return NotFound();
        }

        var subdomain = domain.Split('.')[0];

        // Bloquear nomes reservados
        if (subdomain is "www" or "api" or "admin")
        {
            _logger.LogWarning("Subdomínio reservado não é um tenant: {Subdomain}", subdomain);
            return NotFound();
        }

        // Validar se o tenant existe e está ativo
        var tenant = await _tenantService.GetBySubdomainAsync(subdomain);
        if (tenant is null || !tenant.IsAtivo)
        {
            _logger.LogWarning("Tenant não encontrado ou inativo para domínio: {Domain} (subdomain: {Subdomain})", domain, subdomain);
            return NotFound();
        }

        _logger.LogInformation("Domínio autorizado para On-Demand TLS: {Domain} (tenant: {Subdomain})", domain, subdomain);
        return Ok(); // 2xx autoriza emissão
    }
}

