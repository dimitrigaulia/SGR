using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SGR.Api.Data;
using SGR.Api.Exceptions;
using SGR.Api.Models.Backoffice.DTOs;
using SGR.Api.Models.Backoffice.Entities;
using SGR.Api.Services.Backoffice.Interfaces;
using SGR.Api.Services.Common;
using SGR.Api.Services.Interfaces;
using System.Linq.Expressions;

namespace SGR.Api.Services.Backoffice.Implementations;

public class BackofficePerfilService : BaseService<ApplicationDbContext, BackofficePerfil, BackofficePerfilDto, CreateBackofficePerfilRequest, UpdateBackofficePerfilRequest>, IBackofficePerfilService
{
    public BackofficePerfilService(ApplicationDbContext context, ILogger<BackofficePerfilService> logger) : base(context, logger)
    {
    }

    protected override IQueryable<BackofficePerfil> ApplySearch(IQueryable<BackofficePerfil> query, string? search)
    {
        if (string.IsNullOrWhiteSpace(search)) return query;
        return query.Where(p => EF.Functions.ILike(p.Nome, $"%{search}%"));
    }

    protected override IQueryable<BackofficePerfil> ApplySorting(IQueryable<BackofficePerfil> query, string? sort, string? order)
    {
        var ascending = string.Equals(order, "asc", StringComparison.OrdinalIgnoreCase);
        return (sort?.ToLower()) switch
        {
            "nome" => ascending ? query.OrderBy(p => p.Nome) : query.OrderByDescending(p => p.Nome),
            "ativo" or "isativo" => ascending ? query.OrderBy(p => p.IsAtivo) : query.OrderByDescending(p => p.IsAtivo),
            _ => query.OrderBy(p => p.Nome)
        };
    }

    protected override Expression<Func<BackofficePerfil, BackofficePerfilDto>> MapToDto()
    {
        return p => new BackofficePerfilDto
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

    protected override BackofficePerfil MapToEntity(CreateBackofficePerfilRequest request)
    {
        return new BackofficePerfil
        {
            Nome = request.Nome,
            IsAtivo = request.IsAtivo
        };
    }

    protected override void UpdateEntity(BackofficePerfil entity, UpdateBackofficePerfilRequest request)
    {
        entity.Nome = request.Nome;
        entity.IsAtivo = request.IsAtivo;
    }

    protected override async Task BeforeDeleteAsync(BackofficePerfil entity)
    {
        var hasUsers = await _context.Set<BackofficeUsuario>().AnyAsync(u => u.PerfilId == entity.Id);
        if (hasUsers)
        {
            _logger.LogWarning("Tentativa de excluir perfil {Id} que possui usuÃ¡rios vinculados", entity.Id);
            throw new BusinessException("Perfil possui usuÃ¡rios vinculados");
        }
    }
}

