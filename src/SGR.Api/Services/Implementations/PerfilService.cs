using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SGR.Api.Data;
using SGR.Api.Exceptions;
using SGR.Api.Models.DTOs;
using SGR.Api.Models.Entities;
using SGR.Api.Services.Interfaces;
using System.Linq.Expressions;

namespace SGR.Api.Services.Implementations;

public class PerfilService : BaseService<Perfil, PerfilDto, CreatePerfilRequest, UpdatePerfilRequest>, IPerfilService
{
    public PerfilService(ApplicationDbContext context, ILogger<PerfilService> logger) : base(context, logger)
    {
    }

    protected override IQueryable<Perfil> ApplySearch(IQueryable<Perfil> query, string? search)
    {
        if (string.IsNullOrWhiteSpace(search)) return query;
        return query.Where(p => EF.Functions.ILike(p.Nome, $"%{search}%"));
    }

    protected override IQueryable<Perfil> ApplySorting(IQueryable<Perfil> query, string? sort, string? order)
    {
        var ascending = string.Equals(order, "asc", StringComparison.OrdinalIgnoreCase);
        return (sort?.ToLower()) switch
        {
            "nome" => ascending ? query.OrderBy(p => p.Nome) : query.OrderByDescending(p => p.Nome),
            "ativo" or "isativo" => ascending ? query.OrderBy(p => p.IsAtivo) : query.OrderByDescending(p => p.IsAtivo),
            _ => query.OrderBy(p => p.Nome)
        };
    }

    protected override Expression<Func<Perfil, PerfilDto>> MapToDto()
    {
        return p => new PerfilDto
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

    protected override Perfil MapToEntity(CreatePerfilRequest request)
    {
        return new Perfil
        {
            Nome = request.Nome,
            IsAtivo = request.IsAtivo
        };
    }

    protected override void UpdateEntity(Perfil entity, UpdatePerfilRequest request)
    {
        entity.Nome = request.Nome;
        entity.IsAtivo = request.IsAtivo;
    }

    protected override async Task BeforeDeleteAsync(Perfil entity)
    {
        var hasUsers = await _context.Usuarios.AnyAsync(u => u.PerfilId == entity.Id);
        if (hasUsers)
        {
            _logger.LogWarning("Tentativa de excluir perfil {Id} que possui usuários vinculados", entity.Id);
            throw new BusinessException("Perfil possui usuários vinculados");
        }
    }
}
