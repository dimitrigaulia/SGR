namespace SGR.Api.Models.Tenant.DTOs;

public class InsumoDto
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public long CategoriaId { get; set; }
    public string? CategoriaNome { get; set; }
    public long UnidadeCompraId { get; set; }
    public string? UnidadeCompraNome { get; set; }
    public string? UnidadeCompraSigla { get; set; }
    public decimal QuantidadePorEmbalagem { get; set; }
    public decimal? UnidadesPorEmbalagem { get; set; }
    public decimal? PesoPorUnidade { get; set; }
    public decimal CustoUnitario { get; set; }
    public decimal FatorCorrecao { get; set; }
    public decimal? IpcValor { get; set; }
    public decimal QuantidadeAjustadaIPC { get; set; }
    public decimal CustoPorUnidadeUsoAlternativo { get; set; }
    public decimal? AproveitamentoPercentual { get; set; }
    public decimal CustoPorUnidadeLimpa { get; set; }
    public string? Descricao { get; set; }
    public string? PathImagem { get; set; }
    public bool IsAtivo { get; set; }
    public string? UsuarioAtualizacao { get; set; }
    public DateTime? DataAtualizacao { get; set; }
}
