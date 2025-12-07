using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SGR.Api.Models.Tenant.DTOs;
using SGR.Api.Services.Tenant.Interfaces;

namespace SGR.Api.Controllers.Tenant;

/// <summary>
/// Controller para gerenciamento de usuÃ¡rios do tenant
/// </summary>
public class UsuariosController : BaseController<ITenantUsuarioService, TenantUsuarioDto, CreateTenantUsuarioRequest, UpdateTenantUsuarioRequest>
{
    public UsuariosController(ITenantUsuarioService service, ILogger<UsuariosController> logger) : base(service, logger)
    {
    }

    /// <summary>
    /// Verifica se um email jÃ¡ estÃ¡ em uso
    /// </summary>
    /// <param name="email">Email a ser verificado</param>
    /// <param name="excludeId">ID do usuÃ¡rio a ser excluÃ­do da verificaÃ§Ã£o (opcional)</param>
    /// <returns>Indica se o email existe</returns>
    [HttpGet("check-email")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CheckEmail([FromQuery] string email, [FromQuery] long? excludeId)
    {
        if (string.IsNullOrWhiteSpace(email)) 
            return BadRequest(new { message = "Email invÃ¡lido" });
            
        var exists = await _service.EmailExistsAsync(email, excludeId);
        return Ok(new { exists });
    }
}

