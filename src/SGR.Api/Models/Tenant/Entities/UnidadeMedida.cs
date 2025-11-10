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
    public bool IsAtivo { get; set; }
    public string? UsuarioCriacao { get; set; }
    public string? UsuarioAtualizacao { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime? DataAtualizacao { get; set; }

    // Navegação
    public ICollection<Insumo> Insumos { get; set; } = new List<Insumo>();
}

