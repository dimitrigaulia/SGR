using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SGR.Api.Data;
using SGR.Api.Exceptions;
using SGR.Api.Models.DTOs;
using SGR.Api.Models.Entities;
using SGR.Api.Services.Interfaces;
using System.Linq.Expressions;

namespace SGR.Api.Services.Implementations;

public class UsuarioService : BaseService<Usuario, UsuarioDto, CreateUsuarioRequest, UpdateUsuarioRequest>, IUsuarioService
{
    public UsuarioService(ApplicationDbContext context, ILogger<UsuarioService> logger) : base(context, logger)
    {
    }

    protected override IQueryable<Usuario> ApplySearch(IQueryable<Usuario> query, string? search)
    {
        if (string.IsNullOrWhiteSpace(search)) return query;
        search = search.ToLower();
        return query.Where(u =>
            EF.Functions.ILike(u.NomeCompleto, $"%{search}%") ||
            EF.Functions.ILike(u.Email, $"%{search}%"));
    }

    protected override IQueryable<Usuario> ApplySorting(IQueryable<Usuario> query, string? sort, string? order)
    {
        var ascending = string.Equals(order, "asc", StringComparison.OrdinalIgnoreCase);
        return (sort?.ToLower()) switch
        {
            "nome" or "nomecompleto" => ascending ? query.OrderBy(u => u.NomeCompleto) : query.OrderByDescending(u => u.NomeCompleto),
            "email" => ascending ? query.OrderBy(u => u.Email) : query.OrderByDescending(u => u.Email),
            "ativo" or "isativo" => ascending ? query.OrderBy(u => u.IsAtivo) : query.OrderByDescending(u => u.IsAtivo),
            _ => query.OrderBy(u => u.NomeCompleto)
        };
    }

    protected override Expression<Func<Usuario, UsuarioDto>> MapToDto()
    {
        return u => new UsuarioDto
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

    protected override Usuario MapToEntity(CreateUsuarioRequest request)
    {
        return new Usuario
        {
            PerfilId = request.PerfilId,
            IsAtivo = request.IsAtivo,
            NomeCompleto = request.NomeCompleto,
            Email = request.Email,
            SenhaHash = BCrypt.Net.BCrypt.HashPassword(request.Senha),
            PathImagem = request.PathImagem
        };
    }

    protected override void UpdateEntity(Usuario entity, UpdateUsuarioRequest request)
    {
        entity.PerfilId = request.PerfilId;
        entity.IsAtivo = request.IsAtivo;
        entity.NomeCompleto = request.NomeCompleto;
        entity.Email = request.Email;
        entity.PathImagem = request.PathImagem;
        
        if (!string.IsNullOrWhiteSpace(request.NovaSenha))
        {
            entity.SenhaHash = BCrypt.Net.BCrypt.HashPassword(request.NovaSenha);
        }
    }

    protected override async Task BeforeCreateAsync(Usuario entity, CreateUsuarioRequest request, string? usuarioCriacao)
    {
        // Validação de email único
        var exists = await _context.Usuarios.AnyAsync(x => x.Email == request.Email);
        if (exists)
        {
            _logger.LogWarning("Tentativa de criar usuário com email já existente: {Email}", request.Email);
            throw new BusinessException("E-mail já cadastrado");
        }

        // Validação de perfil
        var perfilExists = await _context.Perfis.AnyAsync(p => p.Id == request.PerfilId && p.IsAtivo);
        if (!perfilExists)
        {
            _logger.LogWarning("Tentativa de criar usuário com perfil inválido ou inativo: {PerfilId}", request.PerfilId);
            throw new BusinessException("Perfil inválido ou inativo");
        }
    }

    protected override async Task BeforeUpdateAsync(Usuario entity, UpdateUsuarioRequest request, string? usuarioAtualizacao)
    {
        // Validação de email único (exceto o próprio)
        var emailTaken = await _context.Usuarios.AnyAsync(x => x.Email == request.Email && x.Id != entity.Id);
        if (emailTaken)
        {
            _logger.LogWarning("Tentativa de atualizar usuário {Id} com email já existente: {Email}", entity.Id, request.Email);
            throw new BusinessException("E-mail já cadastrado");
        }

        // Validação de perfil
        var perfilExists = await _context.Perfis.AnyAsync(p => p.Id == request.PerfilId);
        if (!perfilExists)
        {
            _logger.LogWarning("Tentativa de atualizar usuário {Id} com perfil inválido: {PerfilId}", entity.Id, request.PerfilId);
            throw new BusinessException("Perfil inválido");
        }
    }

    // Método específico que não está na interface base
    public async Task<bool> EmailExistsAsync(string email, long? excludeId = null)
    {
        var q = _context.Usuarios.AsQueryable().Where(x => x.Email == email);
        if (excludeId.HasValue)
            q = q.Where(x => x.Id != excludeId.Value);
        return await q.AnyAsync();
    }
}
