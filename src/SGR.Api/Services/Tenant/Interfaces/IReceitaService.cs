using SGR.Api.Models.DTOs;
using SGR.Api.Models.Tenant.DTOs;

namespace SGR.Api.Services.Tenant.Interfaces;

public interface IReceitaService
{
    Task<PagedResult<ReceitaDto>> GetAllAsync(string? search, int page, int pageSize, string? sort, string? order);
    Task<ReceitaDto?> GetByIdAsync(long id);
    Task<ReceitaDto> CreateAsync(CreateReceitaRequest request, string? usuarioCriacao);
    Task<ReceitaDto?> UpdateAsync(long id, UpdateReceitaRequest request, string? usuarioAtualizacao);
    Task<bool> DeleteAsync(long id);
    Task<ReceitaDto> DuplicarAsync(long id, string novoNome, string? usuarioCriacao);
}

