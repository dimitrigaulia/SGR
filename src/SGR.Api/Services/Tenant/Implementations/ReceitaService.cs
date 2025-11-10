using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SGR.Api.Data;
using SGR.Api.Exceptions;
using SGR.Api.Models.DTOs;
using SGR.Api.Models.Tenant.DTOs;
using SGR.Api.Models.Tenant.Entities;
using SGR.Api.Services.Tenant.Interfaces;

namespace SGR.Api.Services.Tenant.Implementations;

public class ReceitaService : IReceitaService
{
    private readonly TenantDbContext _context;
    private readonly ILogger<ReceitaService> _logger;

    public ReceitaService(TenantDbContext context, ILogger<ReceitaService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PagedResult<ReceitaDto>> GetAllAsync(string? search, int page, int pageSize, string? sort, string? order)
    {
        _logger.LogInformation("Buscando Receitas - Página: {Page}, Tamanho: {PageSize}, Busca: {Search}", page, pageSize, search ?? "N/A");

        var query = _context.Receitas
            .Include(r => r.Categoria)
            .AsQueryable();

        // Aplicar busca
        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.ToLower();
            query = query.Where(r =>
                EF.Functions.ILike(r.Nome, $"%{search}%") ||
                (r.Descricao != null && EF.Functions.ILike(r.Descricao, $"%{search}%")) ||
                (r.Categoria != null && EF.Functions.ILike(r.Categoria.Nome, $"%{search}%")));
        }

        // Aplicar ordenação
        var ascending = string.Equals(order, "asc", StringComparison.OrdinalIgnoreCase);
        query = (sort?.ToLower()) switch
        {
            "nome" => ascending ? query.OrderBy(r => r.Nome) : query.OrderByDescending(r => r.Nome),
            "categoria" => ascending ? query.OrderBy(r => r.Categoria.Nome) : query.OrderByDescending(r => r.Categoria.Nome),
            "rendimento" => ascending ? query.OrderBy(r => r.Rendimento) : query.OrderByDescending(r => r.Rendimento),
            "custototal" or "custo_total" => ascending ? query.OrderBy(r => r.CustoTotal) : query.OrderByDescending(r => r.CustoTotal),
            "custoporporcao" or "custo_por_porcao" => ascending ? query.OrderBy(r => r.CustoPorPorcao) : query.OrderByDescending(r => r.CustoPorPorcao),
            "ativo" or "isativo" => ascending ? query.OrderBy(r => r.IsAtivo) : query.OrderByDescending(r => r.IsAtivo),
            _ => query.OrderBy(r => r.Nome)
        };

        var total = await query.CountAsync();
        var items = await query
            .Skip(Math.Max(0, (page - 1) * pageSize))
            .Take(pageSize)
            .Select(r => new ReceitaDto
            {
                Id = r.Id,
                Nome = r.Nome,
                CategoriaId = r.CategoriaId,
                CategoriaNome = r.Categoria != null ? r.Categoria.Nome : null,
                Descricao = r.Descricao,
                InstrucoesEmpratamento = r.InstrucoesEmpratamento,
                Rendimento = r.Rendimento,
                PesoPorPorcao = r.PesoPorPorcao,
                ToleranciaPeso = r.ToleranciaPeso,
                FatorRendimento = r.FatorRendimento,
                TempoPreparo = r.TempoPreparo,
                Versao = r.Versao,
                CustoTotal = r.CustoTotal,
                CustoPorPorcao = r.CustoPorPorcao,
                PathImagem = r.PathImagem,
                IsAtivo = r.IsAtivo,
                UsuarioCriacao = r.UsuarioCriacao,
                UsuarioAtualizacao = r.UsuarioAtualizacao,
                DataCriacao = r.DataCriacao,
                DataAtualizacao = r.DataAtualizacao,
                Itens = new List<ReceitaItemDto>() // Itens não são carregados na listagem
            })
            .ToListAsync();

        _logger.LogInformation("Encontradas {Total} receitas", total);

        return new PagedResult<ReceitaDto> { Items = items, Total = total };
    }

