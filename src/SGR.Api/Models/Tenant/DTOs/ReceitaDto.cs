namespace SGR.Api.Models.Tenant.DTOs;

public class ReceitaDto
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public long CategoriaId { get; set; }
    public string? CategoriaNome { get; set; }
    public string? Descricao { get; set; }
    public string? InstrucoesEmpratamento { get; set; }
    public decimal Rendimento { get; set; }
    public decimal? PesoPorPorcao { get; set; }
    public decimal FatorRendimento { get; set; }
    public string? IcSinal { get; set; }
    public int? IcValor { get; set; }
    public int? TempoPreparo { get; set; }
    public string? Versao { get; set; }
    public decimal CustoTotal { get; set; }
    public decimal CustoPorPorcao { get; set; }
    public string? PathImagem { get; set; }
    public bool IsAtivo { get; set; }
    public string? UsuarioCriacao { get; set; }
    public string? UsuarioAtualizacao { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime? DataAtualizacao { get; set; }
    public List<ReceitaItemDto> Itens { get; set; } = new List<ReceitaItemDto>();
}

