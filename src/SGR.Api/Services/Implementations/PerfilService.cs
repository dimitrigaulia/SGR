using Microsoft.EntityFrameworkCore;
using SGR.Api.Data;
using SGR.Api.Models.DTOs;
using SGR.Api.Models.Entities;
using SGR.Api.Services.Interfaces;

namespace SGR.Api.Services.Implementations;

public class PerfilService : IPerfilService
{
    private readonly ApplicationDbContext _context;

    public PerfilService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<PerfilDto>> GetAllAsync(string? search, int page, int pageSize, string? sort, string? order)
    {
        var query = _context.Perfis.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p => EF.Functions.ILike(p.Nome, $"%{search}%"));
        }

        var ascending = string.Equals(order, "asc", StringComparison.OrdinalIgnoreCase);
        query = (sort?.ToLower()) switch
        {
            "nome" => ascending ? query.OrderBy(p => p.Nome) : query.OrderByDescending(p => p.Nome),
            "ativo" or "isativo" => ascending ? query.OrderBy(p => p.IsAtivo) : query.OrderByDescending(p => p.IsAtivo),
            _ => query.OrderBy(p => p.Nome)
        };

        var total = await query.CountAsync();
        var items = await query
            .Skip(Math.Max(0, (page - 1) * pageSize))
            .Take(pageSize)
            .Select(p => new PerfilDto
            {
                Id = p.Id,
                Nome = p.Nome,
                IsAtivo = p.IsAtivo,
                UsuarioCriacao = p.UsuarioCriacao,
                UsuarioAtualizacao = p.UsuarioAtualizacao,
                DataCriacao = p.DataCriacao,
                DataAtualizacao = p.DataAtualizacao
            })
            .ToListAsync();

        return new PagedResult<PerfilDto> { Items = items, Total = total };
    }

    public async Task<PerfilDto?> GetByIdAsync(long id)
    {
        var p = await _context.Perfis.FindAsync(id);
        if (p == null) return null;
        return new PerfilDto
        {
            Id = p.Id,
            Nome = p.Nome,
            IsAtivo = p.IsAtivo,
            UsuarioCriacao = p.UsuarioCriacao,
            UsuarioAtualizacao = p.UsuarioAtualizacao,
            DataCriacao = p.DataCriacao,
            DataAtualizacao = p.DataAtualizacao
        };
    }

    public async Task<PerfilDto> CreateAsync(CreatePerfilRequest request, string? usuarioCriacao)
    {
        var entity = new Perfil
        {
            Nome = request.Nome,
            IsAtivo = request.IsAtivo,
            UsuarioCriacao = usuarioCriacao,
            DataCriacao = DateTime.UtcNow
        };

        _context.Perfis.Add(entity);
        await _context.SaveChangesAsync();

        return await GetRequiredDto(entity.Id);
    }

    public async Task<PerfilDto?> UpdateAsync(long id, UpdatePerfilRequest request, string? usuarioAtualizacao)
    {
        var p = await _context.Perfis.FindAsync(id);
        if (p == null) return null;
        p.Nome = request.Nome;
        p.IsAtivo = request.IsAtivo;
        p.UsuarioAtualizacao = usuarioAtualizacao;
        p.DataAtualizacao = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return await GetRequiredDto(p.Id);
    }

    public async Task<bool> DeleteAsync(long id)
    {
        var p = await _context.Perfis.FindAsync(id);
        if (p == null) return false;

        var hasUsers = await _context.Usuarios.AnyAsync(u => u.PerfilId == id);
        if (hasUsers)
            throw new InvalidOperationException("Perfil possui usuários vinculados");

        _context.Perfis.Remove(p);
        await _context.SaveChangesAsync();
        return true;
    }

    private async Task<PerfilDto> GetRequiredDto(long id)
    {
        var p = await _context.Perfis.AsNoTracking().FirstAsync(x => x.Id == id);
        return new PerfilDto
        {
            Id = p.Id,
            Nome = p.Nome,
            IsAtivo = p.IsAtivo,
            UsuarioCriacao = p.UsuarioCriacao,
            UsuarioAtualizacao = p.UsuarioAtualizacao,
            DataCriacao = p.DataCriacao,
            DataAtualizacao = p.DataAtualizacao
        };
    }
}
