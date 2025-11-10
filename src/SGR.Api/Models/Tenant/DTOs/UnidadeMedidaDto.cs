namespace SGR.Api.Models.Tenant.DTOs;

public class UnidadeMedidaDto
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Sigla { get; set; } = string.Empty;
    public string? Tipo { get; set; }
    public long? UnidadeBaseId { get; set; }
    public decimal? FatorConversaoBase { get; set; }
    public bool IsAtivo { get; set; }
    public string? UsuarioCriacao { get; set; }
    public string? UsuarioAtualizacao { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime? DataAtualizacao { get; set; }
}

