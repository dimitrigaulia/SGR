namespace SGR.Api.Models.Tenant.Entities;

/// <summary>
/// Item de uma receita (insumo utilizado)
/// </summary>
public class ReceitaItem
{
    public long Id { get; set; }
    public long ReceitaId { get; set; }
    public long InsumoId { get; set; }
    public decimal Quantidade { get; set; } // Quantidade na unidade de uso do insumo
    public int Ordem { get; set; } // Ordem de exibição (1, 2, 3...)
    public string? Observacoes { get; set; } // Notas específicas do item

    // Navegação
    public Receita Receita { get; set; } = null!;
    public Insumo Insumo { get; set; } = null!;
}

