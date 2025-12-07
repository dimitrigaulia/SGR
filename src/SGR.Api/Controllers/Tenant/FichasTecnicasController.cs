using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SGR.Api.Models.Tenant.DTOs;
using SGR.Api.Services.Common;
using SGR.Api.Services.Tenant.Interfaces;

namespace SGR.Api.Controllers.Tenant;

/// <summary>
/// Controller para gerenciamento de fichas tÃ©cnicas comerciais
/// </summary>
[ApiController]
[Route("api/tenant/fichas-tecnicas")]
[Authorize]
public class FichasTecnicasController : ControllerBase
{
    private readonly IFichaTecnicaService _service;
    private readonly PdfService _pdfService;
    private readonly ILogger<FichasTecnicasController> _logger;

    public FichasTecnicasController(IFichaTecnicaService service, PdfService pdfService, ILogger<FichasTecnicasController> logger)
    {
        _service = service;
        _pdfService = pdfService;
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
    /// Visualiza a ficha tÇ¸cnica em HTML para impressï¿½ï¿½o/PDF
    /// </summary>
    [HttpGet("{id:long}/print")]
    [AllowAnonymous]
    public async Task<IActionResult> Print(long id)
    {
        var ficha = await _service.GetByIdAsync(id);
        if (ficha == null) return NotFound();

        // Construir as linhas da tabela de itens
        var itensHtml = string.Join("", ficha.Itens.Select(i =>
        {
            var quantidadeDisplay = i.ExibirComoQB ? "QB" : i.Quantidade.ToString("0.####");
            var unidadeDisplay = i.ExibirComoQB ? "" : $" {System.Net.WebUtility.HtmlEncode(i.UnidadeMedidaSigla ?? "")}";
            var tipoDisplay = i.TipoItem == "Receita" ? "Receita" : "Insumo";
            var nomeDisplay = i.TipoItem == "Receita" ? (i.ReceitaNome ?? "") : (i.InsumoNome ?? "");
            return $"<tr><td>{tipoDisplay}</td><td>{System.Net.WebUtility.HtmlEncode(nomeDisplay)}</td><td>{quantidadeDisplay}{unidadeDisplay}</td></tr>";
        }));

        // Construir as linhas da tabela de canais
        var canaisHtml = string.Join("", ficha.Canais.Select(c =>
            $"<tr><td>{System.Net.WebUtility.HtmlEncode(c.Canal)}</td><td>{System.Net.WebUtility.HtmlEncode(c.NomeExibicao ?? "")}</td><td>{c.PrecoVenda:C}</td><td>{c.TaxaPercentual ?? 0}</td><td>{(c.MargemCalculadaPercentual?.ToString("0.##") ?? "-")}</td></tr>"));

        var html = $@"
<!DOCTYPE html>
<html lang=""pt-BR"">
<head>
  <meta charset=""utf-8"" />
  <title>Ficha Tï¿½ï¿½cnica - {System.Net.WebUtility.HtmlEncode(ficha.Nome)}</title>
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
  <h1>Ficha Tï¿½ï¿½cnica: {System.Net.WebUtility.HtmlEncode(ficha.Nome)}</h1>
  <p><strong>Receita:</strong> {System.Net.WebUtility.HtmlEncode(ficha.CategoriaNome ?? "")}</p>
  <p><strong>Custo tÇ¸cnico por porï¿½ï¿½Çœo:</strong> {ficha.CustoPorUnidade:C}</p>
  <p><strong>Preï¿½ï¿½o sugerido (por porï¿½ï¿½Çœo):</strong> {(ficha.PrecoSugeridoVenda.HasValue ? ficha.PrecoSugeridoVenda.Value.ToString("C") : "-")}</p>

  <h2>Canais</h2>
  <table>
    <thead>
      <tr>
        <th>Canal</th>
        <th>Nome</th>
        <th>Preï¿½ï¿½o venda</th>
        <th>Taxa (%)</th>
        <th>Comissï¿½ï¿½o (%)</th>
        <th>Margem (%)</th>
      </tr>
    </thead>
    <tbody>
      {canaisHtml}
    </tbody>
  </table>
</body>
</html>";

        return Content(html, "text/html; charset=utf-8");
    }

    /// <summary>
    /// Gera PDF da ficha tÃ©cnica
    /// </summary>
    [HttpGet("{id:long}/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPdf(long id)
    {
        var ficha = await _service.GetByIdAsync(id);
        if (ficha == null) return NotFound();

        try
        {
            var pdfBytes = _pdfService.GenerateFichaTecnicaPdf(ficha);
            return File(pdfBytes, "application/pdf", $"ficha-tecnica-{ficha.Nome.Replace(" ", "-")}-{id}.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar PDF da ficha tÃ©cnica {Id}", id);
            return StatusCode(500, new { message = "Erro ao gerar PDF" });
        }
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
            _logger.LogWarning(ex, "Falha ao criar Ficha TÃ©cnica");
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
            _logger.LogWarning(ex, "Falha ao atualizar Ficha TÃ©cnica");
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
