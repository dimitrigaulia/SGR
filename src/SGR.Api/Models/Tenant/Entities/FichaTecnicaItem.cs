namespace SGR.Api.Models.Tenant.Entities;

/// <summary>
/// Item de uma ficha técnica (pode ser uma Receita ou um Insumo)
/// </summary>
public class FichaTecnicaItem
{
    public long Id { get; set; }
    public long FichaTecnicaId { get; set; }
    
    /// <summary>
    /// Tipo do item: "Receita" ou "Insumo"
    /// </summary>
    public string TipoItem { get; set; } = string.Empty;
    
    /// <summary>
    /// ID da Receita (preenchido quando TipoItem = "Receita")
    /// </summary>
    public long? ReceitaId { get; set; }
    
    /// <summary>
    /// ID do Insumo (preenchido quando TipoItem = "Insumo")
    /// </summary>
    public long? InsumoId { get; set; }
    
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
    /// Ordem de exibição (1, 2, 3...)
    /// </summary>
    public int Ordem { get; set; }
    
    /// <summary>
    /// Observações específicas do item
    /// </summary>
    public string? Observacoes { get; set; }
    
    public string? UsuarioCriacao { get; set; }
    public DateTime DataCriacao { get; set; }
    public string? UsuarioAtualizacao { get; set; }
    public DateTime? DataAtualizacao { get; set; }
    
    // Navegação
    public FichaTecnica FichaTecnica { get; set; } = null!;
    public Receita? Receita { get; set; }
    public Insumo? Insumo { get; set; }
    public UnidadeMedida UnidadeMedida { get; set; } = null!;
}



