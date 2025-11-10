using Microsoft.AspNetCore.Mvc;
using SGR.Api.Controllers.Tenant;
using SGR.Api.Models.Tenant.DTOs;
using SGR.Api.Services.Tenant.Interfaces;

namespace SGR.Api.Controllers.Tenant;

[ApiController]
[Route("api/tenant/insumos")]
public class InsumosController : BaseController<IInsumoService, InsumoDto, CreateInsumoRequest, UpdateInsumoRequest>
{
    private readonly IInsumoService _insumoService;

    public InsumosController(IInsumoService service, ILogger<InsumosController> logger) 
        : base(service, logger)
    {
        _insumoService = service;
    }

    /// <summary>
    /// Verifica se um código de barras já está em uso
    /// </summary>
    [HttpGet("check-codigo-barras")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckCodigoBarras([FromQuery] string codigoBarras, [FromQuery] long? excludeId = null)
    {
        if (string.IsNullOrWhiteSpace(codigoBarras))
        {
            return BadRequest(new { message = "Código de barras é obrigatório" });
        }

        var exists = await _insumoService.CodigoBarrasExistsAsync(codigoBarras, excludeId);
        return Ok(new { exists });
    }
}

