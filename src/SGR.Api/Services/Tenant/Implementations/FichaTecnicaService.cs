using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SGR.Api.Data;
using SGR.Api.Exceptions;
using SGR.Api.Models.DTOs;
using SGR.Api.Models.Tenant.DTOs;
using SGR.Api.Models.Tenant.Entities;
using SGR.Api.Services.Tenant.Interfaces;

namespace SGR.Api.Services.Tenant.Implementations;

public class FichaTecnicaService : IFichaTecnicaService
{
    private readonly TenantDbContext _context;
    private readonly ILogger<FichaTecnicaService> _logger;

    public FichaTecnicaService(TenantDbContext context, ILogger<FichaTecnicaService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Calcula o custo por unidade de uso de um insumo
    /// Fórmula: (CustoUnitario / QuantidadePorEmbalagem) * FatorCorrecao
    /// </summary>
    private static decimal CalcularCustoPorUnidadeUso(Insumo insumo)
    {
        if (insumo.QuantidadePorEmbalagem <= 0)
        {
            return 0;
        }

        return (insumo.CustoUnitario / insumo.QuantidadePorEmbalagem) * insumo.FatorCorrecao;
    }

    /// <summary>
    /// Calcula os custos da ficha técnica
    /// </summary>
    private void CalcularCustosFichaTecnica(FichaTecnica ficha, List<Insumo> insumos, List<Receita> receitas)
    {
        decimal custoTotal = 0;

        foreach (var item in ficha.Itens)
        {
            decimal custoLinha = 0;

            if (item.TipoItem == "Insumo" && item.InsumoId.HasValue)
            {
                var insumo = insumos.FirstOrDefault(i => i.Id == item.InsumoId.Value);
                if (insumo != null)
                {
                    var custoPorUnidadeUso = CalcularCustoPorUnidadeUso(insumo);
                    // IMPORTANTE: ExibirComoQB é apenas visual, sempre usar Quantidade numérica
                    custoLinha = item.Quantidade * custoPorUnidadeUso;
                }
            }
            else if (item.TipoItem == "Receita" && item.ReceitaId.HasValue)
            {
                var receita = receitas.FirstOrDefault(r => r.Id == item.ReceitaId.Value);
                if (receita != null)
                {
                    // Usar CustoPorPorcao da receita como custo unitário
                    // IMPORTANTE: ExibirComoQB é apenas visual, sempre usar Quantidade numérica
                    custoLinha = item.Quantidade * receita.CustoPorPorcao;
                }
            }

            custoTotal += custoLinha;
        }

        ficha.CustoTotal = Math.Round(custoTotal, 4);
        
        // Proteger divisão por zero/nulo
        if (ficha.RendimentoFinal.HasValue && ficha.RendimentoFinal.Value > 0)
        {
            ficha.CustoPorUnidade = Math.Round(custoTotal / ficha.RendimentoFinal.Value, 4);
        }
        else
        {
            ficha.CustoPorUnidade = 0m; // Definir como 0 quando RendimentoFinal não estiver disponível
        }
    }

    /// <summary>
    /// Calcula o rendimento final da ficha técnica considerando apenas itens em GR (gramas)
    /// </summary>
    private void CalcularRendimentoFinal(FichaTecnica ficha, List<UnidadeMedida> unidadesMedida)
    {
        // Considerar apenas itens cuja UnidadeMedida.Sigla seja "GR"
        var quantidadeTotalBase = 0m;

        foreach (var item in ficha.Itens)
        {
            var unidadeMedida = unidadesMedida.FirstOrDefault(u => u.Id == item.UnidadeMedidaId);
            if (unidadeMedida != null && unidadeMedida.Sigla.ToUpper() == "GR")
            {
                // IMPORTANTE: ExibirComoQB é apenas visual, sempre usar Quantidade numérica
                quantidadeTotalBase += item.Quantidade;
            }
        }

        // Aplicar IC (Índice de Cocção)
        decimal pesoAposCoccao = quantidadeTotalBase;
        if (ficha.ICOperador.HasValue && ficha.ICValor.HasValue)
        {
            var icValor = Math.Clamp(ficha.ICValor.Value, 0, 9999);
            var icPercentual = icValor / 100m;

            if (ficha.ICOperador == '+')
            {
                pesoAposCoccao = quantidadeTotalBase * (1 + icPercentual);
            }
            else if (ficha.ICOperador == '-')
            {
                pesoAposCoccao = quantidadeTotalBase * (1 - icPercentual);
            }
        }

        // Aplicar IPC (Índice de Partes Comestíveis)
        decimal pesoComestivel = pesoAposCoccao;
        if (ficha.IPCValor.HasValue)
        {
            var ipcValor = Math.Clamp(ficha.IPCValor.Value, 0, 999);
            var ipcPercentual = ipcValor / 100m;
            pesoComestivel = pesoAposCoccao * ipcPercentual;
        }

        ficha.RendimentoFinal = pesoComestivel;
    }

    /// <summary>
    /// Calcula o preço sugerido de venda
    /// PrecoSugeridoVenda = CustoPorUnidade * IndiceContabil
    /// Só calcula se CustoPorUnidade for válido (RendimentoFinal > 0)
    /// </summary>
    private void CalcularPrecoSugerido(FichaTecnica ficha)
    {
        // Não calcular se RendimentoFinal não estiver disponível (CustoPorUnidade seria inválido)
        if (!ficha.RendimentoFinal.HasValue || ficha.RendimentoFinal.Value <= 0)
        {
            ficha.PrecoSugeridoVenda = null;
            return;
        }

        if (ficha.IndiceContabil.HasValue && ficha.IndiceContabil.Value > 0 && ficha.CustoPorUnidade > 0)
        {
            ficha.PrecoSugeridoVenda = Math.Round(ficha.CustoPorUnidade * ficha.IndiceContabil.Value, 4);
        }
        else
        {
            ficha.PrecoSugeridoVenda = null;
        }
    }

    /// <summary>
    /// Calcula os preços dos canais
    /// </summary>
    private void CalcularPrecosCanais(FichaTecnica ficha)
    {
        var precoBase = ficha.PrecoSugeridoVenda ?? 0m;

        foreach (var canal in ficha.Canais)
        {
            // Se PrecoSugeridoVenda não for válido, zerar preço e margem para não ficar com valor antigo
            if (precoBase <= 0)
            {
                canal.PrecoVenda = 0m;
                canal.MargemCalculadaPercentual = null;
                continue;
            }

            // Tratar TaxaPercentual null como 0% (vende pelo preço base)
            var taxaPercentual = canal.TaxaPercentual ?? 0m;
            var taxa = taxaPercentual / 100m;
            canal.PrecoVenda = Math.Round(precoBase * (1 + taxa), 4);

            // Calcular margem
            if (canal.PrecoVenda > 0 && ficha.CustoPorUnidade > 0)
            {
                var taxaMargem = (canal.TaxaPercentual ?? 0m) / 100m;
                var comissao = (canal.ComissaoPercentual ?? 0m) / 100m;
                var custoComTaxas = ficha.CustoPorUnidade * (1 + taxaMargem + comissao);
                var margem = (canal.PrecoVenda - custoComTaxas) / canal.PrecoVenda * 100m;
                canal.MargemCalculadaPercentual = Math.Round(margem, 2);
            }
            else
            {
                canal.MargemCalculadaPercentual = null;
            }
        }
    }

    public async Task<PagedResult<FichaTecnicaDto>> GetAllAsync(string? search, int page, int pageSize, string? sort, string? order)
    {
        _logger.LogInformation("Buscando Fichas Técnicas - Página: {Page}, Tamanho: {PageSize}, Busca: {Search}", page, pageSize, search ?? "N/A");

        var query = _context.FichasTecnicas
            .Include(f => f.Categoria)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.ToLower();
            query = query.Where(f =>
                EF.Functions.ILike(f.Nome, $"%{search}%") ||
                (f.Codigo != null && EF.Functions.ILike(f.Codigo, $"%{search}%")) ||
                (f.Categoria != null && EF.Functions.ILike(f.Categoria.Nome, $"%{search}%")));
        }

        var ascending = string.Equals(order, "asc", StringComparison.OrdinalIgnoreCase);
        query = (sort?.ToLower()) switch
        {
            "nome" => ascending ? query.OrderBy(f => f.Nome) : query.OrderByDescending(f => f.Nome),
            "codigo" => ascending ? query.OrderBy(f => f.Codigo) : query.OrderByDescending(f => f.Codigo),
            "categoria" => ascending ? query.OrderBy(f => f.Categoria.Nome) : query.OrderByDescending(f => f.Categoria.Nome),
            "ativo" or "isativo" => ascending ? query.OrderBy(f => f.IsAtivo) : query.OrderByDescending(f => f.IsAtivo),
            _ => query.OrderBy(f => f.Nome)
        };

        var total = await query.CountAsync();
        var items = await query
            .Skip(Math.Max(0, (page - 1) * pageSize))
            .Take(pageSize)
            .Include(f => f.Categoria)
            .Include(f => f.Canais)
            .ToListAsync();

        var list = items.Select(MapToDto).ToList();

        return new PagedResult<FichaTecnicaDto>
        {
            Items = list,
            Total = total
        };
    }

