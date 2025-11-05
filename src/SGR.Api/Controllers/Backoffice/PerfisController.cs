using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SGR.Api.Models.DTOs;
using SGR.Api.Services.Interfaces;

namespace SGR.Api.Controllers.Backoffice;

[ApiController]
[Route("api/backoffice/[controller]")]
[Authorize]
public class PerfisController : ControllerBase
{
    private readonly IPerfilService _service;

    public PerfisController(IPerfilService service)
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
    public async Task<IActionResult> Create([FromBody] CreatePerfilRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var userName = User?.Identity?.Name;
        var dto = await _service.CreateAsync(request, userName);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdatePerfilRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var userName = User?.Identity?.Name;
        var dto = await _service.UpdateAsync(id, request, userName);
        if (dto == null) return NotFound();
        return Ok(dto);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        try
        {
            var ok = await _service.DeleteAsync(id);
            if (!ok) return NotFound();
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}
