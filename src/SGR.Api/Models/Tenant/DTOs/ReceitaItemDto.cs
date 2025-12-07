namespace SGR.Api.Models.Tenant.DTOs;

public class ReceitaItemDto
{
    public long Id { get; set; }
    public long ReceitaId { get; set; }
    public long InsumoId { get; set; }
    public string? InsumoNome { get; set; }
    public string? InsumoCategoriaNome { get; set; }
    public long UnidadeMedidaId { get; set; }
    public string? UnidadeMedidaNome { get; set; }
    public string? UnidadeMedidaSigla { get; set; }
    public decimal Quantidade { get; set; }
    public decimal QuantidadeBruta { get; set; } // Quantidade Ã— FatorCorrecao (calculado)
    public decimal CustoItem { get; set; } // Custo calculado do item
    public decimal? CustoPorUnidadeUso { get; set; } // Custo por 1 unidade de uso
    public decimal? CustoPor100UnidadesUso { get; set; } // Custo por 100 unidades de uso (peso/volume)
    public bool ExibirComoQB { get; set; }
    public int Ordem { get; set; }
    public string? Observacoes { get; set; }
}

