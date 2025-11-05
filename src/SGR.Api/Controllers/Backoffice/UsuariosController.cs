using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SGR.Api.Models.DTOs;
using SGR.Api.Services.Interfaces;

namespace SGR.Api.Controllers.Backoffice;

[ApiController]
[Route("api/backoffice/[controller]")]
[Authorize]
public class UsuariosController : ControllerBase
{
    private readonly IUsuarioService _service;

    public UsuariosController(IUsuarioService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? sort = null, [FromQuery] string? order = null)
    {
        var result = await _service.GetAllAsync(search, page, pageSize, sort, order);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUsuarioRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            var dto = await _service.CreateAsync(request, User?.Identity?.Name);
            return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateUsuarioRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            var dto = await _service.UpdateAsync(id, request, User?.Identity?.Name);
            if (dto == null) return NotFound();
            return Ok(dto);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        var ok = await _service.DeleteAsync(id);
        if (!ok) return NotFound();
        return NoContent();
    }

    [HttpGet("check-email")]
    public async Task<IActionResult> CheckEmail([FromQuery] string email, [FromQuery] long? excludeId)
    {
        if (string.IsNullOrWhiteSpace(email)) return BadRequest(new { message = "Email inválido" });
        var exists = await _service.EmailExistsAsync(email, excludeId);
        return Ok(new { exists });
    }
}
