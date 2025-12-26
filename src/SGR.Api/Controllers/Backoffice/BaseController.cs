using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SGR.Api.Services.Interfaces;

namespace SGR.Api.Controllers.Backoffice;

/// <summary>
/// Controller base genérico para operações CRUD
/// </summary>
/// <typeparam name="TService">Tipo do service</typeparam>
/// <typeparam name="TDto">Tipo do DTO de resposta</typeparam>
/// <typeparam name="TCreateRequest">Tipo do DTO de criação</typeparam>
/// <typeparam name="TUpdateRequest">Tipo do DTO de atualização</typeparam>
[ApiController]
[Route("api/backoffice/[controller]")]
[Authorize]
public abstract class BaseController<TService, TDto, TCreateRequest, TUpdateRequest> : ControllerBase
    where TService : class, IBaseServiceController<TDto, TCreateRequest, TUpdateRequest>
    where TDto : class
    where TCreateRequest : class
    where TUpdateRequest : class
{
    protected readonly TService _service;
    protected readonly ILogger<BaseController<TService, TDto, TCreateRequest, TUpdateRequest>> _logger;

    protected BaseController(TService service, ILogger<BaseController<TService, TDto, TCreateRequest, TUpdateRequest>> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Lista todos os registros com paginação, busca e ordenação
    /// </summary>
    /// <param name="search">Termo de busca</param>
    /// <param name="page">NÃºmero da pÃ¡gina (padrÃ£o: 1)</param>
    /// <param name="pageSize">Tamanho da pÃ¡gina (padrÃ£o: 10)</param>
    /// <param name="sort">Campo para ordenação</param>
    /// <param name="order">Direção da ordenação (asc/desc)</param>
    /// <returns>Lista paginada de registros</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public virtual async Task<IActionResult> GetAll(
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
    /// Busca um registro por ID
    /// </summary>
    /// <param name="id">ID do registro</param>
    /// <returns>Registro encontrado</returns>
    [HttpGet("{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public virtual async Task<IActionResult> GetById(long id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>
    /// Cria um novo registro
    /// </summary>
    /// <param name="request">Dados do registro a ser criado</param>
    /// <returns>Registro criado</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public virtual async Task<IActionResult> Create([FromBody] TCreateRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        
        var dto = await _service.CreateAsync(request, User?.Identity?.Name);
        return CreatedAtAction(nameof(GetById), new { id = GetIdFromDto(dto) }, dto);
    }

    /// <summary>
    /// Atualiza um registro existente
    /// </summary>
    /// <param name="id">ID do registro</param>
    /// <param name="request">Dados atualizados</param>
    /// <returns>Registro atualizado</returns>
    [HttpPut("{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public virtual async Task<IActionResult> Update(long id, [FromBody] TUpdateRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        
        var dto = await _service.UpdateAsync(id, request, User?.Identity?.Name);
        if (dto == null) return NotFound();
        return Ok(dto);
    }

    /// <summary>
    /// Exclui um registro
    /// </summary>
    /// <param name="id">ID do registro</param>
    /// <returns>Sem conteÃºdo</returns>
    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public virtual async Task<IActionResult> Delete(long id)
    {
        var ok = await _service.DeleteAsync(id);
        if (!ok) return NotFound();
        return NoContent();
    }

    protected virtual long GetIdFromDto(TDto dto)
    {
        var idProperty = typeof(TDto).GetProperty("Id");
        if (idProperty != null)
            return Convert.ToInt64(idProperty.GetValue(dto));
        throw new InvalidOperationException("DTO não possui propriedade Id");
    }
}