    public async Task<ReceitaDto?> GetByIdAsync(long id)
    {
        _logger.LogInformation("Buscando Receita por ID: {Id}", id);

        var receita = await _context.Receitas
            .Include(r => r.Categoria)
            .Include(r => r.Itens)
                .ThenInclude(i => i.Insumo)
                    .ThenInclude(ins => ins.UnidadeUso)
            .Include(r => r.Itens)
                .ThenInclude(i => i.Insumo)
                    .ThenInclude(ins => ins.Categoria)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (receita == null)
        {
            _logger.LogWarning("Receita com ID {Id} não encontrada", id);
            return null;
        }

        return MapToDto(receita);
    }

    public async Task<ReceitaDto> CreateAsync(CreateReceitaRequest request, string? usuarioCriacao)
    {
        _logger.LogInformation("Criando nova Receita - Usuário: {Usuario}", usuarioCriacao ?? "Sistema");

        // Validar categoria
        var categoriaExists = await _context.CategoriasReceita.AnyAsync(c => c.Id == request.CategoriaId && c.IsAtivo);
        if (!categoriaExists)
        {
            throw new BusinessException("Categoria inválida ou inativa");
        }

        // Validar itens
        if (request.Itens == null || !request.Itens.Any())
        {
            throw new BusinessException("A receita deve ter pelo menos um item");
        }

        // Validar insumos
        var insumoIds = request.Itens.Select(i => i.InsumoId).Distinct().ToList();
        var insumos = await _context.Insumos
            .Include(i => i.UnidadeUso)
            .Include(i => i.Categoria)
            .Where(i => insumoIds.Contains(i.Id) && i.IsAtivo)
            .ToListAsync();

        if (insumos.Count != insumoIds.Count)
        {
            throw new BusinessException("Um ou mais insumos são inválidos ou estão inativos");
        }

        // Criar receita
        var receita = new Receita
        {
            Nome = request.Nome,
            CategoriaId = request.CategoriaId,
            Descricao = request.Descricao,
            InstrucoesEmpratamento = request.InstrucoesEmpratamento,
            Rendimento = request.Rendimento,
            PesoPorPorcao = request.PesoPorPorcao,
            ToleranciaPeso = request.ToleranciaPeso,
            FatorRendimento = request.FatorRendimento,
            TempoPreparo = request.TempoPreparo,
            Versao = request.Versao ?? "1.0",
            PathImagem = request.PathImagem,
            IsAtivo = request.IsAtivo,
            UsuarioCriacao = usuarioCriacao ?? "Sistema",
            DataCriacao = DateTime.UtcNow
        };

        // Criar itens
        var ordem = 1;
        foreach (var itemRequest in request.Itens.OrderBy(i => i.Ordem))
        {
            var insumo = insumos.First(i => i.Id == itemRequest.InsumoId);
            receita.Itens.Add(new ReceitaItem
            {
                InsumoId = itemRequest.InsumoId,
                Quantidade = itemRequest.Quantidade,
                Ordem = ordem++,
                Observacoes = itemRequest.Observacoes
            });
        }

        // Calcular custos
        CalcularCustos(receita, insumos);

        _context.Receitas.Add(receita);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Receita criada com sucesso - ID: {Id}", receita.Id);

        return await GetByIdAsync(receita.Id) ?? throw new InvalidOperationException("Erro ao buscar receita criada");
    }

