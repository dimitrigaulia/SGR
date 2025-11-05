using SGR.Api.Models.DTOs;

namespace SGR.Api.Services.Interfaces;

public interface IPerfilService
{
    Task<PagedResult<PerfilDto>> GetAllAsync(string? search, int page, int pageSize, string? sort, string? order);
    Task<PerfilDto?> GetByIdAsync(long id);
    Task<PerfilDto> CreateAsync(CreatePerfilRequest request, string? usuarioCriacao);
    Task<PerfilDto?> UpdateAsync(long id, UpdatePerfilRequest request, string? usuarioAtualizacao);
    Task<bool> DeleteAsync(long id);
}
