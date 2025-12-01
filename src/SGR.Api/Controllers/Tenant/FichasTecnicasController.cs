using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SGR.Api.Models.Tenant.DTOs;
using SGR.Api.Services.Tenant.Interfaces;

namespace SGR.Api.Controllers.Tenant;

/// <summary>
/// Controller para gerenciamento de fichas técnicas comerciais
/// </summary>
[ApiController]
[Route("api/tenant/fichas-tecnicas")]
[Authorize]
public class FichasTecnicasController : ControllerBase
{
    private readonly IFichaTecnicaService _service;
    private readonly ILogger<FichasTecnicasController> _logger;

    public FichasTecnicasController(IFichaTecnicaService service, ILogger<FichasTecnicasController> logger)
    {
        _service = service;
        _logger = logger;
    }

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
    /// Visualiza a ficha tǸcnica em HTML para impress��o/PDF
    /// </summary>
    [HttpGet("{id:long}/print")]
    [AllowAnonymous]
    public async Task<IActionResult> Print(long id)
    {
        var ficha = await _service.GetByIdAsync(id);
        if (ficha == null) return NotFound();

        var html = $@"
<!DOCTYPE html>
<html lang=""pt-BR"">
<head>
  <meta charset=""utf-8"" />
  <title>Ficha T��cnica - {System.Net.WebUtility.HtmlEncode(ficha.Nome)}</title>
  <style>
    body {{ font-family: Arial, sans-serif; font-size: 12px; margin: 16px; }}
    h1 {{ font-size: 20px; margin-bottom: 4px; }}
    h2 {{ font-size: 16px; margin-top: 16px; margin-bottom: 4px; }}
    table {{ width: 100%; border-collapse: collapse; margin-top: 8px; }}
    th, td {{ border: 1px solid #ccc; padding: 4px; text-align: left; }}
    th {{ background: #f2f2f2; }}
  </style>
</head>
<body>
  <h1>Ficha T��cnica: {System.Net.WebUtility.HtmlEncode(ficha.Nome)}</h1>
  <p><strong>Receita:</strong> {System.Net.WebUtility.HtmlEncode(ficha.ReceitaNome)}</p>
  <p><strong>Custo tǸcnico por por��ǜo:</strong> {ficha.CustoTecnicoPorPorcao:C}</p>
  <p><strong>Pre��o sugerido (por por��ǜo):</strong> {(ficha.PrecoSugeridoVenda.HasValue ? ficha.PrecoSugeridoVenda.Value.ToString("C") : "-")}</p>

  <h2>Canais</h2>
  <table>
    <thead>
      <tr>
        <th>Canal</th>
        <th>Nome</th>
        <th>Pre��o venda</th>
        <th>Taxa (%)</th>
        <th>Comiss��o (%)</th>
        <th>Margem (%)</th>
      </tr>
    </thead>
    <tbody>
      {string.Join("", ficha.Canais.Select(c =>
        $"<tr><td>{System.Net.WebUtility.HtmlEncode(c.Canal)}</td><td>{System.Net.WebUtility.HtmlEncode(c.NomeExibicao ?? \"\")}</td><td>{c.PrecoVenda:C}</td><td>{c.TaxaPercentual ?? 0}</td><td>{c.ComissaoPercentual ?? 0}</td><td>{(c.MargemCalculadaPercentual?.ToString(\"0.##\") ?? \"-\")}</td></tr>"))}
    </tbody>
  </table>
</body>
</html>";

        return Content(html, "text/html; charset=utf-8");
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateFichaTecnicaRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var dto = await _service.CreateAsync(request, User?.Identity?.Name);
            return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
        }
        catch (SGR.Api.Exceptions.BusinessException ex)
        {
            _logger.LogWarning(ex, "Falha ao criar Ficha Técnica");
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateFichaTecnicaRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var dto = await _service.UpdateAsync(id, request, User?.Identity?.Name);
            if (dto == null) return NotFound();
            return Ok(dto);
        }
        catch (SGR.Api.Exceptions.BusinessException ex)
        {
            _logger.LogWarning(ex, "Falha ao atualizar Ficha Técnica");
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id)
    {
        var ok = await _service.DeleteAsync(id);
        if (!ok) return NotFound();
        return NoContent();
    }
}
