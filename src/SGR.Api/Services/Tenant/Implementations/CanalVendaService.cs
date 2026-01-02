using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SGR.Api.Data;
using SGR.Api.Exceptions;
using SGR.Api.Models.Tenant.DTOs;
using SGR.Api.Models.Tenant.Entities;
using SGR.Api.Services.Common;
using SGR.Api.Services.Tenant.Interfaces;
using System.Linq.Expressions;

namespace SGR.Api.Services.Tenant.Implementations;

public class CanalVendaService : BaseService<TenantDbContext, CanalVenda, CanalVendaDto, CreateCanalVendaRequest, UpdateCanalVendaRequest>, ICanalVendaService
{
    public CanalVendaService(TenantDbContext context, ILogger<CanalVendaService> logger) : base(context, logger)
    {
    }

    protected override IQueryable<CanalVenda> ApplySearch(IQueryable<CanalVenda> query, string? search)
    {
        if (string.IsNullOrWhiteSpace(search)) return query;
        return query.Where(p => EF.Functions.ILike(p.Nome, $"%{search}%"));
    }

    protected override IQueryable<CanalVenda> ApplySorting(IQueryable<CanalVenda> query, string? sort, string? order)
    {
        var ascending = string.Equals(order, "asc", StringComparison.OrdinalIgnoreCase);
        return (sort?.ToLower()) switch
        {
            "nome" => ascending ? query.OrderBy(p => p.Nome) : query.OrderByDescending(p => p.Nome),
            "ativo" or "isativo" => ascending ? query.OrderBy(p => p.IsAtivo) : query.OrderByDescending(p => p.IsAtivo),
            _ => query.OrderBy(p => p.Nome)
        };
    }

    protected override Expression<Func<CanalVenda, CanalVendaDto>> MapToDto()
    {
        return p => new CanalVendaDto
        {
            Id = p.Id,
            Nome = p.Nome,
            TaxaPercentualPadrao = p.TaxaPercentualPadrao,
            IsAtivo = p.IsAtivo,
            UsuarioCriacao = p.UsuarioCriacao,
            UsuarioAtualizacao = p.UsuarioAtualizacao,
            DataCriacao = p.DataCriacao,
            DataAtualizacao = p.DataAtualizacao
        };
    }

    protected override CanalVenda MapToEntity(CreateCanalVendaRequest request)
    {
        return new CanalVenda
        {
            Nome = request.Nome,
            TaxaPercentualPadrao = request.TaxaPercentualPadrao,
            IsAtivo = request.IsAtivo
        };
    }

    protected override void UpdateEntity(CanalVenda entity, UpdateCanalVendaRequest request)
    {
        entity.Nome = request.Nome;
        entity.TaxaPercentualPadrao = request.TaxaPercentualPadrao;
        entity.IsAtivo = request.IsAtivo;
    }

    protected override async Task BeforeCreateAsync(CanalVenda entity, CreateCanalVendaRequest request, string? usuarioCriacao)
    {
        // Verificar se já existe um canal com o mesmo nome
        var nomeExists = await _context.Set<CanalVenda>()
            .AnyAsync(c => c.Nome == request.Nome);
        
        if (nomeExists)
        {
            throw new BusinessException($"Já existe um canal com o nome '{request.Nome}'");
        }
    }

    protected override async Task BeforeUpdateAsync(CanalVenda entity, UpdateCanalVendaRequest request, string? usuarioAtualizacao)
    {
        // Verificar se já existe outro canal com o mesmo nome
        var nomeExists = await _context.Set<CanalVenda>()
            .AnyAsync(c => c.Nome == request.Nome && c.Id != entity.Id);
        
        if (nomeExists)
        {
            throw new BusinessException($"Já existe um canal com o nome '{request.Nome}'");
        }
    }

    protected override async Task BeforeDeleteAsync(CanalVenda entity)
    {
        var hasFichasTecnicas = await _context.Set<FichaTecnicaCanal>()
            .AnyAsync(f => f.CanalVendaId == entity.Id);
        
        if (hasFichasTecnicas)
        {
            throw new BusinessException("Não é possível excluir um canal que está sendo usado em fichas técnicas");
        }
    }
}
