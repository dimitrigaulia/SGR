namespace SGR.Api.Models.Tenant.Entities;

/// <summary>
/// Insumo do tenant (matérias-primas, produtos comprados, etc.)
/// </summary>
public class Insumo
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public long CategoriaId { get; set; }
    public long UnidadeCompraId { get; set; } // Unidade em que o insumo é comprado (ex: Caixa, Saco 5kg)
    public long UnidadeUsoId { get; set; } // Unidade usada nas receitas (ex: kg, unidade)
	public decimal QuantidadePorEmbalagem { get; set; } // Quantidade na unidade de compra (ex: 12 unidades, 5 kg)
	public decimal CustoUnitario { get; set; } // Custo por unidade de compra
	public decimal FatorCorrecao { get; set; } = 1.0m; // Fator de perda/preparo (1.0 = sem perda, 1.10 = 10% de perda)
    public string? Descricao { get; set; }
    public string? PathImagem { get; set; }
    public bool IsAtivo { get; set; }
    public string? UsuarioCriacao { get; set; }
    public DateTime DataCriacao { get; set; }
    public string? UsuarioAtualizacao { get; set; }
    public DateTime? DataAtualizacao { get; set; }

    // Navegação
    public CategoriaInsumo Categoria { get; set; } = null!;
    public UnidadeMedida UnidadeCompra { get; set; } = null!;
    public UnidadeMedida UnidadeUso { get; set; } = null!;
}

