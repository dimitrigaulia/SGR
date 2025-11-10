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

public class CategoriaInsumoService : BaseService<TenantDbContext, CategoriaInsumo, CategoriaInsumoDto, CreateCategoriaInsumoRequest, UpdateCategoriaInsumoRequest>, ICategoriaInsumoService
{
    public CategoriaInsumoService(TenantDbContext context, ILogger<CategoriaInsumoService> logger) : base(context, logger)
    {
    }

    protected override IQueryable<CategoriaInsumo> ApplySearch(IQueryable<CategoriaInsumo> query, string? search)
    {
        if (string.IsNullOrWhiteSpace(search)) return query;
        return query.Where(p => EF.Functions.ILike(p.Nome, $"%{search}%"));
    }

    protected override IQueryable<CategoriaInsumo> ApplySorting(IQueryable<CategoriaInsumo> query, string? sort, string? order)
    {
        var ascending = string.Equals(order, "asc", StringComparison.OrdinalIgnoreCase);
        return (sort?.ToLower()) switch
        {
            "nome" => ascending ? query.OrderBy(p => p.Nome) : query.OrderByDescending(p => p.Nome),
            "ativo" or "isativo" => ascending ? query.OrderBy(p => p.IsAtivo) : query.OrderByDescending(p => p.IsAtivo),
            _ => query.OrderBy(p => p.Nome)
        };
    }

    protected override Expression<Func<CategoriaInsumo, CategoriaInsumoDto>> MapToDto()
    {
        return p => new CategoriaInsumoDto
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

    protected override CategoriaInsumo MapToEntity(CreateCategoriaInsumoRequest request)
    {
        return new CategoriaInsumo
        {
            Nome = request.Nome,
            IsAtivo = request.IsAtivo
        };
    }

    protected override void UpdateEntity(CategoriaInsumo entity, UpdateCategoriaInsumoRequest request)
    {
        entity.Nome = request.Nome;
        entity.IsAtivo = request.IsAtivo;
    }

    protected override async Task BeforeDeleteAsync(CategoriaInsumo entity)
    {
        var hasInsumos = await _context.Set<Insumo>().AnyAsync(i => i.CategoriaId == entity.Id);
        if (hasInsumos)
        {
            throw new BusinessException("Não é possível excluir uma categoria que possui insumos cadastrados");
        }
    }
}

