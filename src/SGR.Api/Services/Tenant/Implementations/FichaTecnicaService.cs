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

    public async Task<PagedResult<FichaTecnicaDto>> GetAllAsync(string? search, int page, int pageSize, string? sort, string? order)
    {
        _logger.LogInformation("Buscando Fichas Tecnicas - Pagina: {Page}, Tamanho: {PageSize}, Busca: {Search}", page, pageSize, search ?? "N/A");

        var query = _context.FichasTecnicas
            .AsNoTracking()
            .Include(f => f.Categoria)
            .Include(f => f.Canais)
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
            "categoria" => ascending ? query.OrderBy(f => f.Categoria.Nome) : query.OrderByDescending(f => f.Categoria.Nome),
            "codigo" => ascending ? query.OrderBy(f => f.Codigo) : query.OrderByDescending(f => f.Codigo),
            "custoporunidade" or "custo_por_unidade" => ascending ? query.OrderBy(f => f.CustoPorUnidade) : query.OrderByDescending(f => f.CustoPorUnidade),
            "precosugerido" or "preco_sugerido" => ascending ? query.OrderBy(f => f.PrecoSugeridoVenda) : query.OrderByDescending(f => f.PrecoSugeridoVenda),
            "ativo" or "isativo" => ascending ? query.OrderBy(f => f.IsAtivo) : query.OrderByDescending(f => f.IsAtivo),
            _ => query.OrderBy(f => f.Nome)
        };

        var total = await query.CountAsync();
        var items = await query
            .Skip(Math.Max(0, (page - 1) * pageSize))
            .Take(pageSize)
            .Select(f => new FichaTecnicaDto
            {
                Id = f.Id,
                CategoriaId = f.CategoriaId,
                CategoriaNome = f.Categoria != null ? f.Categoria.Nome : null,
                ReceitaPrincipalId = f.ReceitaPrincipalId,
                Nome = f.Nome,
                Codigo = f.Codigo,
                DescricaoComercial = f.DescricaoComercial,
                CustoTotal = f.CustoTotal,
                CustoPorUnidade = f.CustoPorUnidade,
                RendimentoFinal = f.RendimentoFinal,
                IndiceContabil = f.IndiceContabil,
                PrecoSugeridoVenda = f.PrecoSugeridoVenda,
                ICOperador = f.ICOperador,
                ICValor = f.ICValor,
                IPCValor = f.IPCValor,
                MargemAlvoPercentual = f.MargemAlvoPercentual,
                PorcaoVendaQuantidade = f.PorcaoVendaQuantidade,
                PorcaoVendaUnidadeMedidaId = f.PorcaoVendaUnidadeMedidaId,
                RendimentoPorcoes = f.RendimentoPorcoes,
                RendimentoPorcoesNumero = f.RendimentoPorcoesNumero,
                TempoPreparo = f.TempoPreparo,
                IsAtivo = f.IsAtivo,
                UsuarioCriacao = f.UsuarioCriacao,
                UsuarioAtualizacao = f.UsuarioAtualizacao,
                DataCriacao = f.DataCriacao,
                DataAtualizacao = f.DataAtualizacao,
                Itens = new List<FichaTecnicaItemDto>(),
                Canais = f.Canais.Select(c => new FichaTecnicaCanalDto
                {
                    Id = c.Id,
                    FichaTecnicaId = f.Id,
                    CanalVendaId = c.CanalVendaId,
                    Canal = c.Canal,
                    NomeExibicao = c.NomeExibicao,
                    PrecoVenda = c.PrecoVenda,
                    TaxaPercentual = c.TaxaPercentual,
                    ComissaoPercentual = c.ComissaoPercentual,
                    Multiplicador = c.Multiplicador,
                    MargemCalculadaPercentual = c.MargemCalculadaPercentual,
                    Observacoes = c.Observacoes,
                    IsAtivo = c.IsAtivo
                }).ToList()
            })
            .ToListAsync();

        _logger.LogInformation("Encontradas {Total} fichas tecnicas", total);

        return new PagedResult<FichaTecnicaDto> { Items = items, Total = total };
    }

    public async Task<FichaTecnicaDto?> GetByIdAsync(long id)
    {
        _logger.LogInformation("Buscando Ficha Tecnica por ID: {Id}", id);

        var ficha = await _context.FichasTecnicas
            .AsNoTracking()
            .Include(f => f.Categoria)
            .Include(f => f.ReceitaPrincipal)
            .Include(f => f.PorcaoVendaUnidadeMedida)
            .Include(f => f.Itens)
                .ThenInclude(i => i.Insumo)
                    .ThenInclude(i => i.UnidadeCompra)
            .Include(f => f.Itens)
                .ThenInclude(i => i.Receita)
            .Include(f => f.Itens)
                .ThenInclude(i => i.UnidadeMedida)
            .Include(f => f.Canais)
                .ThenInclude(c => c.CanalVenda)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (ficha == null)
        {
            _logger.LogWarning("Ficha Tecnica com ID {Id} nao encontrada", id);
            return null;
        }

        return MapToDto(ficha);
    }

    public async Task<FichaTecnicaDto> CreateAsync(CreateFichaTecnicaRequest request, string? usuarioCriacao)
    {
        _logger.LogInformation("Criando nova Ficha Tecnica - Usuario: {Usuario}", usuarioCriacao ?? "Sistema");

        var categoriaExists = await _context.CategoriasReceita.AnyAsync(c => c.Id == request.CategoriaId && c.IsAtivo);
        if (!categoriaExists)
        {
            throw new BusinessException("Categoria invalida ou inativa");
        }

        if (request.Itens == null || !request.Itens.Any())
        {
            throw new BusinessException("A ficha tecnica deve ter pelo menos um item");
        }

        ValidarItensBasicos(request.Itens);

        var receitaIds = request.Itens
            .Where(i => i.TipoItem == "Receita" && i.ReceitaId.HasValue)
            .Select(i => i.ReceitaId!.Value)
            .Distinct()
            .ToList();

        if (request.ReceitaPrincipalId.HasValue)
        {
            receitaIds.Add(request.ReceitaPrincipalId.Value);
            receitaIds = receitaIds.Distinct().ToList();
        }

        var insumoIds = request.Itens
            .Where(i => i.TipoItem == "Insumo" && i.InsumoId.HasValue)
            .Select(i => i.InsumoId!.Value)
            .Distinct()
            .ToList();

        var unidadeMedidaIds = request.Itens
            .Select(i => i.UnidadeMedidaId)
            .Distinct()
            .ToList();

        var receitas = await _context.Receitas
            .Where(r => receitaIds.Contains(r.Id) && r.IsAtivo)
            .ToListAsync();

        if (request.ReceitaPrincipalId.HasValue && !receitas.Any(r => r.Id == request.ReceitaPrincipalId.Value))
        {
            throw new BusinessException("Receita principal invalida ou inativa");
        }

        if (receitas.Count != receitaIds.Count)
        {
            throw new BusinessException("Uma ou mais receitas sao invalidas ou estao inativas");
        }

        var insumos = await _context.Insumos
            .Include(i => i.UnidadeCompra)
            .Where(i => insumoIds.Contains(i.Id) && i.IsAtivo)
            .ToListAsync();

        if (insumos.Count != insumoIds.Count)
        {
            throw new BusinessException("Um ou mais insumos sao invalidos ou estao inativos");
        }

        var unidadesMedida = await _context.UnidadesMedida
            .Where(u => unidadeMedidaIds.Contains(u.Id) && u.IsAtivo)
            .ToListAsync();

        if (unidadesMedida.Count != unidadeMedidaIds.Count)
        {
            throw new BusinessException("Uma ou mais unidades de medida sao invalidas ou estao inativas");
        }

        ValidarUnidadesItens(request.Itens, insumos, unidadesMedida);
        await ValidarPorcaoVendaAsync(request.PorcaoVendaQuantidade, request.PorcaoVendaUnidadeMedidaId);

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
            RendimentoPorcoesNumero = request.RendimentoPorcoesNumero.HasValue && request.RendimentoPorcoesNumero.Value > 0
                ? request.RendimentoPorcoesNumero
                : null,
            TempoPreparo = request.TempoPreparo,
            IsAtivo = request.IsAtivo,
            UsuarioCriacao = usuarioCriacao ?? "Sistema",
            DataCriacao = DateTime.UtcNow
        };

        _logger.LogInformation("Criando {Count} itens para a ficha tecnica", request.Itens.Count);

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

        CalcularRendimentoFinal(ficha, unidadesMedida, receitas, insumos);
        CalcularCustosFichaTecnica(ficha, insumos, receitas, unidadesMedida);
        CalcularPrecoSugerido(ficha);

        if (request.Canais != null && request.Canais.Any())
        {
            foreach (var canalReq in request.Canais)
            {
                ficha.Canais.Add(new FichaTecnicaCanal
                {
                    CanalVendaId = canalReq.CanalVendaId,
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
        else
        {
            await CriarCanaisPadraoAsync(ficha);
        }

        CalcularPrecosCanais(ficha);

        _context.FichasTecnicas.Add(ficha);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Ficha Tecnica criada com sucesso - ID: {Id}", ficha.Id);

        return await GetByIdAsync(ficha.Id) ?? throw new InvalidOperationException("Erro ao buscar ficha tecnica criada");
    }

    public async Task<FichaTecnicaDto?> UpdateAsync(long id, UpdateFichaTecnicaRequest request, string? usuarioAtualizacao)
    {
        _logger.LogInformation("Atualizando Ficha Tecnica - ID: {Id}, Usuario: {Usuario}", id, usuarioAtualizacao ?? "Sistema");

        var ficha = await _context.FichasTecnicas
            .Include(f => f.Canais)
            .Include(f => f.Itens)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (ficha == null)
        {
            _logger.LogWarning("Ficha Tecnica com ID {Id} nao encontrada", id);
            return null;
        }

        var categoriaExists = await _context.CategoriasReceita.AnyAsync(c => c.Id == request.CategoriaId && c.IsAtivo);
        if (!categoriaExists)
        {
            throw new BusinessException("Categoria invalida ou inativa");
        }

        if (request.Itens == null || !request.Itens.Any())
        {
            throw new BusinessException("A ficha tecnica deve ter pelo menos um item");
        }

        ValidarItensBasicos(request.Itens);

        var receitaIds = request.Itens
            .Where(i => i.TipoItem == "Receita" && i.ReceitaId.HasValue)
            .Select(i => i.ReceitaId!.Value)
            .Distinct()
            .ToList();

        if (request.ReceitaPrincipalId.HasValue)
        {
            receitaIds.Add(request.ReceitaPrincipalId.Value);
            receitaIds = receitaIds.Distinct().ToList();
        }

        var insumoIds = request.Itens
            .Where(i => i.TipoItem == "Insumo" && i.InsumoId.HasValue)
            .Select(i => i.InsumoId!.Value)
            .Distinct()
            .ToList();

        var unidadeMedidaIds = request.Itens
            .Select(i => i.UnidadeMedidaId)
            .Distinct()
            .ToList();

        var receitas = await _context.Receitas
            .Where(r => receitaIds.Contains(r.Id) && r.IsAtivo)
            .ToListAsync();

        if (request.ReceitaPrincipalId.HasValue && !receitas.Any(r => r.Id == request.ReceitaPrincipalId.Value))
        {
            throw new BusinessException("Receita principal invalida ou inativa");
        }

        if (receitas.Count != receitaIds.Count)
        {
            throw new BusinessException("Uma ou mais receitas sao invalidas ou estao inativas");
        }

        var insumos = await _context.Insumos
            .Include(i => i.UnidadeCompra)
            .Where(i => insumoIds.Contains(i.Id) && i.IsAtivo)
            .ToListAsync();

        if (insumos.Count != insumoIds.Count)
        {
            throw new BusinessException("Um ou mais insumos sao invalidos ou estao inativos");
        }

        var unidadesMedida = await _context.UnidadesMedida
            .Where(u => unidadeMedidaIds.Contains(u.Id) && u.IsAtivo)
            .ToListAsync();

        if (unidadesMedida.Count != unidadeMedidaIds.Count)
        {
            throw new BusinessException("Uma ou mais unidades de medida sao invalidas ou estao inativas");
        }

        ValidarUnidadesItens(request.Itens, insumos, unidadesMedida);
        await ValidarPorcaoVendaAsync(request.PorcaoVendaQuantidade, request.PorcaoVendaUnidadeMedidaId);

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
        ficha.RendimentoPorcoesNumero = request.RendimentoPorcoesNumero.HasValue && request.RendimentoPorcoesNumero.Value > 0
            ? request.RendimentoPorcoesNumero
            : null;
        ficha.TempoPreparo = request.TempoPreparo;
        ficha.IsAtivo = request.IsAtivo;
        ficha.UsuarioAtualizacao = usuarioAtualizacao;
        ficha.DataAtualizacao = DateTime.UtcNow;

        var itensParaRemover = ficha.Itens.ToList();
        _context.FichaTecnicaItens.RemoveRange(itensParaRemover);
        ficha.Itens.Clear();

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

        CalcularRendimentoFinal(ficha, unidadesMedida, receitas, insumos);
        CalcularCustosFichaTecnica(ficha, insumos, receitas, unidadesMedida);
        CalcularPrecoSugerido(ficha);

        var canaisParaRemover = ficha.Canais.ToList();
        _context.FichaTecnicaCanais.RemoveRange(canaisParaRemover);
        ficha.Canais.Clear();

        if (request.Canais != null && request.Canais.Any())
        {
            foreach (var canalReq in request.Canais)
            {
                ficha.Canais.Add(new FichaTecnicaCanal
                {
                    CanalVendaId = canalReq.CanalVendaId,
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
        else
        {
            await CriarCanaisPadraoAsync(ficha);
        }

        CalcularPrecosCanais(ficha);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Ficha Tecnica atualizada com sucesso - ID: {Id}", id);

        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(long id)
    {
        _logger.LogInformation("Excluindo Ficha Tecnica - ID: {Id}", id);

        var ficha = await _context.FichasTecnicas
            .Include(f => f.Canais)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (ficha == null)
        {
            _logger.LogWarning("Ficha Tecnica com ID {Id} nao encontrada", id);
            return false;
        }

        _context.FichaTecnicaCanais.RemoveRange(ficha.Canais);
        _context.FichasTecnicas.Remove(ficha);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Ficha Tecnica excluida com sucesso - ID: {Id}", id);
        return true;
    }

    private static void ValidarItensBasicos(IEnumerable<CreateFichaTecnicaItemRequest> itens)
    {
        foreach (var item in itens)
        {
            if (item.TipoItem == "Receita" && !item.ReceitaId.HasValue)
            {
                throw new BusinessException("ReceitaId e obrigatorio quando TipoItem e 'Receita'");
            }
            if (item.TipoItem == "Insumo" && !item.InsumoId.HasValue)
            {
                throw new BusinessException("InsumoId e obrigatorio quando TipoItem e 'Insumo'");
            }
            if (item.TipoItem != "Receita" && item.TipoItem != "Insumo")
            {
                throw new BusinessException("TipoItem deve ser 'Receita' ou 'Insumo'");
            }
        }
    }

    private static void ValidarItensBasicos(IEnumerable<UpdateFichaTecnicaItemRequest> itens)
    {
        foreach (var item in itens)
        {
            if (item.TipoItem == "Receita" && !item.ReceitaId.HasValue)
            {
                throw new BusinessException("ReceitaId e obrigatorio quando TipoItem e 'Receita'");
            }
            if (item.TipoItem == "Insumo" && !item.InsumoId.HasValue)
            {
                throw new BusinessException("InsumoId e obrigatorio quando TipoItem e 'Insumo'");
            }
            if (item.TipoItem != "Receita" && item.TipoItem != "Insumo")
            {
                throw new BusinessException("TipoItem deve ser 'Receita' ou 'Insumo'");
            }
        }
    }

    private static void ValidarUnidadesItens<T>(IEnumerable<T> itens, List<Insumo> insumos, List<UnidadeMedida> unidadesMedida)
        where T : class
    {
        foreach (var item in itens)
        {
            string tipoItem;
            long? insumoId;
            long unidadeMedidaId;

            switch (item)
            {
                case CreateFichaTecnicaItemRequest createItem:
                    tipoItem = createItem.TipoItem;
                    insumoId = createItem.InsumoId;
                    unidadeMedidaId = createItem.UnidadeMedidaId;
                    break;
                case UpdateFichaTecnicaItemRequest updateItem:
                    tipoItem = updateItem.TipoItem;
                    insumoId = updateItem.InsumoId;
                    unidadeMedidaId = updateItem.UnidadeMedidaId;
                    break;
                default:
                    continue;
            }

            if (tipoItem == "Insumo" && insumoId.HasValue)
            {
                var insumo = insumos.FirstOrDefault(i => i.Id == insumoId.Value);
                if (insumo == null)
                {
                    throw new BusinessException($"Insumo com ID {insumoId.Value} nao encontrado.");
                }

                var unidadeItem = unidadesMedida.FirstOrDefault(u => u.Id == unidadeMedidaId);
                if (unidadeItem == null)
                {
                    throw new BusinessException($"Unidade de medida com ID {unidadeMedidaId} nao encontrada.");
                }

                if (unidadeMedidaId != insumo.UnidadeCompraId)
                {
                    var sigla = unidadeItem.Sigla.ToUpper();
                    if (sigla == "UN" && insumo.PesoPorUnidade.HasValue && insumo.PesoPorUnidade.Value > 0)
                    {
                        continue;
                    }

                    throw new BusinessException($"Unidade do item deve ser igual a unidade de medida do insumo '{insumo.Nome}'.");
                }
            }
            else if (tipoItem == "Receita")
            {
                var unidade = unidadesMedida.FirstOrDefault(u => u.Id == unidadeMedidaId);
                if (unidade == null)
                {
                    throw new BusinessException($"Unidade de medida com ID {unidadeMedidaId} nao encontrada.");
                }

                var sigla = unidade.Sigla.ToUpper();
                if (sigla != "GR" && sigla != "UN")
                {
                    throw new BusinessException("Itens do tipo Receita devem usar unidade de medida GR (gramas) ou UN (unidade).");
                }
            }
        }
    }

    private async Task ValidarPorcaoVendaAsync(decimal? quantidade, long? unidadeMedidaId)
    {
        if (quantidade.HasValue && quantidade.Value > 0 && !unidadeMedidaId.HasValue)
        {
            throw new BusinessException("PorcaoVendaUnidadeMedidaId e obrigatorio quando PorcaoVendaQuantidade esta preenchido.");
        }

        if (!unidadeMedidaId.HasValue)
        {
            return;
        }

        var porcaoUnidadeMedida = await _context.UnidadesMedida
            .FirstOrDefaultAsync(u => u.Id == unidadeMedidaId.Value);

        if (porcaoUnidadeMedida == null || !porcaoUnidadeMedida.IsAtivo)
        {
            throw new BusinessException("Unidade de medida da porcao de venda invalida ou inativa.");
        }

        var siglaPorcao = porcaoUnidadeMedida.Sigla.ToUpper();
        if (siglaPorcao != "GR" && siglaPorcao != "ML")
        {
            throw new BusinessException("Unidade de medida da porcao de venda deve ser GR (gramas) ou ML (mililitros).");
        }
    }

    /// <summary>
    /// Calcula o custo por unidade de uso de um insumo
    /// Formula: CustoUnitario / IPCValor (quando IPC informado)
    /// IPC representa quantidade aproveitavel na mesma unidade de uso
    /// Se IPCValor for null ou 0, calcula custo por unidade de compra: CustoUnitario / QuantidadePorEmbalagem
    /// </summary>
    private static decimal CalcularCustoPorUnidadeUso(Insumo insumo)
    {
        if (insumo.QuantidadePorEmbalagem <= 0 || insumo.CustoUnitario <= 0)
        {
            return 0;
        }

        if (insumo.IPCValor.HasValue && insumo.IPCValor.Value > 0)
        {
            return insumo.CustoUnitario / insumo.IPCValor.Value;
        }

        return insumo.CustoUnitario / insumo.QuantidadePorEmbalagem;
    }

    private static decimal AjustarPesoPorUnidade(decimal pesoPorUnidade, string? unidadeCompraSigla)
    {
        if (string.IsNullOrWhiteSpace(unidadeCompraSigla))
        {
            return pesoPorUnidade;
        }

        return unidadeCompraSigla.Trim().ToUpperInvariant() switch
        {
            "KG" => pesoPorUnidade / 1000m,
            "L" => pesoPorUnidade / 1000m,
            "GR" => pesoPorUnidade,
            "ML" => pesoPorUnidade,
            _ => pesoPorUnidade
        };
    }

    /// <summary>
    /// Calcula o rendimento final (peso comestivel total) aplicando IC e IPC.
    /// </summary>
    private static void CalcularRendimentoFinal(FichaTecnica ficha, List<UnidadeMedida> unidadesMedida, List<Receita> receitas, List<Insumo> insumos)
    {
        var receitasById = receitas.ToDictionary(r => r.Id, r => r);
        var insumosById = insumos.ToDictionary(i => i.Id, i => i);
        var unidadesById = unidadesMedida.ToDictionary(u => u.Id, u => u);

        var quantidadeTotalBase = 0m;

        foreach (var item in ficha.Itens)
        {
            if (item.TipoItem == "Receita" && item.ReceitaId.HasValue && receitasById.TryGetValue(item.ReceitaId.Value, out var receita))
            {
                if (receita.PesoPorPorcao.HasValue && receita.PesoPorPorcao.Value > 0)
                {
                    quantidadeTotalBase += item.Quantidade * receita.PesoPorPorcao.Value;
                }
                continue;
            }

            if (item.TipoItem != "Insumo")
            {
                continue;
            }

            if (!unidadesById.TryGetValue(item.UnidadeMedidaId, out var unidade))
            {
                continue;
            }

            var sigla = unidade.Sigla.ToUpper();

            if (sigla == "GR" || sigla == "ML")
            {
                quantidadeTotalBase += item.Quantidade;
            }
            else if (sigla == "KG" || sigla == "L")
            {
                quantidadeTotalBase += item.Quantidade * 1000m;
            }
            else if (sigla == "UN" && item.InsumoId.HasValue && insumosById.TryGetValue(item.InsumoId.Value, out var insumo))
            {
                if (insumo.PesoPorUnidade.HasValue && insumo.PesoPorUnidade.Value > 0)
                {
                    quantidadeTotalBase += item.Quantidade * insumo.PesoPorUnidade.Value;
                }
            }
        }

        if (quantidadeTotalBase <= 0)
        {
            ficha.RendimentoFinal = null;
            return;
        }

        var pesoAposCoccao = quantidadeTotalBase;
        if (ficha.ICOperador.HasValue && ficha.ICValor.HasValue && ficha.ICValor.Value > 0)
        {
            var icValor = Math.Clamp(ficha.ICValor.Value, 0, 9999);
            var icPercentual = icValor / 100m;

            if (ficha.ICOperador.Value == '+')
            {
                pesoAposCoccao = quantidadeTotalBase * (1m + icPercentual);
            }
            else if (ficha.ICOperador.Value == '-')
            {
                pesoAposCoccao = quantidadeTotalBase * (1m - icPercentual);
            }
        }

        var pesoComestivel = pesoAposCoccao;
        if (ficha.IPCValor.HasValue && ficha.IPCValor.Value > 0)
        {
            var ipcValor = Math.Clamp(ficha.IPCValor.Value, 0, 999);
            var ipcPercentual = ipcValor / 100m;
            pesoComestivel = pesoAposCoccao * ipcPercentual;
        }

        ficha.RendimentoFinal = pesoComestivel > 0 ? pesoComestivel : null;
    }

    /// <summary>
    /// Calcula os custos da ficha tecnica.
    /// </summary>
    private static void CalcularCustosFichaTecnica(FichaTecnica ficha, List<Insumo> insumos, List<Receita> receitas, List<UnidadeMedida> unidadesMedida)
    {
        var receitasById = receitas.ToDictionary(r => r.Id, r => r);
        var insumosById = insumos.ToDictionary(i => i.Id, i => i);
        var unidadesById = unidadesMedida.ToDictionary(u => u.Id, u => u);

        decimal custoTotal = 0;

        foreach (var item in ficha.Itens)
        {
            decimal custoLinha = 0;

            if (item.TipoItem == "Insumo" && item.InsumoId.HasValue && insumosById.TryGetValue(item.InsumoId.Value, out var insumo))
            {
                var custoPorUnidadeUso = CalcularCustoPorUnidadeUso(insumo);
                unidadesById.TryGetValue(item.UnidadeMedidaId, out var unidade);
                var sigla = unidade?.Sigla?.ToUpper() ?? string.Empty;

                if (sigla == "UN" && insumo.PesoPorUnidade.HasValue && insumo.PesoPorUnidade.Value > 0)
                {
                    // IMPORTANTE: PesoPorUnidade já está em g/ml (normalizado), não dividir por 1000
                    custoLinha = item.Quantidade * insumo.PesoPorUnidade.Value * custoPorUnidadeUso;
                }
                else
                {
                    custoLinha = item.Quantidade * custoPorUnidadeUso;
                }
            }
            else if (item.TipoItem == "Receita" && item.ReceitaId.HasValue && receitasById.TryGetValue(item.ReceitaId.Value, out var receita))
            {
                custoLinha = item.Quantidade * receita.CustoPorPorcao;
            }

            custoTotal += custoLinha;
        }

        ficha.CustoTotal = custoTotal;
        if (ficha.RendimentoFinal.HasValue && ficha.RendimentoFinal.Value > 0)
        {
            ficha.CustoPorUnidade = ficha.CustoTotal / ficha.RendimentoFinal.Value;
        }
        else
        {
            ficha.CustoPorUnidade = 0;
        }
    }

    private static decimal? CalcularCustoPorPorcao(FichaTecnica ficha)
    {
        if (ficha.PorcaoVendaQuantidade.HasValue && ficha.PorcaoVendaQuantidade.Value > 0)
        {
            if (ficha.RendimentoFinal.HasValue && ficha.RendimentoFinal.Value > 0)
            {
                var custoUnitario = ficha.CustoTotal / ficha.RendimentoFinal.Value;
                return custoUnitario * ficha.PorcaoVendaQuantidade.Value;
            }
            return null;
        }

        if (ficha.RendimentoPorcoesNumero.HasValue && ficha.RendimentoPorcoesNumero.Value > 0)
        {
            if (ficha.CustoTotal > 0)
            {
                return ficha.CustoTotal / ficha.RendimentoPorcoesNumero.Value;
            }
            return null;
        }

        if (ficha.CustoPorUnidade > 0)
        {
            // Modo legado: custo por unidade base (g/ml)
            return ficha.CustoPorUnidade;
        }

        return null;
    }

    /// <summary>
    /// Calcula preco sugerido da ficha tecnica (PrecoMesa)
    /// </summary>
    private static void CalcularPrecoSugerido(FichaTecnica ficha)
    {
        var custoPorPorcao = CalcularCustoPorPorcao(ficha);
        if (!custoPorPorcao.HasValue || custoPorPorcao.Value <= 0)
        {
            ficha.PrecoSugeridoVenda = null;
            return;
        }

        if (ficha.IndiceContabil.HasValue && ficha.IndiceContabil.Value > 0)
        {
            ficha.PrecoSugeridoVenda = custoPorPorcao.Value * ficha.IndiceContabil.Value;
            return;
        }

        ficha.PrecoSugeridoVenda = null;
    }

    private void CalcularPrecosCanais(FichaTecnica ficha)
    {
        var precoBase = ficha.PrecoSugeridoVenda ?? 0m;
        var custoPorPorcao = CalcularCustoPorPorcao(ficha) ?? 0m;

        foreach (var canal in ficha.Canais)
        {
            var taxaTotal = (canal.TaxaPercentual ?? 0m) + (canal.ComissaoPercentual ?? 0m);
            var taxaFator = taxaTotal / 100m;

            if (canal.Multiplicador.HasValue && canal.Multiplicador.Value > 0)
            {
                canal.PrecoVenda = precoBase * canal.Multiplicador.Value;
            }
            else if (taxaTotal > 0 && precoBase > 0)
            {
                var divisor = 1m - taxaFator;
                canal.PrecoVenda = divisor > 0 ? precoBase / divisor : 0m;
            }
            else if (canal.PrecoVenda <= 0)
            {
                canal.PrecoVenda = precoBase;
            }

            if (canal.PrecoVenda > 0)
            {
                var receitaLiquida = canal.PrecoVenda * (1m - taxaFator);
                canal.MargemCalculadaPercentual = receitaLiquida > 0
                    ? (receitaLiquida - custoPorPorcao) / receitaLiquida * 100m
                    : null;
            }
            else
            {
                canal.MargemCalculadaPercentual = null;
            }
        }
    }

    private async Task CriarCanaisPadraoAsync(FichaTecnica ficha)
    {
        var canaisVenda = await _context.CanaisVenda
            .AsNoTracking()
            .Where(c => c.IsAtivo)
            .OrderBy(c => c.Nome)
            .ToListAsync();

        foreach (var canal in canaisVenda)
        {
            ficha.Canais.Add(new FichaTecnicaCanal
            {
                CanalVendaId = canal.Id,
                Canal = canal.Nome,
                NomeExibicao = canal.Nome,
                PrecoVenda = 0,
                TaxaPercentual = canal.TaxaPercentualPadrao,
                ComissaoPercentual = null,
                Multiplicador = null,
                Observacoes = null,
                IsAtivo = true
            });
        }
    }

    private FichaTecnicaDto MapToDto(FichaTecnica ficha)
    {
        decimal? pesoTotalBase = null;
        decimal? custoKgL = null;
        decimal? custoKgBase = null;
        decimal? custoPorPorcaoVenda = null;

        var itens = ficha.Itens ?? new List<FichaTecnicaItem>();
        if (itens.Any())
        {
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
                    var receita = receitas.FirstOrDefault(r => r.Id == item.ReceitaId.Value);
                    if (receita != null && receita.PesoPorPorcao.HasValue && receita.PesoPorPorcao.Value > 0)
                    {
                        quantidadeTotalBase += item.Quantidade * receita.PesoPorPorcao.Value;
                    }
                }
                else if (item.TipoItem == "Insumo" && unidadeMedida != null)
                {
                    var sigla = unidadeMedida.Sigla.ToUpper();
                    if (sigla == "GR" || sigla == "ML")
                    {
                        quantidadeTotalBase += item.Quantidade;
                    }
                    else if (sigla == "KG" || sigla == "L")
                    {
                        quantidadeTotalBase += item.Quantidade * 1000m;
                    }
                    else if (sigla == "UN")
                    {
                        if (item.Insumo?.PesoPorUnidade.HasValue == true && item.Insumo.PesoPorUnidade.Value > 0)
                        {
                            quantidadeTotalBase += item.Quantidade * item.Insumo.PesoPorUnidade.Value;
                        }
                    }
                }
            }
            pesoTotalBase = quantidadeTotalBase;

            if (ficha.RendimentoFinal.HasValue && ficha.RendimentoFinal.Value > 0)
            {
                custoKgL = (ficha.CustoTotal / ficha.RendimentoFinal.Value) * 1000m;
            }

            if (pesoTotalBase.HasValue && pesoTotalBase.Value > 0)
            {
                custoKgBase = (ficha.CustoTotal / pesoTotalBase.Value) * 1000m;
            }

            // PRIORIDADE: "Vendido por unidade" tem prioridade sobre "vendido por porção (g/ml)"
            // Se rendimentoPorcoesNumero estiver preenchido, usa modo unidade
            if (ficha.RendimentoPorcoesNumero.HasValue && ficha.RendimentoPorcoesNumero.Value > 0 && ficha.CustoTotal > 0)
            {
                custoPorPorcaoVenda = ficha.CustoTotal / ficha.RendimentoPorcoesNumero.Value;
            }
            // Senão, usa modo porção (g/ml)
            else if (ficha.PorcaoVendaQuantidade.HasValue && ficha.PorcaoVendaQuantidade.Value > 0)
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
            RendimentoPorcoesNumero = ficha.RendimentoPorcoesNumero,
            TempoPreparo = ficha.TempoPreparo,
            PesoTotalBase = pesoTotalBase,
            CustoKgL = custoKgL,
            CustoKgBase = custoKgBase,
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
                    decimal? pesoPorUnidadeGml = null;
                    decimal pesoItemGml = 0m;

                    if (i.TipoItem == "Insumo" && i.InsumoId.HasValue && i.Insumo != null)
                    {
                        var custoPorUnidadeUso = CalcularCustoPorUnidadeUso(i.Insumo);
                        var sigla = i.UnidadeMedida?.Sigla?.ToUpper() ?? string.Empty;

                        if (sigla == "UN" && i.Insumo.PesoPorUnidade.HasValue && i.Insumo.PesoPorUnidade.Value > 0)
                        {
                            // IMPORTANTE: PesoPorUnidade já está em g/ml (normalizado), não dividir por 1000
                            custoItem = Math.Round(i.Quantidade * i.Insumo.PesoPorUnidade.Value * custoPorUnidadeUso, 4);
                            pesoPorUnidadeGml = i.Insumo.PesoPorUnidade.Value;
                            pesoItemGml = i.Quantidade * i.Insumo.PesoPorUnidade.Value;
                        }
                        else if (sigla == "GR" || sigla == "ML")
                        {
                            custoItem = Math.Round(i.Quantidade * custoPorUnidadeUso, 4);
                            pesoItemGml = i.Quantidade;
                        }
                        else if (sigla == "KG" || sigla == "L")
                        {
                            custoItem = Math.Round(i.Quantidade * custoPorUnidadeUso, 4);
                            pesoItemGml = i.Quantidade * 1000m;
                        }
                        else
                        {
                            custoItem = Math.Round(i.Quantidade * custoPorUnidadeUso, 4);
                        }
                    }
                    else if (i.TipoItem == "Receita" && i.ReceitaId.HasValue && i.Receita != null)
                    {
                        custoItem = Math.Round(i.Quantidade * i.Receita.CustoPorPorcao, 4);
                        if (i.Receita.PesoPorPorcao.HasValue)
                        {
                            pesoItemGml = i.Quantidade * i.Receita.PesoPorPorcao.Value;
                        }
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
                        PesoPorUnidadeGml = pesoPorUnidadeGml,
                        PesoItemGml = pesoItemGml,
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
                    CanalVendaId = c.CanalVendaId,
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
