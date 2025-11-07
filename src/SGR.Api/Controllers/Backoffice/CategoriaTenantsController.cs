using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SGR.Api.Data;
using SGR.Api.Models.DTOs;

namespace SGR.Api.Controllers.Backoffice;

/// <summary>
/// Controller para gerenciamento de Categorias de Tenant
/// </summary>
[ApiController]
[Route("api/backoffice/[controller]")]
[Authorize]
public class CategoriaTenantsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CategoriaTenantsController> _logger;

    public CategoriaTenantsController(ApplicationDbContext context, ILogger<CategoriaTenantsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Lista todas as categorias ativas
    /// </summary>
    [HttpGet("active")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActive()
    {
        var categorias = await _context.CategoriaTenants
            .Where(c => c.IsAtivo)
            .OrderBy(c => c.Nome)
            .Select(c => new CategoriaTenantDto
            {
                Id = c.Id,
                Nome = c.Nome,
                IsAtivo = c.IsAtivo
            })
            .ToListAsync();

        return Ok(categorias);
    }
}

