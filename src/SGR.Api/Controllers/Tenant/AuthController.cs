using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SGR.Api.Models.DTOs;
using SGR.Api.Services.Interfaces;

namespace SGR.Api.Controllers.Tenant;

/// <summary>
/// Controller para autenticação de tenants
/// </summary>
[ApiController]
[Route("api/tenant/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ITenantAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(ITenantAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Realiza login do usuário do tenant
    /// </summary>
    /// <param name="request">Credenciais de login</param>
    /// <returns>Token JWT e dados do usuário</returns>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Verificar se o tenant foi identificado pelo middleware
        var tenant = HttpContext.Items["Tenant"] as TenantDto;
        if (tenant == null)
        {
            _logger.LogWarning("Tentativa de login do tenant sem identificação do tenant");
            return BadRequest(new { message = "Tenant não identificado. Forneça o header X-Tenant-Subdomain." });
        }

        var response = await _authService.LoginAsync(request);

        if (response == null)
        {
            _logger.LogWarning("Tentativa de login do tenant falhou para: {Email}", request.Email);
            return Unauthorized(new { message = "Email ou senha inválidos" });
        }

        return Ok(response);
    }
}

