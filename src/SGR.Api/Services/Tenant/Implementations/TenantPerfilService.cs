using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SGR.Api.Data;
using SGR.Api.Exceptions;
using SGR.Api.Models.Tenant.DTOs;
using SGR.Api.Models.Tenant.Entities;
using SGR.Api.Services.Common;
using SGR.Api.Services.Interfaces;
using SGR.Api.Services.Tenant.Interfaces;
using System.Linq.Expressions;

namespace SGR.Api.Services.Tenant.Implementations;

public class TenantPerfilService : BaseService<TenantDbContext, TenantPerfil, TenantPerfilDto, CreateTenantPerfilRequest, UpdateTenantPerfilRequest>, ITenantPerfilService
{
    public TenantPerfilService(TenantDbContext context, ILogger<TenantPerfilService> logger) : base(context, logger)
    {
    }

    protected override IQueryable<TenantPerfil> ApplySearch(IQueryable<TenantPerfil> query, string? search)
    {
        if (string.IsNullOrWhiteSpace(search)) return query;
        return query.Where(p => EF.Functions.ILike(p.Nome, $"%{search}%"));
    }

    protected override IQueryable<TenantPerfil> ApplySorting(IQueryable<TenantPerfil> query, string? sort, string? order)
    {
        var ascending = string.Equals(order, "asc", StringComparison.OrdinalIgnoreCase);
        return (sort?.ToLower()) switch
        {
            "nome" => ascending ? query.OrderBy(p => p.Nome) : query.OrderByDescending(p => p.Nome),
            "ativo" or "isativo" => ascending ? query.OrderBy(p => p.IsAtivo) : query.OrderByDescending(p => p.IsAtivo),
            _ => query.OrderBy(p => p.Nome)
        };
    }

    protected override Expression<Func<TenantPerfil, TenantPerfilDto>> MapToDto()
    {
        return p => new TenantPerfilDto
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

    protected override TenantPerfil MapToEntity(CreateTenantPerfilRequest request)
    {
        return new TenantPerfil
        {
            Nome = request.Nome,
            IsAtivo = request.IsAtivo
        };
    }

    protected override void UpdateEntity(TenantPerfil entity, UpdateTenantPerfilRequest request)
    {
        entity.Nome = request.Nome;
        entity.IsAtivo = request.IsAtivo;
    }

    protected override async Task BeforeDeleteAsync(TenantPerfil entity)
    {
        var hasUsers = await _context.Set<TenantUsuario>().AnyAsync(u => u.PerfilId == entity.Id);
        if (hasUsers)
        {
            _logger.LogWarning("Tentativa de excluir perfil {Id} que possui usuários vinculados", entity.Id);
            throw new BusinessException("Perfil possui usuários vinculados");
        }
    }
}

