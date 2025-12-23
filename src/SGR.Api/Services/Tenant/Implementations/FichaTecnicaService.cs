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
    /// Calcula o rendimento final da ficha técnica considerando itens em GR (gramas) ou ML (mililitros)
    /// Para itens do tipo Receita: quantidade representa porções, então multiplica por PesoPorPorcao
    /// Para itens do tipo Insumo: soma quantidade diretamente
    /// </summary>
    private void CalcularRendimentoFinal(FichaTecnica ficha, List<UnidadeMedida> unidadesMedida, List<Receita> receitas)
    {
        // Considerar itens cuja UnidadeMedida.Sigla seja "GR" ou "ML"
        var quantidadeTotalBase = 0m;

        foreach (var item in ficha.Itens)
        {
            var unidadeMedida = unidadesMedida.FirstOrDefault(u => u.Id == item.UnidadeMedidaId);
            
            // IMPORTANTE: ExibirComoQB é apenas visual, sempre usar Quantidade numérica
            
            if (item.TipoItem == "Receita" && item.ReceitaId.HasValue)
            {
                // Para receitas: sempre usar PesoPorPorcao, independente da unidade (GR ou UN)
                var receita = receitas.FirstOrDefault(r => r.Id == item.ReceitaId.Value);
                if (receita != null && receita.PesoPorPorcao.HasValue && receita.PesoPorPorcao.Value > 0)
                {
                    quantidadeTotalBase += item.Quantidade * receita.PesoPorPorcao.Value;
                }
                // Se PesoPorPorcao for null ou <= 0, ignorar essa linha (não somar)
            }
            else if (item.TipoItem == "Insumo" && unidadeMedida != null && (unidadeMedida.Sigla.ToUpper() == "GR" || unidadeMedida.Sigla.ToUpper() == "ML"))
            {
                // Para insumos: somar quantidade diretamente apenas se unidade for GR ou ML
                quantidadeTotalBase += item.Quantidade;
            }
        }

        // Aplicar IC (Índice de Cocção)
        decimal pesoAposCoccao = quantidadeTotalBase;
        if (ficha.ICOperador.HasValue && ficha.ICValor.HasValue && ficha.ICValor.Value > 0)
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
        // Aplicar IPC (Índice de Partes Comestíveis)
        // Aplicar apenas se ipcValor for > 0 (não aplicar se for 0 ou null)
        // Se ipcValor for null ou 0, usar pesoAposCoccao diretamente (100% comestível)
        decimal pesoComestivel = pesoAposCoccao;
        if (ficha.IPCValor.HasValue && ficha.IPCValor.Value > 0)
        {
            var ipcValor = Math.Clamp(ficha.IPCValor.Value, 0, 999);
            var ipcPercentual = ipcValor / 100m;
            pesoComestivel = pesoAposCoccao * ipcPercentual;
        }

        ficha.RendimentoFinal = pesoComestivel;
    }

    /// <summary>
    /// Calcula o preço sugerido de venda
    /// Se PorcaoVendaQuantidade informado: PrecoSugeridoVenda = custo por porção * IndiceContabil (preço mesa por porção)
    /// Se PorcaoVendaQuantidade não informado: PrecoSugeridoVenda = CustoPorUnidade * IndiceContabil (comportamento legado por unidade base)
    /// SÃ³ calcula se CustoPorUnidade for válido (RendimentoFinal > 0)
    /// </summary>
    private void CalcularPrecoSugerido(FichaTecnica ficha)
    {
        // NÃ£o calcular se RendimentoFinal não estiver disponível ou IndiceContabil inválido ou CustoPorUnidade <= 0
        if (!ficha.RendimentoFinal.HasValue || ficha.RendimentoFinal.Value <= 0)
        {
            ficha.PrecoSugeridoVenda = null;
            return;
        }

        if (!ficha.IndiceContabil.HasValue || ficha.IndiceContabil.Value <= 0 || ficha.CustoPorUnidade <= 0)
        {
            ficha.PrecoSugeridoVenda = null;
            return;
        }

        // Calcular custo unitário preciso (não usar CustoPorUnidade arredondado)
        var custoUnitPreciso = ficha.CustoTotal / ficha.RendimentoFinal.Value;

        // Se PorcaoVendaQuantidade informado (> 0): novo cálculo por porção
        if (ficha.PorcaoVendaQuantidade.HasValue && ficha.PorcaoVendaQuantidade.Value > 0)
        {
            var custoPorPorcao = custoUnitPreciso * ficha.PorcaoVendaQuantidade.Value;
            var precoMesa = custoPorPorcao * ficha.IndiceContabil.Value;
            ficha.PrecoSugeridoVenda = Math.Round(precoMesa, 4);
        }
        else
        {
            // Comportamento legado: por unidade base (ex.: por grama)
            ficha.PrecoSugeridoVenda = Math.Round(custoUnitPreciso * ficha.IndiceContabil.Value, 4);
        }
    }

    /// <summary>
    /// Cria canais padrão para a ficha técnica (Plano 12% e Plano 23%)
    /// </summary>
    private void CriarCanaisPadrao(FichaTecnica ficha)
    {
        ficha.Canais.Add(new FichaTecnicaCanal
        {
            Canal = "ifood-1",
            NomeExibicao = "Plano 12%",
            PrecoVenda = 0,
            TaxaPercentual = 12,
            ComissaoPercentual = null,
            Multiplicador = 1.138m,
            Observacoes = null,
            IsAtivo = true
        });

        ficha.Canais.Add(new FichaTecnicaCanal
        {
            Canal = "ifood-2",
            NomeExibicao = "Plano 23%",
            PrecoVenda = 0,
            TaxaPercentual = 23,
            ComissaoPercentual = null,
            Multiplicador = 1.3m,
            Observacoes = null,
            IsAtivo = true
        });
    }

    /// <summary>
    /// Calcula os preços dos canais (lógica híbrida)
    /// - Se PrecoVenda for 0 ou null: calcular automaticamente a partir de PrecoSugeridoVenda
    ///   - Se Multiplicador > 0: usar multiplicador fixo
    ///   - Senão: usar gross-up por fee (precoBase / (1 - fee))
    /// - Se PrecoSugeridoVenda não estiver disponível, usa base alternativa (custo por porção ou custo por unidade)
    /// - Se PrecoVenda > 0: respeitar o valor e apenas recalcular margem
    /// Margem calculada com fee sobre receita (não sobre custo)
    /// </summary>
    private void CalcularPrecosCanais(FichaTecnica ficha)
    {
        // Calcular custo unitário preciso (não usar CustoPorUnidade arredondado)
        var custoUnitPreciso = ficha.RendimentoFinal.HasValue && ficha.RendimentoFinal.Value > 0
            ? (ficha.CustoTotal / ficha.RendimentoFinal.Value)
            : 0m;

        // Calcular custo por porção para uso no cálculo de margem
        decimal custoPorPorcao = 0m;
        if (ficha.PorcaoVendaQuantidade.HasValue && ficha.PorcaoVendaQuantidade.Value > 0)
        {
            custoPorPorcao = custoUnitPreciso * ficha.PorcaoVendaQuantidade.Value;
        }
        else
        {
            // Se não há porção definida, usar custo unitário preciso como base (comportamento legado)
            custoPorPorcao = custoUnitPreciso;
        }

        // Calcular base para precificação dos canais
        // Prioridade 1: PrecoSugeridoVenda (quando IndiceContabil está definido)
        // Prioridade 2: CustoPorPorcaoVenda (quando porção está definida)
        // Prioridade 3: CustoPorUnidade (comportamento legado)
        decimal precoBase = ficha.PrecoSugeridoVenda ?? 0m;

        // Se PrecoSugeridoVenda não estiver disponível, usar custo como base alternativa
        if (precoBase <= 0)
        {
            if (ficha.PorcaoVendaQuantidade.HasValue && ficha.PorcaoVendaQuantidade.Value > 0 && custoPorPorcao > 0)
            {
                // Usar custo por porção como base
                precoBase = custoPorPorcao;
            }
            else if (ficha.CustoPorUnidade > 0)
            {
                // Usar custo por unidade como base (legado)
                precoBase = ficha.CustoPorUnidade;
            }
        }

        foreach (var canal in ficha.Canais)
        {
            // Proteção crítica: se não houver base válida ou CustoPorUnidade inválido, zerar tudo
            if (precoBase <= 0 || ficha.CustoPorUnidade <= 0)
            {
                canal.PrecoVenda = 0m;
                canal.MargemCalculadaPercentual = null;
                continue;
            }

            // Lógica híbrida: verificar se PrecoVenda já foi definido manualmente
            var precoVendaOriginal = canal.PrecoVenda;
            var modoAutomatico = precoVendaOriginal <= 0;

            if (modoAutomatico)
            {
                // Modo automático: calcular PrecoVenda
                if (canal.Multiplicador.HasValue && canal.Multiplicador.Value > 0)
                {
                    // Usar multiplicador fixo (prioridade)
                    canal.PrecoVenda = Math.Round(precoBase * canal.Multiplicador.Value, 4);
                }
                else
                {
                    // Usar gross-up por fee
                    var taxaPercentual = canal.TaxaPercentual ?? 0m;
                    var comissaoPercentual = canal.ComissaoPercentual ?? 0m;
                    var fee = (taxaPercentual + comissaoPercentual) / 100m;
                    
                    if (fee >= 1)
                    {
                        // Fee inválido (>= 100%), não calcular
                        canal.PrecoVenda = 0m;
                        canal.MargemCalculadaPercentual = null;
                        continue;
                    }
                    
                    canal.PrecoVenda = Math.Round(precoBase / (1 - fee), 4);
                }
            }
            // Se modo manual (precoVendaOriginal > 0), manter o valor e apenas recalcular margem

            // Calcular margem (fee sobre receita, não sobre custo)
            if (canal.PrecoVenda > 0 && custoPorPorcao > 0)
            {
                var taxaPercentual = canal.TaxaPercentual ?? 0m;
                var comissaoPercentual = canal.ComissaoPercentual ?? 0m;
                var feePercent = (taxaPercentual + comissaoPercentual) / 100m;
                
                var receitaLiquida = canal.PrecoVenda * (1 - feePercent);
                var lucro = receitaLiquida - custoPorPorcao;
                var margem = (lucro / canal.PrecoVenda) * 100m;
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
        _logger.LogInformation("Buscando Fichas Técnicas - PÃ¡gina: {Page}, Tamanho: {PageSize}, Busca: {Search}", page, pageSize, search ?? "N/A");

        // Usar AsNoTracking para queries de leitura (melhor performance)
        var query = _context.FichasTecnicas
            .AsNoTracking()
            .Include(f => f.Categoria)
            .Include(f => f.ReceitaPrincipal)
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
            .Include(f => f.ReceitaPrincipal)
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

        // Usar AsNoTracking para queries de leitura (melhor performance)
        var ficha = await _context.FichasTecnicas
            .AsNoTracking()
            .Include(f => f.Categoria)
            .Include(f => f.ReceitaPrincipal)
            .Include(f => f.PorcaoVendaUnidadeMedida)
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
            throw new BusinessException("Categoria invÃ¡lida ou inativa");
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
        if (request.ReceitaPrincipalId.HasValue)
        {
            receitaIds.Add(request.ReceitaPrincipalId.Value);
            receitaIds = receitaIds.Distinct().ToList();
        }
        var insumoIds = request.Itens.Where(i => i.TipoItem == "Insumo" && i.InsumoId.HasValue).Select(i => i.InsumoId!.Value).Distinct().ToList();
        var unidadeMedidaIds = request.Itens.Select(i => i.UnidadeMedidaId).Distinct().ToList();

        var receitas = await _context.Receitas
            .Where(r => receitaIds.Contains(r.Id) && r.IsAtivo)
            .ToListAsync();

        if (request.ReceitaPrincipalId.HasValue && !receitas.Any(r => r.Id == request.ReceitaPrincipalId.Value))
        {
            throw new BusinessException("Receita principal invalida ou inativa");
        }

        if (receitas.Count != receitaIds.Count)
        {
            throw new BusinessException("Uma ou mais receitas sÃ£o invÃ¡lidas ou estão inativas");
        }

        var insumos = await _context.Insumos
            .Where(i => insumoIds.Contains(i.Id) && i.IsAtivo)
            .ToListAsync();

        if (insumos.Count != insumoIds.Count)
        {
            throw new BusinessException("Um ou mais insumos sÃ£o inválidos ou estão inativos");
        }

        var unidadesMedida = await _context.UnidadesMedida
            .Where(u => unidadeMedidaIds.Contains(u.Id) && u.IsAtivo)
            .ToListAsync();

        if (unidadesMedida.Count != unidadeMedidaIds.Count)
        {
            throw new BusinessException("Uma ou mais unidades de medida sÃ£o invÃ¡lidas ou estão inativas");
        }

        // Validar unidades de medida dos itens
        foreach (var item in request.Itens)
        {
            if (item.TipoItem == "Insumo" && item.InsumoId.HasValue)
            {
                var insumo = insumos.FirstOrDefault(i => i.Id == item.InsumoId.Value);
                if (insumo == null)
                {
                    throw new BusinessException($"Insumo com ID {item.InsumoId.Value} não encontrado.");
                }
                if (item.UnidadeMedidaId != insumo.UnidadeUsoId)
                {
                    throw new BusinessException($"Unidade do item deve ser igual à unidade de uso do insumo '{insumo.Nome}'.");
                }
            }
            else if (item.TipoItem == "Receita")
            {
                var unidade = unidadesMedida.FirstOrDefault(u => u.Id == item.UnidadeMedidaId);
                if (unidade == null)
                {
                    throw new BusinessException($"Unidade de medida com ID {item.UnidadeMedidaId} não encontrada.");
                }
                // Permitir GR (legado) ou UN (recomendado para melhor UX e integridade de dados)
                var sigla = unidade.Sigla.ToUpper();
                if (sigla != "GR" && sigla != "UN")
                {
                    throw new BusinessException("Itens do tipo Receita na ficha técnica devem usar unidade de medida GR (gramas) ou UN (unidade).");
                }
            }
        }

        // Validar porção de venda
        if (request.PorcaoVendaQuantidade.HasValue && request.PorcaoVendaQuantidade.Value > 0)
        {
            if (!request.PorcaoVendaUnidadeMedidaId.HasValue)
            {
                throw new BusinessException("PorcaoVendaUnidadeMedidaId é obrigatório quando PorcaoVendaQuantidade está preenchido.");
            }

            var porcaoUnidadeMedida = await _context.UnidadesMedida
                .FirstOrDefaultAsync(u => u.Id == request.PorcaoVendaUnidadeMedidaId.Value);

            if (porcaoUnidadeMedida == null || !porcaoUnidadeMedida.IsAtivo)
            {
                throw new BusinessException("Unidade de medida da porção de venda inválida ou inativa.");
            }

            var siglaPorcao = porcaoUnidadeMedida.Sigla.ToUpper();
            if (siglaPorcao != "GR" && siglaPorcao != "ML")
            {
                throw new BusinessException("Unidade de medida da porção de venda deve ser GR (gramas) ou ML (mililitros).");
            }
        }

        // Se PorcaoVendaUnidadeMedidaId informado, validar mesmo sem PorcaoVendaQuantidade
        if (request.PorcaoVendaUnidadeMedidaId.HasValue)
        {
            var porcaoUnidadeMedida = await _context.UnidadesMedida
                .FirstOrDefaultAsync(u => u.Id == request.PorcaoVendaUnidadeMedidaId.Value);

            if (porcaoUnidadeMedida == null || !porcaoUnidadeMedida.IsAtivo)
            {
                throw new BusinessException("Unidade de medida da porção de venda inválida ou inativa.");
            }

            var siglaPorcao = porcaoUnidadeMedida.Sigla.ToUpper();
            if (siglaPorcao != "GR" && siglaPorcao != "ML")
            {
                throw new BusinessException("Unidade de medida da porção de venda deve ser GR (gramas) ou ML (mililitros).");
            }
        }

        var ficha = new FichaTecnica
        {
            CategoriaId = request.CategoriaId,
            ReceitaPrincipalId = request.ReceitaPrincipalId,
            Nome = request.Nome,
            Codigo = request.Codigo,
            DescricaoComercial = request.DescricaoComercial,
            IndiceContabil = request.IndiceContabil,
            ICOperador = request.ICOperador,
            ICValor = request.ICValor,
            IPCValor = request.IPCValor,
            MargemAlvoPercentual = request.MargemAlvoPercentual,
            PorcaoVendaQuantidade = request.PorcaoVendaQuantidade,
            PorcaoVendaUnidadeMedidaId = request.PorcaoVendaUnidadeMedidaId,
            RendimentoPorcoes = request.RendimentoPorcoes,
            TempoPreparo = request.TempoPreparo,
            IsAtivo = request.IsAtivo,
            UsuarioCriacao = usuarioCriacao ?? "Sistema",
            DataCriacao = DateTime.UtcNow
        };

        // Criar itens
        _logger.LogInformation("Criando {Count} itens para a ficha técnica", request.Itens.Count);
        
        var ordem = 1;
        foreach (var itemRequest in request.Itens.OrderBy(i => i.Ordem))
        {
            _logger.LogDebug("Adicionando item: Tipo={Tipo}, Ordem={Ordem}, ReceitaId={ReceitaId}, InsumoId={InsumoId}", 
                itemRequest.TipoItem, ordem, itemRequest.ReceitaId, itemRequest.InsumoId);
            
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
        
        _logger.LogInformation("Total de {Count} itens adicionados à ficha técnica", ficha.Itens.Count);

        // Calcular rendimento final
        CalcularRendimentoFinal(ficha, unidadesMedida, receitas);

        // Calcular custos
        CalcularCustosFichaTecnica(ficha, insumos, receitas);

        // Calcular preço sugerido
        CalcularPrecoSugerido(ficha);

        // Criar canais: usar canais do request se presentes, senão criar canais padrão
        if (request.Canais != null && request.Canais.Any())
        {
            // Usar exatamente os canais do request
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
        else
        {
            // Criar canais padrão quando request.Canais estiver vazio ou null
            CriarCanaisPadrao(ficha);
        }

        // Calcular preços dos canais (sempre haverá canais: do request ou padrão)
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
                throw new BusinessException("Categoria invÃ¡lida ou inativa");
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
            if (request.ReceitaPrincipalId.HasValue)
            {
                receitaIds.Add(request.ReceitaPrincipalId.Value);
                receitaIds = receitaIds.Distinct().ToList();
            }
            var insumoIds = request.Itens.Where(i => i.TipoItem == "Insumo" && i.InsumoId.HasValue).Select(i => i.InsumoId!.Value).Distinct().ToList();
            var unidadeMedidaIds = request.Itens.Select(i => i.UnidadeMedidaId).Distinct().ToList();

            var receitas = await _context.Receitas
                .Where(r => receitaIds.Contains(r.Id) && r.IsAtivo)
                .ToListAsync();

            if (request.ReceitaPrincipalId.HasValue && !receitas.Any(r => r.Id == request.ReceitaPrincipalId.Value))
            {
                throw new BusinessException("Receita principal invalida ou inativa");
            }

            if (receitas.Count != receitaIds.Count)
            {
                throw new BusinessException("Uma ou mais receitas sÃ£o invÃ¡lidas ou estão inativas");
            }

            var insumos = await _context.Insumos
                .Where(i => insumoIds.Contains(i.Id) && i.IsAtivo)
                .ToListAsync();

            if (insumos.Count != insumoIds.Count)
            {
                throw new BusinessException("Um ou mais insumos sÃ£o inválidos ou estão inativos");
            }

            var unidadesMedida = await _context.UnidadesMedida
                .Where(u => unidadeMedidaIds.Contains(u.Id) && u.IsAtivo)
                .ToListAsync();

            if (unidadesMedida.Count != unidadeMedidaIds.Count)
            {
                throw new BusinessException("Uma ou mais unidades de medida sÃ£o invÃ¡lidas ou estão inativas");
            }

            // Validar unidades de medida dos itens
            foreach (var item in request.Itens)
            {
                if (item.TipoItem == "Insumo" && item.InsumoId.HasValue)
                {
                    var insumo = insumos.FirstOrDefault(i => i.Id == item.InsumoId.Value);
                    if (insumo == null)
                    {
                        throw new BusinessException($"Insumo com ID {item.InsumoId.Value} não encontrado.");
                    }
                    if (item.UnidadeMedidaId != insumo.UnidadeUsoId)
                    {
                        throw new BusinessException($"Unidade do item deve ser igual à unidade de uso do insumo '{insumo.Nome}'.");
                    }
                }
                else if (item.TipoItem == "Receita")
                {
                    var unidade = unidadesMedida.FirstOrDefault(u => u.Id == item.UnidadeMedidaId);
                    if (unidade == null)
                    {
                        throw new BusinessException($"Unidade de medida com ID {item.UnidadeMedidaId} não encontrada.");
                    }
                    // Permitir GR (legado) ou UN (recomendado para melhor UX e integridade de dados)
                    var sigla = unidade.Sigla.ToUpper();
                    if (sigla != "GR" && sigla != "UN")
                    {
                        throw new BusinessException("Itens do tipo Receita na ficha técnica devem usar unidade de medida GR (gramas) ou UN (unidade).");
                    }
                }
            }

            // Validar porção de venda
            if (request.PorcaoVendaQuantidade.HasValue && request.PorcaoVendaQuantidade.Value > 0)
            {
                if (!request.PorcaoVendaUnidadeMedidaId.HasValue)
                {
                    throw new BusinessException("PorcaoVendaUnidadeMedidaId é obrigatório quando PorcaoVendaQuantidade está preenchido.");
                }

                var porcaoUnidadeMedida = await _context.UnidadesMedida
                    .FirstOrDefaultAsync(u => u.Id == request.PorcaoVendaUnidadeMedidaId.Value);

                if (porcaoUnidadeMedida == null || !porcaoUnidadeMedida.IsAtivo)
                {
                    throw new BusinessException("Unidade de medida da porção de venda inválida ou inativa.");
                }

                var siglaPorcao = porcaoUnidadeMedida.Sigla.ToUpper();
                if (siglaPorcao != "GR" && siglaPorcao != "ML")
                {
                    throw new BusinessException("Unidade de medida da porção de venda deve ser GR (gramas) ou ML (mililitros).");
                }
            }

            // Se PorcaoVendaUnidadeMedidaId informado, validar mesmo sem PorcaoVendaQuantidade
            if (request.PorcaoVendaUnidadeMedidaId.HasValue)
            {
                var porcaoUnidadeMedida = await _context.UnidadesMedida
                    .FirstOrDefaultAsync(u => u.Id == request.PorcaoVendaUnidadeMedidaId.Value);

                if (porcaoUnidadeMedida == null || !porcaoUnidadeMedida.IsAtivo)
                {
                    throw new BusinessException("Unidade de medida da porção de venda inválida ou inativa.");
                }

                var siglaPorcao = porcaoUnidadeMedida.Sigla.ToUpper();
                if (siglaPorcao != "GR" && siglaPorcao != "ML")
                {
                    throw new BusinessException("Unidade de medida da porção de venda deve ser GR (gramas) ou ML (mililitros).");
                }
            }

            // Atualizar ficha
            ficha.CategoriaId = request.CategoriaId;
            ficha.ReceitaPrincipalId = request.ReceitaPrincipalId;
            ficha.Nome = request.Nome;
            ficha.Codigo = request.Codigo;
            ficha.DescricaoComercial = request.DescricaoComercial;
            ficha.IndiceContabil = request.IndiceContabil;
            ficha.ICOperador = request.ICOperador;
            ficha.ICValor = request.ICValor;
            ficha.IPCValor = request.IPCValor;
            ficha.MargemAlvoPercentual = request.MargemAlvoPercentual;
            ficha.PorcaoVendaQuantidade = request.PorcaoVendaQuantidade;
            ficha.PorcaoVendaUnidadeMedidaId = request.PorcaoVendaUnidadeMedidaId;
            ficha.RendimentoPorcoes = request.RendimentoPorcoes;
            ficha.TempoPreparo = request.TempoPreparo;
            ficha.IsAtivo = request.IsAtivo;
            ficha.UsuarioAtualizacao = usuarioAtualizacao;
            ficha.DataAtualizacao = DateTime.UtcNow;

            // Remover itens antigos
            // NÃ£o usar Clear() pois RemoveRange jÃ¡ remove do contexto
            // Apenas remover do contexto, a coleção serÃ¡ atualizada automaticamente
            var itensParaRemover = ficha.Itens.ToList();
            _context.FichaTecnicaItens.RemoveRange(itensParaRemover);
            
            // Limpar a coleção em memÃ³ria para garantir que novos itens sejam adicionados corretamente
            ficha.Itens.Clear();

            // Adicionar novos itens
            _logger.LogInformation("Atualizando ficha técnica - Criando {Count} itens", request.Itens.Count);
            
            var ordem = 1;
            foreach (var itemRequest in request.Itens.OrderBy(i => i.Ordem))
            {
                _logger.LogDebug("Adicionando item: Tipo={Tipo}, Ordem={Ordem}, ReceitaId={ReceitaId}, InsumoId={InsumoId}", 
                    itemRequest.TipoItem, ordem, itemRequest.ReceitaId, itemRequest.InsumoId);
                
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
            
            _logger.LogInformation("Total de {Count} itens adicionados à ficha técnica", ficha.Itens.Count);

            // Calcular rendimento final
            CalcularRendimentoFinal(ficha, unidadesMedida, receitas);

            // Calcular custos
            CalcularCustosFichaTecnica(ficha, insumos, receitas);

            // Calcular preço sugerido
            CalcularPrecoSugerido(ficha);

            // Atualizar canais (manter existentes ou criar novos)
            if (request.Canais != null && request.Canais.Any())
            {
                // NÃ£o usar Clear() pois RemoveRange jÃ¡ remove do contexto
                var canaisParaRemover = ficha.Canais.ToList();
                _context.FichaTecnicaCanais.RemoveRange(canaisParaRemover);

                foreach (var canalReq in request.Canais)
                {
                    ficha.Canais.Add(new FichaTecnicaCanal
                    {
                        Canal = canalReq.Canal,
                        NomeExibicao = canalReq.NomeExibicao,
                        PrecoVenda = canalReq.PrecoVenda,
                        TaxaPercentual = canalReq.TaxaPercentual,
                        ComissaoPercentual = canalReq.ComissaoPercentual,
                        Multiplicador = canalReq.Multiplicador,
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
        // Calcular campos calculados apenas se Itens estiverem carregados (GetById)
        decimal? pesoTotalBase = null;
        decimal? custoKgL = null;
        decimal? custoPorPorcaoVenda = null;

        var itens = ficha.Itens ?? new List<FichaTecnicaItem>();
        if (itens.Any())
        {
            // Calcular PesoTotalBase: soma GR/ML antes de IC/IPC
            var unidadesMedida = itens
                .Where(i => i.UnidadeMedida != null)
                .Select(i => i.UnidadeMedida!)
                .GroupBy(u => u.Id)
                .Select(g => g.First())
                .ToList();

            var receitas = itens
                .Where(i => i.Receita != null)
                .Select(i => i.Receita!)
                .GroupBy(r => r.Id)
                .Select(g => g.First())
                .ToList();

            var quantidadeTotalBase = 0m;
            foreach (var item in itens)
            {
                var unidadeMedida = item.UnidadeMedida;
                
                if (item.TipoItem == "Receita" && item.ReceitaId.HasValue)
                {
                    // Para receitas: sempre usar PesoPorPorcao, independente da unidade (GR ou UN)
                    var receita = receitas.FirstOrDefault(r => r.Id == item.ReceitaId.Value);
                    if (receita != null && receita.PesoPorPorcao.HasValue && receita.PesoPorPorcao.Value > 0)
                    {
                        quantidadeTotalBase += item.Quantidade * receita.PesoPorPorcao.Value;
                    }
                }
                else if (item.TipoItem == "Insumo" && unidadeMedida != null && (unidadeMedida.Sigla.ToUpper() == "GR" || unidadeMedida.Sigla.ToUpper() == "ML"))
                {
                    // Para insumos: somar quantidade diretamente apenas se unidade for GR ou ML
                    quantidadeTotalBase += item.Quantidade;
                }
            }
            pesoTotalBase = quantidadeTotalBase;

            // Calcular CustoKgL: (CustoTotal / RendimentoFinal) * 1000
            // Se unidade base for GR => custo/kg; se ML => custo/L (multiplicar por 1000 converte g→kg ou ml→L)
            if (ficha.RendimentoFinal.HasValue && ficha.RendimentoFinal.Value > 0)
            {
                custoKgL = (ficha.CustoTotal / ficha.RendimentoFinal.Value) * 1000m;
            }

            // Calcular CustoPorPorcaoVenda usando custo unitário preciso
            if (ficha.PorcaoVendaQuantidade.HasValue && ficha.PorcaoVendaQuantidade.Value > 0)
            {
                if (ficha.RendimentoFinal.HasValue && ficha.RendimentoFinal.Value > 0)
                {
                    var custoUnitPreciso = ficha.CustoTotal / ficha.RendimentoFinal.Value;
                    custoPorPorcaoVenda = custoUnitPreciso * ficha.PorcaoVendaQuantidade.Value;
                }
            }
        }

        return new FichaTecnicaDto
        {
            Id = ficha.Id,
            CategoriaId = ficha.CategoriaId,
            CategoriaNome = ficha.Categoria?.Nome,
            ReceitaPrincipalId = ficha.ReceitaPrincipalId,
            ReceitaPrincipalNome = ficha.ReceitaPrincipal?.Nome,
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
            PorcaoVendaQuantidade = ficha.PorcaoVendaQuantidade,
            PorcaoVendaUnidadeMedidaId = ficha.PorcaoVendaUnidadeMedidaId,
            PorcaoVendaUnidadeMedidaNome = ficha.PorcaoVendaUnidadeMedida?.Nome,
            PorcaoVendaUnidadeMedidaSigla = ficha.PorcaoVendaUnidadeMedida?.Sigla,
            RendimentoPorcoes = ficha.RendimentoPorcoes,
            TempoPreparo = ficha.TempoPreparo,
            PesoTotalBase = pesoTotalBase,
            CustoKgL = custoKgL,
            CustoPorPorcaoVenda = custoPorPorcaoVenda,
            PrecoMesaSugerido = ficha.PrecoSugeridoVenda,
            IsAtivo = ficha.IsAtivo,
            UsuarioCriacao = ficha.UsuarioCriacao,
            UsuarioAtualizacao = ficha.UsuarioAtualizacao,
            DataCriacao = ficha.DataCriacao,
            DataAtualizacao = ficha.DataAtualizacao,
            Itens = (ficha.Itens ?? new List<FichaTecnicaItem>())
                .OrderBy(i => i.Ordem)
                .Select(i =>
                {
                    decimal custoItem = 0m;
                    
                    if (i.TipoItem == "Insumo" && i.InsumoId.HasValue && i.Insumo != null)
                    {
                        var custoPorUnidadeUso = CalcularCustoPorUnidadeUso(i.Insumo);
                        custoItem = Math.Round(i.Quantidade * custoPorUnidadeUso, 4);
                    }
                    else if (i.TipoItem == "Receita" && i.ReceitaId.HasValue && i.Receita != null)
                    {
                        custoItem = Math.Round(i.Quantidade * i.Receita.CustoPorPorcao, 4);
                    }
                    
                    return new FichaTecnicaItemDto
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
                        CustoItem = custoItem,
                        UsuarioCriacao = i.UsuarioCriacao,
                        UsuarioAtualizacao = i.UsuarioAtualizacao,
                        DataCriacao = i.DataCriacao,
                        DataAtualizacao = i.DataAtualizacao
                    };
                })
                .ToList(),
            Canais = (ficha.Canais ?? new List<FichaTecnicaCanal>())
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
                    Multiplicador = c.Multiplicador,
                    MargemCalculadaPercentual = c.MargemCalculadaPercentual,
                    Observacoes = c.Observacoes,
                    IsAtivo = c.IsAtivo
                })
                .ToList()
        };
    }
}
