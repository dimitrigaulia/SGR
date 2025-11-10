using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SGR.Api.Models.Tenant.DTOs;
using SGR.Api.Services.Tenant.Interfaces;

namespace SGR.Api.Controllers.Tenant;

/// <summary>
/// Controller para gerenciamento de receitas do tenant
/// </summary>
[ApiController]
[Route("api/tenant/receitas")]
[Authorize]
public class ReceitasController : ControllerBase
{
    private readonly IReceitaService _service;
    private readonly ILogger<ReceitasController> _logger;

    public ReceitasController(IReceitaService service, ILogger<ReceitasController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Lista todas as receitas com paginação, busca e ordenação
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? sort = null,
        [FromQuery] string? order = null)
    {
        var result = await _service.GetAllAsync(search, page, pageSize, sort, order);
        return Ok(result);
    }

    /// <summary>
    /// Busca uma receita por ID
    /// </summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(long id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>
    /// Cria uma nova receita
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateReceitaRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var dto = await _service.CreateAsync(request, User?.Identity?.Name);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    /// <summary>
    /// Atualiza uma receita existente
    /// </summary>
    [HttpPut("{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateReceitaRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var dto = await _service.UpdateAsync(id, request, User?.Identity?.Name);
        if (dto == null) return NotFound();
        return Ok(dto);
    }

    /// <summary>
    /// Exclui uma receita
    /// </summary>
    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id)
    {
        var ok = await _service.DeleteAsync(id);
        if (!ok) return NotFound();
        return NoContent();
    }

    /// <summary>
    /// Duplica uma receita existente
    /// </summary>
    [HttpPost("{id:long}/duplicar")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Duplicar(long id, [FromBody] DuplicarReceitaRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (string.IsNullOrWhiteSpace(request.NovoNome))
        {
            return BadRequest(new { message = "Novo nome é obrigatório" });
        }

        try
        {
            var dto = await _service.DuplicarAsync(id, request.NovoNome, User?.Identity?.Name);
            return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
        }
        catch (SGR.Api.Exceptions.BusinessException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

/// <summary>
/// Request para duplicar receita
/// </summary>
public class DuplicarReceitaRequest
{
    public string NovoNome { get; set; } = string.Empty;
}

