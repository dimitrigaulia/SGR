using SGR.Api.Models.DTOs;

namespace SGR.Api.Services.Interfaces;

public interface IUsuarioService
{
    Task<PagedResult<UsuarioDto>> GetAllAsync(string? search, int page, int pageSize, string? sort, string? order);
    Task<UsuarioDto?> GetByIdAsync(long id);
    Task<UsuarioDto> CreateAsync(CreateUsuarioRequest request, string? usuarioCriacao);
    Task<UsuarioDto?> UpdateAsync(long id, UpdateUsuarioRequest request, string? usuarioAtualizacao);
    Task<bool> DeleteAsync(long id);
    Task<bool> EmailExistsAsync(string email, long? excludeId = null);
}
