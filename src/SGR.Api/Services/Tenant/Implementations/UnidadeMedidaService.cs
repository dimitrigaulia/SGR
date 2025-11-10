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

public class UnidadeMedidaService : BaseService<TenantDbContext, UnidadeMedida, UnidadeMedidaDto, CreateUnidadeMedidaRequest, UpdateUnidadeMedidaRequest>, IUnidadeMedidaService
{
    public UnidadeMedidaService(TenantDbContext context, ILogger<UnidadeMedidaService> logger) : base(context, logger)
    {
    }

    protected override IQueryable<UnidadeMedida> ApplySearch(IQueryable<UnidadeMedida> query, string? search)
    {
        if (string.IsNullOrWhiteSpace(search)) return query;
        return query.Where(u => 
            EF.Functions.ILike(u.Nome, $"%{search}%") ||
            EF.Functions.ILike(u.Sigla, $"%{search}%"));
    }

    protected override IQueryable<UnidadeMedida> ApplySorting(IQueryable<UnidadeMedida> query, string? sort, string? order)
    {
        var ascending = string.Equals(order, "asc", StringComparison.OrdinalIgnoreCase);
        return (sort?.ToLower()) switch
        {
            "nome" => ascending ? query.OrderBy(u => u.Nome) : query.OrderByDescending(u => u.Nome),
            "sigla" => ascending ? query.OrderBy(u => u.Sigla) : query.OrderByDescending(u => u.Sigla),
            "tipo" => ascending ? query.OrderBy(u => u.Tipo ?? "") : query.OrderByDescending(u => u.Tipo ?? ""),
            "ativo" or "isativo" => ascending ? query.OrderBy(u => u.IsAtivo) : query.OrderByDescending(u => u.IsAtivo),
            _ => query.OrderBy(u => u.Nome)
        };
    }

    protected override Expression<Func<UnidadeMedida, UnidadeMedidaDto>> MapToDto()
    {
        return u => new UnidadeMedidaDto
        {
            Id = u.Id,
            Nome = u.Nome,
            Sigla = u.Sigla,
            Tipo = u.Tipo,
            IsAtivo = u.IsAtivo,
            UsuarioCriacao = u.UsuarioCriacao,
            UsuarioAtualizacao = u.UsuarioAtualizacao,
            DataCriacao = u.DataCriacao,
            DataAtualizacao = u.DataAtualizacao
        };
    }

    protected override UnidadeMedida MapToEntity(CreateUnidadeMedidaRequest request)
    {
        return new UnidadeMedida
        {
            Nome = request.Nome,
            Sigla = request.Sigla,
            Tipo = request.Tipo,
            IsAtivo = request.IsAtivo
        };
    }

    protected override void UpdateEntity(UnidadeMedida entity, UpdateUnidadeMedidaRequest request)
    {
        entity.Nome = request.Nome;
        entity.Sigla = request.Sigla;
        entity.Tipo = request.Tipo;
        entity.IsAtivo = request.IsAtivo;
    }

    protected override async Task BeforeDeleteAsync(UnidadeMedida entity)
    {
        var hasInsumos = await _context.Set<Insumo>().AnyAsync(i => i.UnidadeMedidaId == entity.Id);
        if (hasInsumos)
        {
            throw new BusinessException("Não é possível excluir uma unidade de medida que possui insumos cadastrados");
        }
    }
}

