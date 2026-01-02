namespace SGR.Api.Models.Tenant.Entities;

/// <summary>
/// Canal de venda/comercial do tenant (ex: iFood, Balcão, Delivery próprio, etc.)
/// </summary>
public class CanalVenda
{
    public long Id { get; set; }
    
    /// <summary>
    /// Nome do canal (ex: iFood 1, iFood 2, Balcão, Delivery Próprio)
    /// </summary>
    public string Nome { get; set; } = string.Empty;
    
    /// <summary>
    /// Taxa percentual padrão do canal (ex: 13 = 13%)
    /// </summary>
    public decimal? TaxaPercentualPadrao { get; set; }
    
    /// <summary>
    /// Indica se o canal está ativo
    /// </summary>
    public bool IsAtivo { get; set; }
    
    // Campos de auditoria
    public string? UsuarioCriacao { get; set; }
    public string? UsuarioAtualizacao { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime? DataAtualizacao { get; set; }
    
    // Navegação
    public ICollection<FichaTecnicaCanal> FichaTecnicaCanais { get; set; } = new List<FichaTecnicaCanal>();
}
