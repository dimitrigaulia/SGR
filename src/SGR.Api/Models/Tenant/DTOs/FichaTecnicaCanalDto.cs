namespace SGR.Api.Models.Tenant.DTOs;

public class FichaTecnicaCanalDto
{
    public long Id { get; set; }
    public long FichaTecnicaId { get; set; }
    public string Canal { get; set; } = string.Empty;
    public string? NomeExibicao { get; set; }
    public decimal PrecoVenda { get; set; }
    public decimal? TaxaPercentual { get; set; }
    public decimal? ComissaoPercentual { get; set; }
    public decimal? MargemCalculadaPercentual { get; set; }
    public string? Observacoes { get; set; }
    public bool IsAtivo { get; set; }
}

