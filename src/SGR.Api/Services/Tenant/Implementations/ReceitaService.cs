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

    private static decimal CalcularQuantidadeBruta(ReceitaItem item, Insumo insumo)
    {
        // Quantidade bruta = Quantidade Ã— FatorCorrecao (perdas no preparo do insumo)
        return item.Quantidade * insumo.FatorCorrecao;
    }

    private static decimal CalcularFatorRendimentoFromIc(decimal fatorRendimentoRequest, string? icSinal, int? icValor)
    {
        if (!string.IsNullOrWhiteSpace(icSinal) && icValor.HasValue)
        {
            var sinal = icSinal.Trim();
            var v = Math.Clamp(icValor.Value, 0, 999);
            var delta = v / 100m;

            decimal fator = sinal == "-" ? 1m - delta : 1m + delta;
            if (fator < 0.01m) fator = 0.01m;
            if (fator > 10m) fator = 10m;
            return fator;
        }

        return fatorRendimentoRequest <= 0 ? 1.0m : fatorRendimentoRequest;
    }

    /// <summary>
    /// Calcula o custo por unidade de uso de um insumo
    /// FÃ³rmula: (CustoUnitario / QuantidadePorEmbalagem) * FatorCorrecao
    /// </summary>
    private static decimal CalcularCustoPorUnidadeUso(Insumo insumo)
    {
        if (insumo.QuantidadePorEmbalagem <= 0)
        {
            return 0;
        }

        return (insumo.CustoUnitario / insumo.QuantidadePorEmbalagem) * insumo.FatorCorrecao;
    }

    private decimal CalcularCustoItem(ReceitaItem item, Insumo insumo)
    {
        // Quantidade bruta = Quantidade Ã— FatorCorrecao (perdas no preparo do insumo)
        var quantidadeBruta = CalcularQuantidadeBruta(item, insumo);
        
        // Custo por unidade de uso
        var custoPorUnidadeUso = CalcularCustoPorUnidadeUso(insumo);
        
        // Custo do item = QuantidadeBruta Ã— CustoPorUnidadeUso
        // IMPORTANTE: ExibirComoQB Ã© apenas visual, sempre usar Quantidade numÃ©rica para cÃ¡lculos
        return quantidadeBruta * custoPorUnidadeUso;
    }

    public async Task<PagedResult<ReceitaDto>> GetAllAsync(string? search, int page, int pageSize, string? sort, string? order)
    {
        _logger.LogInformation("Buscando Receitas - PÃ¡gina: {Page}, Tamanho: {PageSize}, Busca: {Search}", page, pageSize, search ?? "N/A");

        // Usar AsNoTracking para queries de leitura (melhor performance)
        var query = _context.Receitas
            .AsNoTracking()
            .Include(r => r.Categoria)
            .Include(r => r.Itens)
                .ThenInclude(i => i.Insumo)
                    .ThenInclude(i => i.Categoria)
            .Include(r => r.Itens)
                .ThenInclude(i => i.Insumo)
                    .ThenInclude(i => i.UnidadeUso)
            .Include(r => r.Itens)
                .ThenInclude(i => i.UnidadeMedida)
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

        // Aplicar ordenaÃ§Ã£o
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
                Itens = new List<ReceitaItemDto>() // Itens nÃ£o sÃ£o carregados na listagem
            })
            .ToListAsync();

        _logger.LogInformation("Encontradas {Total} receitas", total);

        return new PagedResult<ReceitaDto> { Items = items, Total = total };
    }

    public async Task<ReceitaDto?> GetByIdAsync(long id)
    {
        _logger.LogInformation("Buscando Receita por ID: {Id}", id);

        // Usar AsNoTracking para queries de leitura (melhor performance)
        var receita = await _context.Receitas
            .AsNoTracking()
            .Include(r => r.Categoria)
            .Include(r => r.Itens)
                .ThenInclude(i => i.Insumo)
                    .ThenInclude(ins => ins.UnidadeUso)
            .Include(r => r.Itens)
                .ThenInclude(i => i.Insumo)
                    .ThenInclude(ins => ins.UnidadeCompra)
            .Include(r => r.Itens)
                .ThenInclude(i => i.Insumo)
                    .ThenInclude(ins => ins.Categoria)
            .Include(r => r.Itens)
                .ThenInclude(i => i.UnidadeMedida)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (receita == null)
        {
            _logger.LogWarning("Receita com ID {Id} nÃ£o encontrada", id);
            return null;
        }

        return MapToDto(receita);
    }

    public async Task<ReceitaDto> CreateAsync(CreateReceitaRequest request, string? usuarioCriacao)
    {
        _logger.LogInformation("Criando nova Receita - UsuÃ¡rio: {Usuario}", usuarioCriacao ?? "Sistema");

        // Validar categoria
            var categoriaExists = await _context.CategoriasReceita.AnyAsync(c => c.Id == request.CategoriaId && c.IsAtivo);
            if (!categoriaExists)
            {
                throw new BusinessException("Categoria invÃ¡lida ou inativa");
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
                .Include(i => i.UnidadeCompra)
                .Include(i => i.Categoria)
                .Where(i => insumoIds.Contains(i.Id) && i.IsAtivo)
                .ToListAsync();

            if (insumos.Count != insumoIds.Count)
            {
                throw new BusinessException("Um ou mais insumos sÃ£o invÃ¡lidos ou estÃ£o inativos");
            }

            // Criar receita
            var fatorRendimento = CalcularFatorRendimentoFromIc(request.FatorRendimento, request.IcSinal, request.IcValor);

            var receita = new Receita
            {
                Nome = request.Nome,
                CategoriaId = request.CategoriaId,
                Descricao = request.Descricao,
                Conservacao = request.Conservacao,
                InstrucoesEmpratamento = request.InstrucoesEmpratamento,
                Rendimento = request.Rendimento,
                PesoPorPorcao = request.PesoPorPorcao,
                FatorRendimento = fatorRendimento,
                TempoPreparo = request.TempoPreparo,
                Versao = request.Versao ?? "1.0",
                PathImagem = request.PathImagem,
                IsAtivo = request.IsAtivo,
                UsuarioCriacao = usuarioCriacao ?? "Sistema",
                DataCriacao = DateTime.UtcNow
            };

            // Validar unidades de medida
            var unidadeMedidaIds = request.Itens.Select(i => i.UnidadeMedidaId).Distinct().ToList();
            var unidadesMedida = await _context.UnidadesMedida
                .Where(u => unidadeMedidaIds.Contains(u.Id) && u.IsAtivo)
                .ToListAsync();

            if (unidadesMedida.Count != unidadeMedidaIds.Count)
            {
                throw new BusinessException("Uma ou mais unidades de medida sÃ£o invÃ¡lidas ou estÃ£o inativas");
            }

            // Criar itens
            var ordem = 1;
            foreach (var itemRequest in request.Itens.OrderBy(i => i.Ordem))
            {
                var insumo = insumos.First(i => i.Id == itemRequest.InsumoId);
                receita.Itens.Add(new ReceitaItem
                {
                    InsumoId = itemRequest.InsumoId,
                    Quantidade = itemRequest.Quantidade,
                    UnidadeMedidaId = itemRequest.UnidadeMedidaId,
                    ExibirComoQB = itemRequest.ExibirComoQB,
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
        _logger.LogInformation("Atualizando Receita - ID: {Id}, UsuÃ¡rio: {Usuario}", id, usuarioAtualizacao ?? "Sistema");

        var receita = await _context.Receitas
                .Include(r => r.Itens)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (receita == null)
            {
                _logger.LogWarning("Receita com ID {Id} nÃ£o encontrada", id);
                return null;
            }

            // Validar categoria
            var categoriaExists = await _context.CategoriasReceita.AnyAsync(c => c.Id == request.CategoriaId && c.IsAtivo);
            if (!categoriaExists)
            {
                throw new BusinessException("Categoria invÃ¡lida ou inativa");
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
                .Include(i => i.UnidadeCompra)
                .Include(i => i.Categoria)
                .Where(i => insumoIds.Contains(i.Id) && i.IsAtivo)
                .ToListAsync();

            if (insumos.Count != insumoIds.Count)
            {
                throw new BusinessException("Um ou mais insumos sÃ£o invÃ¡lidos ou estÃ£o inativos");
            }

            // Atualizar receita
            receita.Nome = request.Nome;
            receita.CategoriaId = request.CategoriaId;
            receita.Descricao = request.Descricao;
            receita.Conservacao = request.Conservacao;
            receita.InstrucoesEmpratamento = request.InstrucoesEmpratamento;
            receita.Rendimento = request.Rendimento;
            receita.PesoPorPorcao = request.PesoPorPorcao;
            receita.FatorRendimento = CalcularFatorRendimentoFromIc(request.FatorRendimento, request.IcSinal, request.IcValor);
            receita.TempoPreparo = request.TempoPreparo;
            receita.Versao = request.Versao ?? "1.0";
            receita.PathImagem = request.PathImagem;
            receita.IsAtivo = request.IsAtivo;
            receita.UsuarioAtualizacao = usuarioAtualizacao;
            receita.DataAtualizacao = DateTime.UtcNow;

            // Remover itens antigos
            // NÃ£o usar Clear() pois RemoveRange jÃ¡ remove do contexto
            // Apenas remover do contexto, a coleÃ§Ã£o serÃ¡ atualizada automaticamente
            var itensParaRemover = receita.Itens.ToList();
            _context.ReceitaItens.RemoveRange(itensParaRemover);

            // Adicionar novos itens
            var ordem = 1;
            foreach (var itemRequest in request.Itens.OrderBy(i => i.Ordem))
            {
                receita.Itens.Add(new ReceitaItem
                {
                    InsumoId = itemRequest.InsumoId,
                    Quantidade = itemRequest.Quantidade,
                    UnidadeMedidaId = itemRequest.UnidadeMedidaId,
                    ExibirComoQB = itemRequest.ExibirComoQB,
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

        // Usar FirstOrDefaultAsync ao invÃ©s de FindAsync para garantir que respeita o schema do tenant
        var receita = await _context.Receitas.FirstOrDefaultAsync(r => r.Id == id);
        if (receita == null)
        {
            _logger.LogWarning("Receita com ID {Id} nÃ£o encontrada", id);
            return false;
        }

        _context.Receitas.Remove(receita);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Receita excluÃ­da com sucesso - ID: {Id}", id);
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
                throw new BusinessException("Receita nÃ£o encontrada");
            }

            // Criar nova receita
            var novaReceita = new Receita
            {
                Nome = novoNome,
                CategoriaId = receitaOriginal.CategoriaId,
                Descricao = receitaOriginal.Descricao,
                Conservacao = receitaOriginal.Conservacao,
                InstrucoesEmpratamento = receitaOriginal.InstrucoesEmpratamento,
                Rendimento = receitaOriginal.Rendimento,
                PesoPorPorcao = receitaOriginal.PesoPorPorcao,
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
                .Include(i => i.UnidadeCompra)
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
            
            // Quantidade bruta = Quantidade Ã— FatorCorrecao (perdas no preparo do insumo)
            var quantidadeBruta = item.Quantidade * insumo.FatorCorrecao;
            
            // Custo do item = (QuantidadeBruta / QuantidadePorEmbalagem) Ã— CustoUnitario
            var custoItem = CalcularCustoItem(item, insumo);
            
            custoTotal += custoItem;
        }

        // Aplicar FatorRendimento (perdas no preparo da receita)
        // Se FatorRendimento < 1: hÃ¡ perdas (ex: 0.95 = 5% de perda)
        // Se FatorRendimento > 1: hÃ¡ ganho (raro, mas possÃ­vel)
        // Proteger divisÃ£o por zero
        var fatorRendimento = receita.FatorRendimento > 0 ? receita.FatorRendimento : 1.0m;
        var custoTotalComRendimento = custoTotal / fatorRendimento;
        
        receita.CustoTotal = custoTotalComRendimento;
        receita.CustoPorPorcao = receita.Rendimento > 0 ? custoTotalComRendimento / receita.Rendimento : 0;
    }

    private decimal? CalcularCustoUnitarioUso(Insumo insumo)
    {
        if (insumo.QuantidadePorEmbalagem <= 0 || insumo.CustoUnitario <= 0)
        {
            return null;
        }

        return CalcularCustoPorUnidadeUso(insumo);
    }

    private ReceitaDto MapToDto(Receita receita)
    {
        string? icSinal = null;
        int? icValor = null;

        if (receita.FatorRendimento > 0)
        {
            if (receita.FatorRendimento >= 1m)
            {
                icSinal = "+";
                icValor = (int)Math.Round((receita.FatorRendimento - 1m) * 100m);
            }
            else
            {
                icSinal = "-";
                icValor = (int)Math.Round((1m - receita.FatorRendimento) * 100m);
            }
        }

        return new ReceitaDto
        {
            Id = receita.Id,
            Nome = receita.Nome,
            CategoriaId = receita.CategoriaId,
            CategoriaNome = receita.Categoria?.Nome,
            Conservacao = receita.Conservacao,
            Descricao = receita.Descricao,
            InstrucoesEmpratamento = receita.InstrucoesEmpratamento,
            Rendimento = receita.Rendimento,
            PesoPorPorcao = receita.PesoPorPorcao,
            FatorRendimento = receita.FatorRendimento,
            IcSinal = icSinal,
            IcValor = icValor,
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
                var custoItem = CalcularCustoItem(item, insumo);
                var custoPorUnidadeUso = CalcularCustoUnitarioUso(insumo);
                decimal? custoPor100UnidadesUso = null;

                if (custoPorUnidadeUso.HasValue)
                {
                    // Para peso/volume, Ã© comum visualizar custo por 100 g / 100 mL
                    // Verificar pela sigla se Ã© GR (grama) ou ML (mililitro)
                    var sigla = insumo.UnidadeUso?.Sigla?.ToUpper();
                    if (sigla == "GR" || sigla == "ML")
                    {
                        custoPor100UnidadesUso = custoPorUnidadeUso.Value * 100m;
                    }
                }

                return new ReceitaItemDto
                {
                    Id = item.Id,
                    ReceitaId = item.ReceitaId,
                    InsumoId = item.InsumoId,
                    InsumoNome = insumo.Nome,
                    InsumoCategoriaNome = insumo.Categoria?.Nome,
                    UnidadeMedidaId = item.UnidadeMedidaId,
                    UnidadeMedidaNome = item.UnidadeMedida?.Nome,
                    UnidadeMedidaSigla = item.UnidadeMedida?.Sigla,
                    Quantidade = item.Quantidade,
                    QuantidadeBruta = quantidadeBruta,
                    CustoItem = custoItem,
                    CustoPorUnidadeUso = custoPorUnidadeUso,
                    CustoPor100UnidadesUso = custoPor100UnidadesUso,
                    ExibirComoQB = item.ExibirComoQB,
                    Ordem = item.Ordem,
                    Observacoes = item.Observacoes
                };
            }).ToList()
        };
    }
}