    public async Task<ReceitaDto?> UpdateAsync(long id, UpdateReceitaRequest request, string? usuarioAtualizacao)
    {
        _logger.LogInformation("Atualizando Receita - ID: {Id}, Usuário: {Usuario}", id, usuarioAtualizacao ?? "Sistema");

        var receita = await _context.Receitas
            .Include(r => r.Itens)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (receita == null)
        {
            _logger.LogWarning("Receita com ID {Id} não encontrada", id);
            return null;
        }

        // Validar categoria
        var categoriaExists = await _context.CategoriasReceita.AnyAsync(c => c.Id == request.CategoriaId && c.IsAtivo);
        if (!categoriaExists)
        {
            throw new BusinessException("Categoria inválida ou inativa");
        }

        // Validar itens
        if (request.Itens == null || !request.Itens.Any())
        {
            throw new BusinessException("A receita deve ter pelo menos um item");
        }

        // Validar insumos
        var insumoIds = request.Itens.Select(i => i.InsumoId).Distinct().ToList();
        var insumos = await _context.Insumos
            .Include(i => i.UnidadeUso)
            .Include(i => i.Categoria)
            .Where(i => insumoIds.Contains(i.Id) && i.IsAtivo)
            .ToListAsync();

        if (insumos.Count != insumoIds.Count)
        {
            throw new BusinessException("Um ou mais insumos são inválidos ou estão inativos");
        }

        // Atualizar receita
        receita.Nome = request.Nome;
        receita.CategoriaId = request.CategoriaId;
        receita.Descricao = request.Descricao;
        receita.InstrucoesEmpratamento = request.InstrucoesEmpratamento;
        receita.Rendimento = request.Rendimento;
        receita.PesoPorPorcao = request.PesoPorPorcao;
        receita.ToleranciaPeso = request.ToleranciaPeso;
        receita.FatorRendimento = request.FatorRendimento;
        receita.TempoPreparo = request.TempoPreparo;
        receita.Versao = request.Versao ?? "1.0";
        receita.PathImagem = request.PathImagem;
        receita.IsAtivo = request.IsAtivo;
        receita.UsuarioAtualizacao = usuarioAtualizacao;
        receita.DataAtualizacao = DateTime.UtcNow;

        // Remover itens antigos
        _context.ReceitaItens.RemoveRange(receita.Itens);
        receita.Itens.Clear();

        // Adicionar novos itens
        var ordem = 1;
        foreach (var itemRequest in request.Itens.OrderBy(i => i.Ordem))
        {
            receita.Itens.Add(new ReceitaItem
            {
                InsumoId = itemRequest.InsumoId,
                Quantidade = itemRequest.Quantidade,
                Ordem = ordem++,
                Observacoes = itemRequest.Observacoes
            });
        }

        // Recalcular custos
        CalcularCustos(receita, insumos);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Receita atualizada com sucesso - ID: {Id}", id);

        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(long id)
    {
        _logger.LogInformation("Excluindo Receita - ID: {Id}", id);

        var receita = await _context.Receitas.FindAsync(id);
        if (receita == null)
        {
            _logger.LogWarning("Receita com ID {Id} não encontrada", id);
            return false;
        }

        _context.Receitas.Remove(receita);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Receita excluída com sucesso - ID: {Id}", id);
        return true;
    }

    public async Task<ReceitaDto> DuplicarAsync(long id, string novoNome, string? usuarioCriacao)
    {
        _logger.LogInformation("Duplicando Receita - ID: {Id}, Novo Nome: {NovoNome}", id, novoNome);

        var receitaOriginal = await _context.Receitas
            .Include(r => r.Itens)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (receitaOriginal == null)
        {
            throw new BusinessException("Receita não encontrada");
        }

        // Criar nova receita
        var novaReceita = new Receita
        {
            Nome = novoNome,
            CategoriaId = receitaOriginal.CategoriaId,
            Descricao = receitaOriginal.Descricao,
            InstrucoesEmpratamento = receitaOriginal.InstrucoesEmpratamento,
            Rendimento = receitaOriginal.Rendimento,
            PesoPorPorcao = receitaOriginal.PesoPorPorcao,
            ToleranciaPeso = receitaOriginal.ToleranciaPeso,
            FatorRendimento = receitaOriginal.FatorRendimento,
            TempoPreparo = receitaOriginal.TempoPreparo,
            Versao = receitaOriginal.Versao ?? "1.0",
            PathImagem = receitaOriginal.PathImagem,
            IsAtivo = receitaOriginal.IsAtivo,
            UsuarioCriacao = usuarioCriacao ?? "Sistema",
            DataCriacao = DateTime.UtcNow
        };

        // Duplicar itens
        foreach (var itemOriginal in receitaOriginal.Itens.OrderBy(i => i.Ordem))
        {
            novaReceita.Itens.Add(new ReceitaItem
            {
                InsumoId = itemOriginal.InsumoId,
                Quantidade = itemOriginal.Quantidade,
                Ordem = itemOriginal.Ordem,
                Observacoes = itemOriginal.Observacoes
            });
        }

        // Buscar insumos para calcular custos
        var insumoIds = novaReceita.Itens.Select(i => i.InsumoId).Distinct().ToList();
        var insumos = await _context.Insumos
            .Include(i => i.UnidadeUso)
            .Include(i => i.Categoria)
            .Where(i => insumoIds.Contains(i.Id))
            .ToListAsync();

        // Calcular custos
        CalcularCustos(novaReceita, insumos);

        _context.Receitas.Add(novaReceita);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Receita duplicada com sucesso - ID Original: {IdOriginal}, ID Novo: {IdNovo}", id, novaReceita.Id);

        return await GetByIdAsync(novaReceita.Id) ?? throw new InvalidOperationException("Erro ao buscar receita duplicada");
    }

    private void CalcularCustos(Receita receita, List<Insumo> insumos)
    {
        decimal custoTotal = 0;

        foreach (var item in receita.Itens)
        {
            var insumo = insumos.First(i => i.Id == item.InsumoId);
            
            // Quantidade bruta = Quantidade × FatorCorrecao (perdas no preparo do insumo)
            var quantidadeBruta = item.Quantidade * insumo.FatorCorrecao;
            
            // Custo do item = (QuantidadeBruta / QuantidadePorEmbalagem) × CustoUnitario
            var custoItem = (quantidadeBruta / insumo.QuantidadePorEmbalagem) * insumo.CustoUnitario;
            
            custoTotal += custoItem;
        }

        // Aplicar FatorRendimento (perdas no preparo da receita)
        // Se FatorRendimento < 1: há perdas (ex: 0.95 = 5% de perda)
        // Se FatorRendimento > 1: há ganho (raro, mas possível)
        var custoTotalComRendimento = custoTotal / receita.FatorRendimento;
        
        receita.CustoTotal = custoTotalComRendimento;
        receita.CustoPorPorcao = receita.Rendimento > 0 ? custoTotalComRendimento / receita.Rendimento : 0;
    }

    private ReceitaDto MapToDto(Receita receita)
    {
        return new ReceitaDto
        {
            Id = receita.Id,
            Nome = receita.Nome,
            CategoriaId = receita.CategoriaId,
            CategoriaNome = receita.Categoria?.Nome,
            Descricao = receita.Descricao,
            InstrucoesEmpratamento = receita.InstrucoesEmpratamento,
            Rendimento = receita.Rendimento,
            PesoPorPorcao = receita.PesoPorPorcao,
            ToleranciaPeso = receita.ToleranciaPeso,
            FatorRendimento = receita.FatorRendimento,
            TempoPreparo = receita.TempoPreparo,
            Versao = receita.Versao,
            CustoTotal = receita.CustoTotal,
            CustoPorPorcao = receita.CustoPorPorcao,
            PathImagem = receita.PathImagem,
            IsAtivo = receita.IsAtivo,
            UsuarioCriacao = receita.UsuarioCriacao,
            UsuarioAtualizacao = receita.UsuarioAtualizacao,
            DataCriacao = receita.DataCriacao,
            DataAtualizacao = receita.DataAtualizacao,
            Itens = receita.Itens.OrderBy(i => i.Ordem).Select(item =>
            {
                var insumo = item.Insumo;
                var quantidadeBruta = item.Quantidade * insumo.FatorCorrecao;
                var custoItem = (quantidadeBruta / insumo.QuantidadePorEmbalagem) * insumo.CustoUnitario;

                return new ReceitaItemDto
                {
                    Id = item.Id,
                    ReceitaId = item.ReceitaId,
                    InsumoId = item.InsumoId,
                    InsumoNome = insumo.Nome,
                    InsumoCategoriaNome = insumo.Categoria?.Nome,
                    UnidadeUsoNome = insumo.UnidadeUso?.Nome,
                    UnidadeUsoSigla = insumo.UnidadeUso?.Sigla,
                    Quantidade = item.Quantidade,
                    QuantidadeBruta = quantidadeBruta,
                    CustoItem = custoItem,
                    Ordem = item.Ordem,
                    Observacoes = item.Observacoes
                };
            }).ToList()
        };
    }
}

