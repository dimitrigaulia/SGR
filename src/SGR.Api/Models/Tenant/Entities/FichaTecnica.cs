namespace SGR.Api.Models.Tenant.Entities;

/// <summary>
/// Ficha técnica comercial de uma receita
/// </summary>
public class FichaTecnica
{
    public long Id { get; set; }
    public long ReceitaId { get; set; }

    public string Nome { get; set; } = string.Empty;
    public string? Codigo { get; set; }
    public string? DescricaoComercial { get; set; }

    /// <summary>
    /// Rendimento final comercial (porções vendidas)
    /// </summary>
    public decimal? RendimentoFinal { get; set; }

    /// <summary>
    /// Índice contábil / markup base aplicado sobre o custo técnico
    /// </summary>
    public decimal? IndiceContabil { get; set; }

    /// <summary>
    /// Preço sugerido de venda por porção, calculado a partir do custo técnico por porção e do índice contábil.
    /// </summary>
    public decimal? PrecoSugeridoVenda { get; set; }

    /// <summary>
    /// Margem alvo em percentual (ex: 60 = 60%)
    /// </summary>
    public decimal? MargemAlvoPercentual { get; set; }

    public bool IsAtivo { get; set; }

    public string? UsuarioCriacao { get; set; }
    public DateTime DataCriacao { get; set; }
    public string? UsuarioAtualizacao { get; set; }
    public DateTime? DataAtualizacao { get; set; }

    // Navegação
    public Receita Receita { get; set; } = null!;
    public ICollection<FichaTecnicaCanal> Canais { get; set; } = new List<FichaTecnicaCanal>();
}

