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

public class CategoriaReceitaService : BaseService<TenantDbContext, CategoriaReceita, CategoriaReceitaDto, CreateCategoriaReceitaRequest, UpdateCategoriaReceitaRequest>, ICategoriaReceitaService
{
    public CategoriaReceitaService(TenantDbContext context, ILogger<CategoriaReceitaService> logger) : base(context, logger)
    {
    }

    protected override IQueryable<CategoriaReceita> ApplySearch(IQueryable<CategoriaReceita> query, string? search)
    {
        if (string.IsNullOrWhiteSpace(search)) return query;
        return query.Where(p => EF.Functions.ILike(p.Nome, $"%{search}%"));
    }

    protected override IQueryable<CategoriaReceita> ApplySorting(IQueryable<CategoriaReceita> query, string? sort, string? order)
    {
        var ascending = string.Equals(order, "asc", StringComparison.OrdinalIgnoreCase);
        return (sort?.ToLower()) switch
        {
            "nome" => ascending ? query.OrderBy(p => p.Nome) : query.OrderByDescending(p => p.Nome),
            "ativo" or "isativo" => ascending ? query.OrderBy(p => p.IsAtivo) : query.OrderByDescending(p => p.IsAtivo),
            _ => query.OrderBy(p => p.Nome)
        };
    }

    protected override Expression<Func<CategoriaReceita, CategoriaReceitaDto>> MapToDto()
    {
        return p => new CategoriaReceitaDto
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

    protected override CategoriaReceita MapToEntity(CreateCategoriaReceitaRequest request)
    {
        return new CategoriaReceita
        {
            Nome = request.Nome,
            IsAtivo = request.IsAtivo
        };
    }

    protected override void UpdateEntity(CategoriaReceita entity, UpdateCategoriaReceitaRequest request)
    {
        entity.Nome = request.Nome;
        entity.IsAtivo = request.IsAtivo;
    }

    protected override async Task BeforeDeleteAsync(CategoriaReceita entity)
    {
        var hasReceitas = await _context.Set<Receita>().AnyAsync(r => r.CategoriaId == entity.Id);
        if (hasReceitas)
        {
            throw new BusinessException("NÃ£o Ã© possÃ­vel excluir uma categoria que possui receitas cadastradas");
        }
    }
}