    public async Task<FichaTecnicaDto?> GetByIdAsync(long id)
    {
        _logger.LogInformation("Buscando Ficha Técnica por ID: {Id}", id);

        var ficha = await _context.FichasTecnicas
            .Include(f => f.Categoria)
            .Include(f => f.Itens)
                .ThenInclude(i => i.Receita)
            .Include(f => f.Itens)
                .ThenInclude(i => i.Insumo)
            .Include(f => f.Itens)
                .ThenInclude(i => i.UnidadeMedida)
            .Include(f => f.Canais)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (ficha == null)
        {
            _logger.LogWarning("Ficha Técnica com ID {Id} não encontrada", id);
            return null;
        }

        return MapToDto(ficha);
    }

    public async Task<FichaTecnicaDto> CreateAsync(CreateFichaTecnicaRequest request, string? usuarioCriacao)
    {
        _logger.LogInformation("Criando nova Ficha Técnica - Usuário: {Usuario}", usuarioCriacao ?? "Sistema");

        // Validar categoria
        var categoriaExists = await _context.CategoriasReceita.AnyAsync(c => c.Id == request.CategoriaId && c.IsAtivo);
        if (!categoriaExists)
        {
            throw new BusinessException("Categoria inválida ou inativa");
        }

        // Validar itens
        if (request.Itens == null || !request.Itens.Any())
        {
            throw new BusinessException("A ficha técnica deve ter pelo menos um item");
        }

        // Validar itens
        foreach (var item in request.Itens)
        {
            if (item.TipoItem == "Receita" && !item.ReceitaId.HasValue)
            {
                throw new BusinessException("ReceitaId é obrigatório quando TipoItem é 'Receita'");
            }
            if (item.TipoItem == "Insumo" && !item.InsumoId.HasValue)
            {
                throw new BusinessException("InsumoId é obrigatório quando TipoItem é 'Insumo'");
            }
            if (item.TipoItem != "Receita" && item.TipoItem != "Insumo")
            {
                throw new BusinessException("TipoItem deve ser 'Receita' ou 'Insumo'");
            }
        }

        // Validar receitas e insumos
        var receitaIds = request.Itens.Where(i => i.TipoItem == "Receita" && i.ReceitaId.HasValue).Select(i => i.ReceitaId!.Value).Distinct().ToList();
        var insumoIds = request.Itens.Where(i => i.TipoItem == "Insumo" && i.InsumoId.HasValue).Select(i => i.InsumoId!.Value).Distinct().ToList();
        var unidadeMedidaIds = request.Itens.Select(i => i.UnidadeMedidaId).Distinct().ToList();

        var receitas = await _context.Receitas
            .Where(r => receitaIds.Contains(r.Id) && r.IsAtivo)
            .ToListAsync();

        if (receitas.Count != receitaIds.Count)
        {
            throw new BusinessException("Uma ou mais receitas são inválidas ou estão inativas");
        }

        var insumos = await _context.Insumos
            .Where(i => insumoIds.Contains(i.Id) && i.IsAtivo)
            .ToListAsync();

        if (insumos.Count != insumoIds.Count)
        {
            throw new BusinessException("Um ou mais insumos são inválidos ou estão inativos");
        }

        var unidadesMedida = await _context.UnidadesMedida
            .Where(u => unidadeMedidaIds.Contains(u.Id) && u.IsAtivo)
            .ToListAsync();

        if (unidadesMedida.Count != unidadeMedidaIds.Count)
        {
            throw new BusinessException("Uma ou mais unidades de medida são inválidas ou estão inativas");
        }

        var ficha = new FichaTecnica
        {
            CategoriaId = request.CategoriaId,
            Nome = request.Nome,
            Codigo = request.Codigo,
            DescricaoComercial = request.DescricaoComercial,
            IndiceContabil = request.IndiceContabil,
            ICOperador = request.ICOperador,
            ICValor = request.ICValor,
            IPCValor = request.IPCValor,
            MargemAlvoPercentual = request.MargemAlvoPercentual,
            IsAtivo = request.IsAtivo,
            UsuarioCriacao = usuarioCriacao ?? "Sistema",
            DataCriacao = DateTime.UtcNow
        };

        // Criar itens
        var ordem = 1;
        foreach (var itemRequest in request.Itens.OrderBy(i => i.Ordem))
        {
            ficha.Itens.Add(new FichaTecnicaItem
            {
                TipoItem = itemRequest.TipoItem,
                ReceitaId = itemRequest.ReceitaId,
                InsumoId = itemRequest.InsumoId,
                Quantidade = itemRequest.Quantidade,
                UnidadeMedidaId = itemRequest.UnidadeMedidaId,
                ExibirComoQB = itemRequest.ExibirComoQB,
                Ordem = ordem++,
                Observacoes = itemRequest.Observacoes,
                UsuarioCriacao = usuarioCriacao ?? "Sistema",
                DataCriacao = DateTime.UtcNow
            });
        }

        // Calcular rendimento final
        CalcularRendimentoFinal(ficha, unidadesMedida);

        // Calcular custos
        CalcularCustosFichaTecnica(ficha, insumos, receitas);

        // Calcular preço sugerido
        CalcularPrecoSugerido(ficha);

        // Criar canais padrão automaticamente
        ficha.Canais.Add(new FichaTecnicaCanal
        {
            Canal = "ifood-1",
            NomeExibicao = "Ifood 1",
            TaxaPercentual = 13,
            IsAtivo = true
        });

        ficha.Canais.Add(new FichaTecnicaCanal
        {
            Canal = "ifood-2",
            NomeExibicao = "Ifood 2",
            TaxaPercentual = 25,
            IsAtivo = true
        });

        // Calcular preços dos canais
        CalcularPrecosCanais(ficha);

        _context.FichasTecnicas.Add(ficha);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Ficha Técnica criada com sucesso - ID: {Id}", ficha.Id);

        return await GetByIdAsync(ficha.Id) ?? throw new InvalidOperationException("Erro ao buscar ficha técnica criada");
    }

