using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SGR.Api.Data;
using SGR.Api.Exceptions;
using SGR.Api.Models.DTOs;
using SGR.Api.Models.Tenant.DTOs;
using SGR.Api.Models.Tenant.Entities;
using SGR.Api.Services.Common;
using SGR.Api.Services.Interfaces;
using SGR.Api.Services.Tenant.Interfaces;
using System.Linq.Expressions;

namespace SGR.Api.Services.Tenant.Implementations;

public class InsumoService : BaseService<TenantDbContext, Insumo, InsumoDto, CreateInsumoRequest, UpdateInsumoRequest>, IInsumoService
{
    public InsumoService(TenantDbContext context, ILogger<InsumoService> logger) : base(context, logger)
    {
    }

    protected override IQueryable<Insumo> ApplySearch(IQueryable<Insumo> query, string? search)
    {
        if (string.IsNullOrWhiteSpace(search)) return query;
        search = search.ToLower();
        return query.Where(i =>
            EF.Functions.ILike(i.Nome, $"%{search}%") ||
            (i.Descricao != null && EF.Functions.ILike(i.Descricao, $"%{search}%")) ||
            (i.CodigoBarras != null && EF.Functions.ILike(i.CodigoBarras, $"%{search}%")) ||
            (i.Categoria != null && EF.Functions.ILike(i.Categoria.Nome, $"%{search}%")));
    }

    protected override IQueryable<Insumo> ApplySorting(IQueryable<Insumo> query, string? sort, string? order)
    {
        var ascending = string.Equals(order, "asc", StringComparison.OrdinalIgnoreCase);
        return (sort?.ToLower()) switch
        {
            "nome" => ascending ? query.OrderBy(i => i.Nome) : query.OrderByDescending(i => i.Nome),
            "categoria" => ascending ? query.OrderBy(i => i.Categoria.Nome) : query.OrderByDescending(i => i.Categoria.Nome),
            "custo" or "custounitario" => ascending ? query.OrderBy(i => i.CustoUnitario) : query.OrderByDescending(i => i.CustoUnitario),
            "ativo" or "isativo" => ascending ? query.OrderBy(i => i.IsAtivo) : query.OrderByDescending(i => i.IsAtivo),
            _ => query.OrderBy(i => i.Nome)
        };
    }

    public override async Task<PagedResult<InsumoDto>> GetAllAsync(string? search, int page, int pageSize, string? sort, string? order)
    {
        _logger.LogInformation(
            "Buscando {EntityType} - Página: {Page}, Tamanho: {PageSize}, Busca: {Search}",
            typeof(Insumo).Name, page, pageSize, search ?? "N/A");

        var query = _dbSet
            .Include(i => i.Categoria)
            .Include(i => i.UnidadeMedida)
            .AsQueryable();

        query = ApplySearch(query, search);
        query = ApplySorting(query, sort, order);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(MapToDto())
            .ToListAsync();

        _logger.LogInformation("Encontrados {Total} registros de {EntityType}", total, typeof(Insumo).Name);

        return new PagedResult<InsumoDto> { Items = items, Total = total };
    }

    public override async Task<InsumoDto?> GetByIdAsync(long id)
    {
        _logger.LogInformation("Buscando {EntityType} por ID: {Id}", typeof(Insumo).Name, id);

        var entity = await _dbSet
            .Include(i => i.Categoria)
            .Include(i => i.UnidadeMedida)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (entity == null)
        {
            _logger.LogWarning("{EntityType} com ID {Id} não encontrado", typeof(Insumo).Name, id);
            return null;
        }

        var mapper = MapToDto().Compile();
        return mapper(entity);
    }

    protected override Expression<Func<Insumo, InsumoDto>> MapToDto()
    {
        return i => new InsumoDto
        {
            Id = i.Id,
            Nome = i.Nome,
            CategoriaId = i.CategoriaId,
            CategoriaNome = i.Categoria != null ? i.Categoria.Nome : null,
            UnidadeMedidaId = i.UnidadeMedidaId,
            UnidadeMedidaNome = i.UnidadeMedida != null ? i.UnidadeMedida.Nome : null,
            UnidadeMedidaSigla = i.UnidadeMedida != null ? i.UnidadeMedida.Sigla : null,
            CustoUnitario = i.CustoUnitario,
            EstoqueMinimo = i.EstoqueMinimo,
            Descricao = i.Descricao,
            CodigoBarras = i.CodigoBarras,
            PathImagem = i.PathImagem,
            IsAtivo = i.IsAtivo,
            UsuarioAtualizacao = i.UsuarioAtualizacao,
            DataAtualizacao = i.DataAtualizacao
        };
    }

