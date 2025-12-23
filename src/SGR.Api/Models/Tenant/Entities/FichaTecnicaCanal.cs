namespace SGR.Api.Models.Tenant.Entities;

/// <summary>
/// ConfiguraÃ§Ã£o comercial da ficha tÃ©cnica por canal (ifood, balcÃ£o, etc.)
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
    /// Multiplicador fixo para cálculo de preço (prioridade sobre gross-up por fee)
    /// </summary>
    public decimal? Multiplicador { get; set; }

    /// <summary>
    /// Margem calculada em percentual, considerando custo tÃ©cnico e taxas
    /// </summary>
    public decimal? MargemCalculadaPercentual { get; set; }

    public string? Observacoes { get; set; }
    public bool IsAtivo { get; set; }

    // NavegaÃ§Ã£o
    public FichaTecnica FichaTecnica { get; set; } = null!;
}

