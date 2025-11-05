using Microsoft.EntityFrameworkCore;
using SGR.Api.Data;
using SGR.Api.Models.DTOs;
using SGR.Api.Models.Entities;
using SGR.Api.Services.Interfaces;

namespace SGR.Api.Services.Implementations;

public class UsuarioService : IUsuarioService
{
    private readonly ApplicationDbContext _context;

    public UsuarioService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<UsuarioDto>> GetAllAsync(string? search, int page, int pageSize, string? sort, string? order)
    {
        var query = _context.Usuarios.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.ToLower();
            query = query.Where(u =>
                EF.Functions.ILike(u.NomeCompleto, $"%{search}%") ||
                EF.Functions.ILike(u.Email, $"%{search}%"));
        }

        // sorting
        var ascending = string.Equals(order, "asc", StringComparison.OrdinalIgnoreCase);
        query = (sort?.ToLower()) switch
        {
            "nome" or "nomecompleto" => ascending ? query.OrderBy(u => u.NomeCompleto) : query.OrderByDescending(u => u.NomeCompleto),
            "email" => ascending ? query.OrderBy(u => u.Email) : query.OrderByDescending(u => u.Email),
            "ativo" or "isativo" => ascending ? query.OrderBy(u => u.IsAtivo) : query.OrderByDescending(u => u.IsAtivo),
            _ => query.OrderBy(u => u.NomeCompleto)
        };

        var total = await query.CountAsync();
        var items = await query
            .Skip(Math.Max(0, (page - 1) * pageSize))
            .Take(pageSize)
            .Select(u => new UsuarioDto
            {
                Id = u.Id,
                PerfilId = u.PerfilId,
                IsAtivo = u.IsAtivo,
                NomeCompleto = u.NomeCompleto,
                Email = u.Email,
                PathImagem = u.PathImagem,
                UsuarioAtualizacao = u.UsuarioAtualizacao,
                DataAtualizacao = u.DataAtualizacao
            })
            .ToListAsync();

        return new PagedResult<UsuarioDto> { Items = items, Total = total };
    }

    public async Task<UsuarioDto?> GetByIdAsync(long id)
    {
        var u = await _context.Usuarios.FindAsync(id);
        if (u == null) return null;
        return new UsuarioDto
        {
            Id = u.Id,
            PerfilId = u.PerfilId,
            IsAtivo = u.IsAtivo,
            NomeCompleto = u.NomeCompleto,
            Email = u.Email,
            PathImagem = u.PathImagem,
            UsuarioAtualizacao = u.UsuarioAtualizacao,
            DataAtualizacao = u.DataAtualizacao
        };
    }

    public async Task<UsuarioDto> CreateAsync(CreateUsuarioRequest request, string? usuarioCriacao)
    {
        // Email único
        var exists = await _context.Usuarios.AnyAsync(x => x.Email == request.Email);
        if (exists)
            throw new InvalidOperationException("E-mail já cadastrado");

        // Verifica perfil
        var perfilExists = await _context.Perfis.AnyAsync(p => p.Id == request.PerfilId && p.IsAtivo);
        if (!perfilExists)
            throw new InvalidOperationException("Perfil inválido ou inativo");

        var entity = new Usuario
        {
            PerfilId = request.PerfilId,
            IsAtivo = request.IsAtivo,
            NomeCompleto = request.NomeCompleto,
            Email = request.Email,
            SenhaHash = BCrypt.Net.BCrypt.HashPassword(request.Senha),
            PathImagem = request.PathImagem,
            UsuarioCriacao = usuarioCriacao,
            DataCriacao = DateTime.UtcNow,
            DataAtualizacao = DateTime.UtcNow
        };

        _context.Usuarios.Add(entity);
        await _context.SaveChangesAsync();

        return await GetRequiredDto(entity.Id);
    }

    public async Task<UsuarioDto?> UpdateAsync(long id, UpdateUsuarioRequest request, string? usuarioAtualizacao)
    {
        var u = await _context.Usuarios.FindAsync(id);
        if (u == null) return null;

        // Email único (exceto o próprio)
        var emailTaken = await _context.Usuarios.AnyAsync(x => x.Email == request.Email && x.Id != id);
        if (emailTaken)
            throw new InvalidOperationException("E-mail já cadastrado");

        // Verifica perfil
        var perfilExists = await _context.Perfis.AnyAsync(p => p.Id == request.PerfilId);
        if (!perfilExists)
            throw new InvalidOperationException("Perfil inválido");

        u.PerfilId = request.PerfilId;
        u.IsAtivo = request.IsAtivo;
        u.NomeCompleto = request.NomeCompleto;
        u.Email = request.Email;
        u.PathImagem = request.PathImagem;
        if (!string.IsNullOrWhiteSpace(request.NovaSenha))
        {
            u.SenhaHash = BCrypt.Net.BCrypt.HashPassword(request.NovaSenha);
        }
        u.UsuarioAtualizacao = usuarioAtualizacao;
        u.DataAtualizacao = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return await GetRequiredDto(u.Id);
    }

    public async Task<bool> DeleteAsync(long id)
    {
        var u = await _context.Usuarios.FindAsync(id);
        if (u == null) return false;
        _context.Usuarios.Remove(u);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> EmailExistsAsync(string email, long? excludeId = null)
    {
        var q = _context.Usuarios.AsQueryable().Where(x => x.Email == email);
        if (excludeId.HasValue)
            q = q.Where(x => x.Id != excludeId.Value);
        return await q.AnyAsync();
    }

    private async Task<UsuarioDto> GetRequiredDto(long id)
    {
        var u = await _context.Usuarios.AsNoTracking().FirstAsync(x => x.Id == id);
        return new UsuarioDto
        {
            Id = u.Id,
            PerfilId = u.PerfilId,
            IsAtivo = u.IsAtivo,
            NomeCompleto = u.NomeCompleto,
            Email = u.Email,
            PathImagem = u.PathImagem,
            UsuarioAtualizacao = u.UsuarioAtualizacao,
            DataAtualizacao = u.DataAtualizacao
        };
    }
}
