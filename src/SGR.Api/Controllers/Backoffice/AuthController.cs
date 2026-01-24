using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SGR.Api.Models.DTOs;
using SGR.Api.Services.Interfaces;

namespace SGR.Api.Controllers.Backoffice;

/// <summary>
/// Controller para autenticação
/// </summary>
[ApiController]
[Route("api/backoffice/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Realiza login do usuário
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

        var response = await _authService.LoginAsync(request);

        if (response == null)
        {
            _logger.LogWarning("Tentativa de login falhou para: {Email}", request.Email);
            return Unauthorized(new { message = "Email ou senha inválidos." });
        }

        return Ok(response);
    }
}


