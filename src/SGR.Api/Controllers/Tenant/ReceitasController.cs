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
    /// Visualiza a receita em HTML para impress��o/PDF
    /// </summary>
    [HttpGet("{id:long}/print")]
    [AllowAnonymous]
    public async Task<IActionResult> Print(long id)
    {
        var receita = await _service.GetByIdAsync(id);
        if (receita == null) return NotFound();

        // Construir as linhas da tabela separadamente para evitar problemas de interpolação aninhada
        var itensHtml = string.Join("", receita.Itens.Select((i, idx) =>
        {
            var quantidadeDisplay = i.ExibirComoQB ? "QB" : i.Quantidade.ToString("0.####");
            var unidadeDisplay = i.ExibirComoQB ? "" : $" {System.Net.WebUtility.HtmlEncode(i.UnidadeMedidaSigla ?? "")}";
            return $"<tr><td>{idx + 1}</td><td>{System.Net.WebUtility.HtmlEncode(i.InsumoNome ?? "")}</td><td>{quantidadeDisplay}{unidadeDisplay}</td><td>{i.CustoItem:C}</td></tr>";
        }));

        var html = $@"
<!DOCTYPE html>
<html lang=""pt-BR"">
<head>
  <meta charset=""utf-8"" />
  <title>Receita - {System.Net.WebUtility.HtmlEncode(receita.Nome)}</title>
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
  <h1>Receita: {System.Net.WebUtility.HtmlEncode(receita.Nome)}</h1>
  <p><strong>Categoria:</strong> {System.Net.WebUtility.HtmlEncode(receita.CategoriaNome ?? string.Empty)}</p>
  <p><strong>Rendimento:</strong> {receita.Rendimento} por��es</p>
  <p><strong>Peso por por��ǜo:</strong> {(receita.PesoPorPorcao?.ToString("0.##") ?? "-")} g</p>
  <p><strong>Custo total:</strong> {receita.CustoTotal:C}</p>
  <p><strong>Custo por por��ǜo:</strong> {receita.CustoPorPorcao:C}</p>

  <h2>Itens</h2>
  <table>
    <thead>
      <tr>
        <th>#</th>
        <th>Insumo</th>
        <th>Quantidade</th>
        <th>Custo do item</th>
      </tr>
    </thead>
    <tbody>
      {itensHtml}
    </tbody>
  </table>

  <h2>Modo de preparo</h2>
  <p>{System.Net.WebUtility.HtmlEncode(receita.Descricao ?? string.Empty)}</p>
</body>
</html>";

        return Content(html, "text/html; charset=utf-8");
    }

    /// <summary>
    /// Gera PDF da receita
    /// </summary>
    [HttpGet("{id:long}/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPdf(long id)
    {
        var receita = await _service.GetByIdAsync(id);
        if (receita == null) return NotFound();

        // Por enquanto, retorna HTML (pode ser convertido para PDF no frontend ou adicionar biblioteca de PDF depois)
        // TODO: Implementar geração de PDF real usando QuestPDF ou iTextSharp
        return await Print(id);
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

