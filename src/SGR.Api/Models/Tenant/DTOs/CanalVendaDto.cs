namespace SGR.Api.Models.Tenant.DTOs;

public class CanalVendaDto
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public decimal? TaxaPercentualPadrao { get; set; }
    public bool IsAtivo { get; set; }
    public string? UsuarioCriacao { get; set; }
    public string? UsuarioAtualizacao { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime? DataAtualizacao { get; set; }
}
