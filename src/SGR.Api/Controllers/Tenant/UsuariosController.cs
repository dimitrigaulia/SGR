using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SGR.Api.Models.Tenant.DTOs;
using SGR.Api.Services.Tenant.Interfaces;

namespace SGR.Api.Controllers.Tenant;

/// <summary>
/// Controller para gerenciamento de usuários do tenant
/// </summary>
public class UsuariosController : BaseController<ITenantUsuarioService, TenantUsuarioDto, CreateTenantUsuarioRequest, UpdateTenantUsuarioRequest>
{
    public UsuariosController(ITenantUsuarioService service, ILogger<UsuariosController> logger) : base(service, logger)
    {
    }

    /// <summary>
    /// Verifica se um email já está em uso
    /// </summary>
    /// <param name="email">Email a ser verificado</param>
    /// <param name="excludeId">ID do usuário a ser excluído da verificação (opcional)</param>
    /// <returns>Indica se o email existe</returns>
    [HttpGet("check-email")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CheckEmail([FromQuery] string email, [FromQuery] long? excludeId)
    {
        if (string.IsNullOrWhiteSpace(email)) 
            return BadRequest(new { message = "Email inválido" });
            
        var exists = await _service.EmailExistsAsync(email, excludeId);
        return Ok(new { exists });
    }
}

