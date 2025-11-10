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
    public long UnidadeUsoId { get; set; }
    public string? UnidadeUsoNome { get; set; }
    public string? UnidadeUsoSigla { get; set; }
    public string? UnidadeUsoTipo { get; set; } // Tipo da unidade de uso (Peso, Volume, Quantidade)
    public decimal QuantidadePorEmbalagem { get; set; }
    public decimal CustoUnitario { get; set; }
    public decimal FatorCorrecao { get; set; }
    public decimal? EstoqueMinimo { get; set; }
    public string? Descricao { get; set; }
    public string? CodigoBarras { get; set; }
    public string? PathImagem { get; set; }
    public bool IsAtivo { get; set; }
    public string? UsuarioAtualizacao { get; set; }
    public DateTime? DataAtualizacao { get; set; }
}

