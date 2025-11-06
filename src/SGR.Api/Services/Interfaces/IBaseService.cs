using SGR.Api.Models.DTOs;

namespace SGR.Api.Services.Interfaces;

public interface IBaseService<TEntity, TDto, TCreateRequest, TUpdateRequest>
    where TEntity : class
    where TDto : class
    where TCreateRequest : class
    where TUpdateRequest : class
{
    Task<PagedResult<TDto>> GetAllAsync(string? search, int page, int pageSize, string? sort, string? order);
    Task<TDto?> GetByIdAsync(long id);
    Task<TDto> CreateAsync(TCreateRequest request, string? usuarioCriacao);
    Task<TDto?> UpdateAsync(long id, TUpdateRequest request, string? usuarioAtualizacao);
    Task<bool> DeleteAsync(long id);
}

// Interface sem o tipo Entity para uso em controllers
public interface IBaseServiceController<TDto, TCreateRequest, TUpdateRequest>
    where TDto : class
    where TCreateRequest : class
    where TUpdateRequest : class
{
    Task<PagedResult<TDto>> GetAllAsync(string? search, int page, int pageSize, string? sort, string? order);
    Task<TDto?> GetByIdAsync(long id);
    Task<TDto> CreateAsync(TCreateRequest request, string? usuarioCriacao);
    Task<TDto?> UpdateAsync(long id, TUpdateRequest request, string? usuarioAtualizacao);
    Task<bool> DeleteAsync(long id);
}