    public async Task<FichaTecnicaDto?> UpdateAsync(long id, UpdateFichaTecnicaRequest request, string? usuarioAtualizacao)
    {
        _logger.LogInformation("Atualizando Ficha Técnica - ID: {Id}, Usuário: {Usuario}", id, usuarioAtualizacao ?? "Sistema");

        var ficha = await _context.FichasTecnicas
            .Include(f => f.Canais)
            .Include(f => f.Itens)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (ficha == null)
        {
            _logger.LogWarning("Ficha Técnica com ID {Id} não encontrada", id);
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
            throw new BusinessException("A ficha técnica deve ter pelo menos um item");
        }

        // Validar itens
        foreach (var item in request.Itens)
        {
            if (item.TipoItem == "Receita" && !item.ReceitaId.HasValue)
            {
                throw new BusinessException("ReceitaId é obrigatório quando TipoItem é 'Receita'");
            }
            if (item.TipoItem == "Insumo" && !item.InsumoId.HasValue)
            {
                throw new BusinessException("InsumoId é obrigatório quando TipoItem é 'Insumo'");
            }
            if (item.TipoItem != "Receita" && item.TipoItem != "Insumo")
            {
                throw new BusinessException("TipoItem deve ser 'Receita' ou 'Insumo'");
            }
        }

        // Validar receitas e insumos
        var receitaIds = request.Itens.Where(i => i.TipoItem == "Receita" && i.ReceitaId.HasValue).Select(i => i.ReceitaId!.Value).Distinct().ToList();
        var insumoIds = request.Itens.Where(i => i.TipoItem == "Insumo" && i.InsumoId.HasValue).Select(i => i.InsumoId!.Value).Distinct().ToList();
        var unidadeMedidaIds = request.Itens.Select(i => i.UnidadeMedidaId).Distinct().ToList();

        var receitas = await _context.Receitas
            .Where(r => receitaIds.Contains(r.Id) && r.IsAtivo)
            .ToListAsync();

        if (receitas.Count != receitaIds.Count)
        {
            throw new BusinessException("Uma ou mais receitas são inválidas ou estão inativas");
        }

        var insumos = await _context.Insumos
            .Where(i => insumoIds.Contains(i.Id) && i.IsAtivo)
            .ToListAsync();

        if (insumos.Count != insumoIds.Count)
        {
            throw new BusinessException("Um ou mais insumos são inválidos ou estão inativos");
        }

        var unidadesMedida = await _context.UnidadesMedida
            .Where(u => unidadeMedidaIds.Contains(u.Id) && u.IsAtivo)
            .ToListAsync();

        if (unidadesMedida.Count != unidadeMedidaIds.Count)
        {
            throw new BusinessException("Uma ou mais unidades de medida são inválidas ou estão inativas");
        }

        // Atualizar ficha
        ficha.CategoriaId = request.CategoriaId;
        ficha.Nome = request.Nome;
        ficha.Codigo = request.Codigo;
        ficha.DescricaoComercial = request.DescricaoComercial;
        ficha.IndiceContabil = request.IndiceContabil;
        ficha.ICOperador = request.ICOperador;
        ficha.ICValor = request.ICValor;
        ficha.IPCValor = request.IPCValor;
        ficha.MargemAlvoPercentual = request.MargemAlvoPercentual;
        ficha.IsAtivo = request.IsAtivo;
        ficha.UsuarioAtualizacao = usuarioAtualizacao;
        ficha.DataAtualizacao = DateTime.UtcNow;

        // Remover itens antigos
        _context.FichaTecnicaItens.RemoveRange(ficha.Itens);
        ficha.Itens.Clear();

        // Adicionar novos itens
        var ordem = 1;
        foreach (var itemRequest in request.Itens.OrderBy(i => i.Ordem))
        {
            ficha.Itens.Add(new FichaTecnicaItem
            {
                TipoItem = itemRequest.TipoItem,
                ReceitaId = itemRequest.ReceitaId,
                InsumoId = itemRequest.InsumoId,
                Quantidade = itemRequest.Quantidade,
                UnidadeMedidaId = itemRequest.UnidadeMedidaId,
                ExibirComoQB = itemRequest.ExibirComoQB,
                Ordem = ordem++,
                Observacoes = itemRequest.Observacoes,
                UsuarioAtualizacao = usuarioAtualizacao,
                DataAtualizacao = DateTime.UtcNow
            });
        }

        // Calcular rendimento final
        CalcularRendimentoFinal(ficha, unidadesMedida);

        // Calcular custos
        CalcularCustosFichaTecnica(ficha, insumos, receitas);

        // Calcular preço sugerido
        CalcularPrecoSugerido(ficha);

        // Atualizar canais (manter existentes ou criar novos)
        if (request.Canais != null && request.Canais.Any())
        {
            _context.FichaTecnicaCanais.RemoveRange(ficha.Canais);
            ficha.Canais.Clear();

            foreach (var canalReq in request.Canais)
            {
                ficha.Canais.Add(new FichaTecnicaCanal
                {
                    Canal = canalReq.Canal,
                    NomeExibicao = canalReq.NomeExibicao,
                    PrecoVenda = canalReq.PrecoVenda,
                    TaxaPercentual = canalReq.TaxaPercentual,
                    ComissaoPercentual = canalReq.ComissaoPercentual,
                    Observacoes = canalReq.Observacoes,
                    IsAtivo = canalReq.IsAtivo
                });
            }
        }

        // Calcular preços dos canais
        CalcularPrecosCanais(ficha);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Ficha Técnica atualizada com sucesso - ID: {Id}", id);

        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(long id)
    {
        _logger.LogInformation("Excluindo Ficha Técnica - ID: {Id}", id);

        var ficha = await _context.FichasTecnicas
            .Include(f => f.Canais)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (ficha == null)
        {
            _logger.LogWarning("Ficha Técnica com ID {Id} não encontrada", id);
            return false;
        }

        _context.FichaTecnicaCanais.RemoveRange(ficha.Canais);
        _context.FichasTecnicas.Remove(ficha);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Ficha Técnica excluída com sucesso - ID: {Id}", id);
        return true;
    }

    private FichaTecnicaDto MapToDto(FichaTecnica ficha)
    {
        return new FichaTecnicaDto
        {
            Id = ficha.Id,
            CategoriaId = ficha.CategoriaId,
            CategoriaNome = ficha.Categoria?.Nome,
            Nome = ficha.Nome,
            Codigo = ficha.Codigo,
            DescricaoComercial = ficha.DescricaoComercial,
            CustoTotal = ficha.CustoTotal,
            CustoPorUnidade = ficha.CustoPorUnidade,
            RendimentoFinal = ficha.RendimentoFinal,
            IndiceContabil = ficha.IndiceContabil,
            PrecoSugeridoVenda = ficha.PrecoSugeridoVenda,
            ICOperador = ficha.ICOperador,
            ICValor = ficha.ICValor,
            IPCValor = ficha.IPCValor,
            MargemAlvoPercentual = ficha.MargemAlvoPercentual,
            IsAtivo = ficha.IsAtivo,
            UsuarioCriacao = ficha.UsuarioCriacao,
            UsuarioAtualizacao = ficha.UsuarioAtualizacao,
            DataCriacao = ficha.DataCriacao,
            DataAtualizacao = ficha.DataAtualizacao,
            Itens = ficha.Itens
                .OrderBy(i => i.Ordem)
                .Select(i => new FichaTecnicaItemDto
                {
                    Id = i.Id,
                    FichaTecnicaId = i.FichaTecnicaId,
                    TipoItem = i.TipoItem,
                    ReceitaId = i.ReceitaId,
                    ReceitaNome = i.Receita?.Nome,
                    InsumoId = i.InsumoId,
                    InsumoNome = i.Insumo?.Nome,
                    Quantidade = i.Quantidade,
                    UnidadeMedidaId = i.UnidadeMedidaId,
                    UnidadeMedidaNome = i.UnidadeMedida?.Nome,
                    UnidadeMedidaSigla = i.UnidadeMedida?.Sigla,
                    ExibirComoQB = i.ExibirComoQB,
                    Ordem = i.Ordem,
                    Observacoes = i.Observacoes,
                    UsuarioCriacao = i.UsuarioCriacao,
                    UsuarioAtualizacao = i.UsuarioAtualizacao,
                    DataCriacao = i.DataCriacao,
                    DataAtualizacao = i.DataAtualizacao
                })
                .ToList(),
            Canais = ficha.Canais
                .OrderBy(c => c.Canal)
                .Select(c => new FichaTecnicaCanalDto
                {
                    Id = c.Id,
                    FichaTecnicaId = ficha.Id,
                    Canal = c.Canal,
                    NomeExibicao = c.NomeExibicao,
                    PrecoVenda = c.PrecoVenda,
                    TaxaPercentual = c.TaxaPercentual,
                    ComissaoPercentual = c.ComissaoPercentual,
                    MargemCalculadaPercentual = c.MargemCalculadaPercentual,
                    Observacoes = c.Observacoes,
                    IsAtivo = c.IsAtivo
                })
                .ToList()
        };
    }
}
