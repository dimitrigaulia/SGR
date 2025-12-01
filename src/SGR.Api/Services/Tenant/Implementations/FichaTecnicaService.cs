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
        _logger.LogInformation("Buscando Fichas Técnicas - Página: {Page}, Tamanho: {PageSize}, Busca: {Search}", page, pageSize, search ?? "N/A");

        var query = _context.FichasTecnicas
            .Include(f => f.Receita)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.ToLower();
            query = query.Where(f =>
                EF.Functions.ILike(f.Nome, $"%{search}%") ||
                (f.Codigo != null && EF.Functions.ILike(f.Codigo, $"%{search}%")) ||
                (f.Receita != null && EF.Functions.ILike(f.Receita.Nome, $"%{search}%")));
        }

        var ascending = string.Equals(order, "asc", StringComparison.OrdinalIgnoreCase);
        query = (sort?.ToLower()) switch
        {
            "nome" => ascending ? query.OrderBy(f => f.Nome) : query.OrderByDescending(f => f.Nome),
            "codigo" => ascending ? query.OrderBy(f => f.Codigo) : query.OrderByDescending(f => f.Codigo),
            "receita" => ascending ? query.OrderBy(f => f.Receita.Nome) : query.OrderByDescending(f => f.Receita.Nome),
            "ativo" or "isativo" => ascending ? query.OrderBy(f => f.IsAtivo) : query.OrderByDescending(f => f.IsAtivo),
            _ => query.OrderBy(f => f.Nome)
        };

        var total = await query.CountAsync();
        var items = await query
            .Skip(Math.Max(0, (page - 1) * pageSize))
            .Take(pageSize)
            .Include(f => f.Receita)
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
            .Include(f => f.Receita)
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

        var receita = await _context.Receitas.FirstOrDefaultAsync(r => r.Id == request.ReceitaId);
        if (receita == null || !receita.IsAtivo)
        {
            throw new BusinessException("Receita inválida ou inativa");
        }

        if (request.Canais == null || !request.Canais.Any())
        {
            throw new BusinessException("A ficha técnica deve ter pelo menos um canal");
        }

        var ficha = new FichaTecnica
        {
            ReceitaId = request.ReceitaId,
            Nome = request.Nome,
            Codigo = request.Codigo,
            DescricaoComercial = request.DescricaoComercial,
            RendimentoFinal = request.RendimentoFinal,
            IndiceContabil = request.IndiceContabil,
            PrecoSugeridoVenda = null,
            MargemAlvoPercentual = request.MargemAlvoPercentual,
            IsAtivo = request.IsAtivo,
            UsuarioCriacao = usuarioCriacao ?? "Sistema",
            DataCriacao = DateTime.UtcNow
        };

        foreach (var canalReq in request.Canais)
        {
            ficha.Canais.Add(new FichaTecnicaCanal
            {
                Canal = canalReq.Canal,
                NomeExibicao = canalReq.NomeExibicao,
                PrecoVenda = canalReq.PrecoVenda,
                TaxaPercentual = canalReq.TaxaPercentual,
                ComissaoPercentual = canalReq.ComissaoPercentual,
                IsAtivo = canalReq.IsAtivo,
                Observacoes = canalReq.Observacoes
            });
        }

        AtualizarMargens(ficha, receita);

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
            .Include(f => f.Receita)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (ficha == null)
        {
            _logger.LogWarning("Ficha Técnica com ID {Id} não encontrada", id);
            return null;
        }

        if (ficha.ReceitaId != request.ReceitaId)
        {
            var receitaNova = await _context.Receitas.FirstOrDefaultAsync(r => r.Id == request.ReceitaId);
            if (receitaNova == null || !receitaNova.IsAtivo)
            {
                throw new BusinessException("Receita inválida ou inativa");
            }
            ficha.ReceitaId = request.ReceitaId;
            ficha.Receita = receitaNova;
        }

        if (request.Canais == null || !request.Canais.Any())
        {
            throw new BusinessException("A ficha técnica deve ter pelo menos um canal");
        }

        ficha.Nome = request.Nome;
        ficha.Codigo = request.Codigo;
        ficha.DescricaoComercial = request.DescricaoComercial;
        ficha.RendimentoFinal = request.RendimentoFinal;
        ficha.IndiceContabil = request.IndiceContabil;
        ficha.PrecoSugeridoVenda = null;
        ficha.MargemAlvoPercentual = request.MargemAlvoPercentual;
        ficha.IsAtivo = request.IsAtivo;
        ficha.UsuarioAtualizacao = usuarioAtualizacao;
        ficha.DataAtualizacao = DateTime.UtcNow;

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

        var receita = ficha.Receita ?? await _context.Receitas.FirstAsync(r => r.Id == ficha.ReceitaId);
        AtualizarMargens(ficha, receita);

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

    private void AtualizarMargens(FichaTecnica ficha, Receita receita)
    {
        var custoPorPorcao = receita.CustoPorPorcao;

        // Preço sugerido de venda por porção, se houver índice contábil
        if (ficha.IndiceContabil.HasValue && ficha.IndiceContabil.Value > 0 && custoPorPorcao > 0)
        {
            ficha.PrecoSugeridoVenda = Math.Round(custoPorPorcao * ficha.IndiceContabil.Value, 4);
        }
        else
        {
            ficha.PrecoSugeridoVenda = null;
        }

        foreach (var canal in ficha.Canais)
        {
            // Se preço de venda vier 0 e tivermos preço sugerido, calcular automaticamente a partir dele
            if (canal.PrecoVenda <= 0 && ficha.PrecoSugeridoVenda.HasValue)
            {
                var taxa = (canal.TaxaPercentual ?? 0m) / 100m;
                var comissao = (canal.ComissaoPercentual ?? 0m) / 100m;
                var fatorTaxas = 1 + taxa + comissao;
                canal.PrecoVenda = Math.Round(ficha.PrecoSugeridoVenda.Value * fatorTaxas, 4);
            }

            if (canal.PrecoVenda <= 0 || custoPorPorcao <= 0)
            {
                canal.MargemCalculadaPercentual = null;
                continue;
            }

            var taxa = (canal.TaxaPercentual ?? 0m) / 100m;
            var comissao = (canal.ComissaoPercentual ?? 0m) / 100m;

            var custoComTaxas = custoPorPorcao * (1 + taxa + comissao);
            var margem = (canal.PrecoVenda - custoComTaxas) / canal.PrecoVenda * 100m;

            canal.MargemCalculadaPercentual = Math.Round(margem, 2);
        }
    }

    private FichaTecnicaDto MapToDto(FichaTecnica ficha)
    {
        var receita = ficha.Receita;

        return new FichaTecnicaDto
        {
            Id = ficha.Id,
            ReceitaId = ficha.ReceitaId,
            ReceitaNome = receita?.Nome ?? string.Empty,
            Nome = ficha.Nome,
            Codigo = ficha.Codigo,
            DescricaoComercial = ficha.DescricaoComercial,
            RendimentoFinal = ficha.RendimentoFinal,
            IndiceContabil = ficha.IndiceContabil,
            PrecoSugeridoVenda = ficha.PrecoSugeridoVenda,
            MargemAlvoPercentual = ficha.MargemAlvoPercentual,
            CustoTecnicoTotal = receita?.CustoTotal ?? 0m,
            CustoTecnicoPorPorcao = receita?.CustoPorPorcao ?? 0m,
            IsAtivo = ficha.IsAtivo,
            UsuarioCriacao = ficha.UsuarioCriacao,
            UsuarioAtualizacao = ficha.UsuarioAtualizacao,
            DataCriacao = ficha.DataCriacao,
            DataAtualizacao = ficha.DataAtualizacao,
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
