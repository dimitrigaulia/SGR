namespace SGR.Api.Models.Tenant.Entities;

/// <summary>
/// Ficha técnica comercial de um prato/item comercial
/// </summary>
public class FichaTecnica
{
    public long Id { get; set; }
    public long CategoriaId { get; set; }

    public string Nome { get; set; } = string.Empty;
    public string? Codigo { get; set; }
    public string? DescricaoComercial { get; set; }

    /// <summary>
    /// Custo total da ficha técnica (soma dos custos dos itens)
    /// </summary>
    public decimal CustoTotal { get; set; }

    /// <summary>
    /// Custo por unidade de rendimento final
    /// </summary>
    public decimal CustoPorUnidade { get; set; }

    /// <summary>
    /// Rendimento final comercial (peso comestível total em gramas após IC e IPC)
    /// </summary>
    public decimal? RendimentoFinal { get; set; }

    /// <summary>
    /// Índice contábil / markup base aplicado sobre o custo por unidade
    /// </summary>
    public decimal? IndiceContabil { get; set; }

    /// <summary>
    /// Preço sugerido de venda por unidade, calculado a partir do custo por unidade e do índice contábil.
    /// </summary>
    public decimal? PrecoSugeridoVenda { get; set; }

    /// <summary>
    /// Operador do Índice de Cocção: '+' (aumenta) ou '-' (diminui)
    /// </summary>
    public char? ICOperador { get; set; }

    /// <summary>
    /// Valor do Índice de Cocção em percentual inteiro (ex: 15 = 15%)
    /// </summary>
    public int? ICValor { get; set; }

    /// <summary>
    /// Valor do Índice de Partes Comestíveis em percentual inteiro (ex: 80 = 80%)
    /// </summary>
    public int? IPCValor { get; set; }

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
    public CategoriaReceita Categoria { get; set; } = null!;
    public ICollection<FichaTecnicaItem> Itens { get; set; } = new List<FichaTecnicaItem>();
    public ICollection<FichaTecnicaCanal> Canais { get; set; } = new List<FichaTecnicaCanal>();
}

