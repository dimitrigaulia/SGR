namespace SGR.Api.Models.Tenant.Entities;

/// <summary>
/// Configuração comercial da ficha técnica por canal (ifood, balcão, etc.)
/// </summary>
public class FichaTecnicaCanal
{
    public long Id { get; set; }
    public long FichaTecnicaId { get; set; }

    public string Canal { get; set; } = string.Empty;
    public string? NomeExibicao { get; set; }

    public decimal PrecoVenda { get; set; }
    public decimal? TaxaPercentual { get; set; }
    public decimal? ComissaoPercentual { get; set; }

    /// <summary>
    /// Margem calculada em percentual, considerando custo técnico e taxas
    /// </summary>
    public decimal? MargemCalculadaPercentual { get; set; }

    public string? Observacoes { get; set; }
    public bool IsAtivo { get; set; }

    // Navegação
    public FichaTecnica FichaTecnica { get; set; } = null!;
}

