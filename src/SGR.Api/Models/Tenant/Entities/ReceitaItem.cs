namespace SGR.Api.Models.Tenant.Entities;

/// <summary>
/// Item de uma receita (insumo utilizado)
/// </summary>
public class ReceitaItem
{
    public long Id { get; set; }
    public long ReceitaId { get; set; }
    public long InsumoId { get; set; }
    
    /// <summary>
    /// Quantidade na unidade de medida especificada
    /// </summary>
    public decimal Quantidade { get; set; }
    
    /// <summary>
    /// Unidade de medida usada para este item
    /// </summary>
    public long UnidadeMedidaId { get; set; }
    
    /// <summary>
    /// Se true, exibir como "QB" (Quantidade a Bel Prazer) na UI e PDF
    /// </summary>
    public bool ExibirComoQB { get; set; }
    
    /// <summary>
    /// Ordem de exibiÃ§Ã£o (1, 2, 3...)
    /// </summary>
    public int Ordem { get; set; }
    
    /// <summary>
    /// ObservaÃ§Ãµes especÃ­ficas do item
    /// </summary>
    public string? Observacoes { get; set; }

    // NavegaÃ§Ã£o
    public Receita Receita { get; set; } = null!;
    public Insumo Insumo { get; set; } = null!;
    public UnidadeMedida UnidadeMedida { get; set; } = null!;
}

