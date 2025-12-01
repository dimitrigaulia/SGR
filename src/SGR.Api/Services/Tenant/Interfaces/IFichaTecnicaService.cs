using SGR.Api.Models.DTOs;
using SGR.Api.Models.Tenant.DTOs;

namespace SGR.Api.Services.Tenant.Interfaces;

public interface IFichaTecnicaService
{
    Task<PagedResult<FichaTecnicaDto>> GetAllAsync(string? search, int page, int pageSize, string? sort, string? order);
    Task<FichaTecnicaDto?> GetByIdAsync(long id);
    Task<FichaTecnicaDto> CreateAsync(CreateFichaTecnicaRequest request, string? usuarioCriacao);
    Task<FichaTecnicaDto?> UpdateAsync(long id, UpdateFichaTecnicaRequest request, string? usuarioAtualizacao);
    Task<bool> DeleteAsync(long id);
}

