namespace SGR.Api.Models.Tenant.DTOs;

public class ReceitaItemDto
{
    public long Id { get; set; }
    public long ReceitaId { get; set; }
    public long InsumoId { get; set; }
    public string? InsumoNome { get; set; }
    public string? InsumoCategoriaNome { get; set; }
    public string? UnidadeUsoNome { get; set; }
    public string? UnidadeUsoSigla { get; set; }
    public decimal Quantidade { get; set; }
    public decimal QuantidadeBruta { get; set; } // Quantidade × FatorCorrecao (calculado)
    public decimal CustoItem { get; set; } // Custo calculado do item
    public int Ordem { get; set; }
    public string? Observacoes { get; set; }
}

