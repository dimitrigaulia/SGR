namespace SGR.Api.Models.Tenant.DTOs;

public class InsumoDto
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public long CategoriaId { get; set; }
    public string? CategoriaNome { get; set; }
    public long UnidadeMedidaId { get; set; }
    public string? UnidadeMedidaNome { get; set; }
    public string? UnidadeMedidaSigla { get; set; }
    public decimal CustoUnitario { get; set; }
    public decimal? EstoqueMinimo { get; set; }
    public string? Descricao { get; set; }
    public string? CodigoBarras { get; set; }
    public string? PathImagem { get; set; }
    public bool IsAtivo { get; set; }
    public string? UsuarioAtualizacao { get; set; }
    public DateTime? DataAtualizacao { get; set; }
}

