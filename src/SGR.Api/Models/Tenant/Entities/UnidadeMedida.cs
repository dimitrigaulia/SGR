namespace SGR.Api.Models.Tenant.Entities;

/// <summary>
/// Unidade de medida do tenant (ex: Quilograma, Litro, Unidade, etc.)
/// </summary>
public class UnidadeMedida
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty; // Ex: "Quilograma"
    public string Sigla { get; set; } = string.Empty; // Ex: "kg"
    public string? Tipo { get; set; } // Ex: "Peso", "Volume", "Quantidade" (para agrupamento)
    public long? UnidadeBaseId { get; set; } // Referência à unidade base do mesmo tipo (ex: g -> kg)
    public decimal? FatorConversaoBase { get; set; } // Fator para converter para unidade base (ex: 1g = 0.001kg)
    public bool IsAtivo { get; set; }
    public string? UsuarioCriacao { get; set; }
    public string? UsuarioAtualizacao { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime? DataAtualizacao { get; set; }

    // Navegação
    public UnidadeMedida? UnidadeBase { get; set; }
    public ICollection<Insumo> Insumos { get; set; } = new List<Insumo>();
}