    protected override Insumo MapToEntity(CreateInsumoRequest request)
    {
        return new Insumo
        {
            Nome = request.Nome,
            CategoriaId = request.CategoriaId,
            UnidadeMedidaId = request.UnidadeMedidaId,
            CustoUnitario = request.CustoUnitario,
            EstoqueMinimo = request.EstoqueMinimo,
            Descricao = request.Descricao,
            CodigoBarras = request.CodigoBarras,
            PathImagem = request.PathImagem,
            IsAtivo = request.IsAtivo
        };
    }

    protected override void UpdateEntity(Insumo entity, UpdateInsumoRequest request)
    {
        entity.Nome = request.Nome;
        entity.CategoriaId = request.CategoriaId;
        entity.UnidadeMedidaId = request.UnidadeMedidaId;
        entity.CustoUnitario = request.CustoUnitario;
        entity.EstoqueMinimo = request.EstoqueMinimo;
        entity.Descricao = request.Descricao;
        entity.CodigoBarras = request.CodigoBarras;
        entity.PathImagem = request.PathImagem;
        entity.IsAtivo = request.IsAtivo;
    }

    protected override async Task BeforeCreateAsync(Insumo entity, CreateInsumoRequest request, string? usuarioCriacao)
    {
        // Validar categoria existe
        var categoriaExists = await _context.Set<CategoriaInsumo>().AnyAsync(c => c.Id == request.CategoriaId && c.IsAtivo);
        if (!categoriaExists)
        {
            throw new BusinessException("Categoria inválida ou inativa");
        }

        // Validar unidade de medida existe
        var unidadeExists = await _context.Set<UnidadeMedida>().AnyAsync(u => u.Id == request.UnidadeMedidaId && u.IsAtivo);
        if (!unidadeExists)
        {
            throw new BusinessException("Unidade de medida inválida ou inativa");
        }

        // Validar código de barras único (se informado)
        if (!string.IsNullOrWhiteSpace(request.CodigoBarras))
        {
            var codigoExists = await _context.Set<Insumo>().AnyAsync(i => i.CodigoBarras == request.CodigoBarras);
            if (codigoExists)
            {
                throw new BusinessException("Código de barras já está em uso");
            }
        }
    }

    protected override async Task BeforeUpdateAsync(Insumo entity, UpdateInsumoRequest request, string? usuarioAtualizacao)
    {
        // Validar categoria existe
        var categoriaExists = await _context.Set<CategoriaInsumo>().AnyAsync(c => c.Id == request.CategoriaId && c.IsAtivo);
        if (!categoriaExists)
        {
            throw new BusinessException("Categoria inválida ou inativa");
        }

        // Validar unidade de medida existe
        var unidadeExists = await _context.Set<UnidadeMedida>().AnyAsync(u => u.Id == request.UnidadeMedidaId && u.IsAtivo);
        if (!unidadeExists)
        {
            throw new BusinessException("Unidade de medida inválida ou inativa");
        }

        // Validar código de barras único (se informado)
        if (!string.IsNullOrWhiteSpace(request.CodigoBarras))
        {
            var codigoTaken = await _context.Set<Insumo>().AnyAsync(i => i.CodigoBarras == request.CodigoBarras && i.Id != entity.Id);
            if (codigoTaken)
            {
                throw new BusinessException("Código de barras já está em uso");
            }
        }
    }

    public async Task<bool> CodigoBarrasExistsAsync(string codigoBarras, long? excludeId = null)
    {
        if (string.IsNullOrWhiteSpace(codigoBarras))
            return false;

        var query = _context.Set<Insumo>().Where(i => i.CodigoBarras == codigoBarras);
        if (excludeId.HasValue)
        {
            query = query.Where(i => i.Id != excludeId.Value);
        }
        return await query.AnyAsync();
    }
}

