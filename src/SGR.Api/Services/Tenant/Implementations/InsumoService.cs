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

    private static decimal CalcularFatorCorrecao(decimal fatorCorrecaoRequest, int? ipcValor)
    {
        if (ipcValor.HasValue)
        {
            var v = Math.Clamp(ipcValor.Value, 0, 999);
            return 1m + (v / 100m);
        }

        return fatorCorrecaoRequest <= 0 ? 1.0m : fatorCorrecaoRequest;
    }

    protected override IQueryable<Insumo> ApplySearch(IQueryable<Insumo> query, string? search)
    {
        if (string.IsNullOrWhiteSpace(search)) return query;
        search = search.ToLower();
        return query.Where(i =>
            EF.Functions.ILike(i.Nome, $"%{search}%") ||
            (i.Descricao != null && EF.Functions.ILike(i.Descricao, $"%{search}%")) ||
            (i.Categoria != null && EF.Functions.ILike(i.Categoria.Nome, $"%{search}%")) ||
            (i.UnidadeCompra != null && EF.Functions.ILike(i.UnidadeCompra.Nome, $"%{search}%")) ||
            (i.UnidadeUso != null && EF.Functions.ILike(i.UnidadeUso.Nome, $"%{search}%")));
    }

    protected override IQueryable<Insumo> ApplySorting(IQueryable<Insumo> query, string? sort, string? order)
    {
        var ascending = string.Equals(order, "asc", StringComparison.OrdinalIgnoreCase);
        return (sort?.ToLower()) switch
        {
            "nome" => ascending ? query.OrderBy(i => i.Nome) : query.OrderByDescending(i => i.Nome),
            "categoria" => ascending ? query.OrderBy(i => i.Categoria.Nome) : query.OrderByDescending(i => i.Categoria.Nome),
            "unidadecompra" or "unidade_compra" => ascending ? query.OrderBy(i => i.UnidadeCompra.Nome) : query.OrderByDescending(i => i.UnidadeCompra.Nome),
            "unidadeuso" or "unidade_uso" => ascending ? query.OrderBy(i => i.UnidadeUso.Nome) : query.OrderByDescending(i => i.UnidadeUso.Nome),
            "custo" or "custounitario" => ascending ? query.OrderBy(i => i.CustoUnitario) : query.OrderByDescending(i => i.CustoUnitario),
            "ativo" or "isativo" => ascending ? query.OrderBy(i => i.IsAtivo) : query.OrderByDescending(i => i.IsAtivo),
            _ => query.OrderBy(i => i.Nome)
        };
    }

    public override async Task<PagedResult<InsumoDto>> GetAllAsync(string? search, int page, int pageSize, string? sort, string? order)
    {
        _logger.LogInformation(
            "Buscando {EntityType} - PÃ¡gaina: {Page}, Tamanho: {PageSize}, Busca: {Search}",
            typeof(Insumo).Name, page, pageSize, search ?? "N/A");

        var query = _dbSet
            .Include(i => i.Categoria)
            .Include(i => i.UnidadeCompra)
            .Include(i => i.UnidadeUso)
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
            .Include(i => i.UnidadeCompra)
            .Include(i => i.UnidadeUso)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (entity == null)
        {
            _logger.LogWarning("{EntityType} com ID {Id} nÃ£o encontrado", typeof(Insumo).Name, id);
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
            UnidadeCompraId = i.UnidadeCompraId,
            UnidadeCompraNome = i.UnidadeCompra != null ? i.UnidadeCompra.Nome : null,
            UnidadeCompraSigla = i.UnidadeCompra != null ? i.UnidadeCompra.Sigla : null,
            UnidadeUsoId = i.UnidadeUsoId,
            UnidadeUsoNome = i.UnidadeUso != null ? i.UnidadeUso.Nome : null,
            UnidadeUsoSigla = i.UnidadeUso != null ? i.UnidadeUso.Sigla : null,
            QuantidadePorEmbalagem = i.QuantidadePorEmbalagem,
            CustoUnitario = i.CustoUnitario,
            FatorCorrecao = i.FatorCorrecao,
            IpcValor = i.FatorCorrecao >= 1m ? (int?)Math.Round((i.FatorCorrecao - 1m) * 100m) : null,
            Descricao = i.Descricao,
            PathImagem = i.PathImagem,
            IsAtivo = i.IsAtivo,
            UsuarioAtualizacao = i.UsuarioAtualizacao,
            DataAtualizacao = i.DataAtualizacao
        };
    }

    protected override Insumo MapToEntity(CreateInsumoRequest request)
    {
        var fatorCorrecao = CalcularFatorCorrecao(request.FatorCorrecao, request.IpcValor);

        return new Insumo
        {
            Nome = request.Nome,
            CategoriaId = request.CategoriaId,
            UnidadeCompraId = request.UnidadeCompraId,
            UnidadeUsoId = request.UnidadeUsoId,
            QuantidadePorEmbalagem = request.QuantidadePorEmbalagem,
            CustoUnitario = request.CustoUnitario,
            FatorCorrecao = fatorCorrecao,
            Descricao = request.Descricao,
            PathImagem = request.PathImagem,
            IsAtivo = request.IsAtivo
        };
    }

    protected override void UpdateEntity(Insumo entity, UpdateInsumoRequest request)
    {
        var fatorCorrecao = CalcularFatorCorrecao(request.FatorCorrecao, request.IpcValor);

        entity.Nome = request.Nome;
        entity.CategoriaId = request.CategoriaId;
        entity.UnidadeCompraId = request.UnidadeCompraId;
        entity.UnidadeUsoId = request.UnidadeUsoId;
        entity.QuantidadePorEmbalagem = request.QuantidadePorEmbalagem;
        entity.CustoUnitario = request.CustoUnitario;
        entity.FatorCorrecao = fatorCorrecao;
        entity.Descricao = request.Descricao;
        entity.PathImagem = request.PathImagem;
        entity.IsAtivo = request.IsAtivo;
    }

    protected override async Task BeforeCreateAsync(Insumo entity, CreateInsumoRequest request, string? usuarioCriacao)
    {
        // Validar categoria existe
        var categoriaExists = await _context.Set<CategoriaInsumo>().AnyAsync(c => c.Id == request.CategoriaId && c.IsAtivo);
        if (!categoriaExists)
        {
            throw new BusinessException("Categoria invÃ¡lida ou inativa");
        }

        // Validar unidades de medida existem
        var unidadeCompraExists = await _context.Set<UnidadeMedida>().AnyAsync(u => u.Id == request.UnidadeCompraId && u.IsAtivo);
        if (!unidadeCompraExists)
        {
            throw new BusinessException("Unidade de compra invÃ¡lida ou inativa");
        }

        var unidadeUsoExists = await _context.Set<UnidadeMedida>().AnyAsync(u => u.Id == request.UnidadeUsoId && u.IsAtivo);
        if (!unidadeUsoExists)
        {
            throw new BusinessException("Unidade de uso invÃ¡lida ou inativa");
        }
    }

    protected override async Task BeforeUpdateAsync(Insumo entity, UpdateInsumoRequest request, string? usuarioAtualizacao)
    {
        // Validar categoria existe
        var categoriaExists = await _context.Set<CategoriaInsumo>().AnyAsync(c => c.Id == request.CategoriaId && c.IsAtivo);
        if (!categoriaExists)
        {
            throw new BusinessException("Categoria invÃ¡lida ou inativa");
        }

        // Validar unidades de medida existem
        var unidadeCompraExists = await _context.Set<UnidadeMedida>().AnyAsync(u => u.Id == request.UnidadeCompraId && u.IsAtivo);
        if (!unidadeCompraExists)
        {
            throw new BusinessException("Unidade de compra invÃ¡lida ou inativa");
        }

        var unidadeUsoExists = await _context.Set<UnidadeMedida>().AnyAsync(u => u.Id == request.UnidadeUsoId && u.IsAtivo);
        if (!unidadeUsoExists)
        {
            throw new BusinessException("Unidade de uso invÃ¡lida ou inativa");
        }
    }
}
