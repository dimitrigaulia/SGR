namespace SGR.Api.Models.Tenant.Entities;

/// <summary>
/// Insumo do tenant (matérias-primas, produtos comprados, etc.)
/// </summary>
public class Insumo
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public long CategoriaId { get; set; }
    public long UnidadeMedidaId { get; set; }
    public decimal CustoUnitario { get; set; }
    public decimal? EstoqueMinimo { get; set; }
    public string? Descricao { get; set; }
    public string? CodigoBarras { get; set; }
    public string? PathImagem { get; set; }
    public bool IsAtivo { get; set; }
    public string? UsuarioCriacao { get; set; }
    public DateTime DataCriacao { get; set; }
    public string? UsuarioAtualizacao { get; set; }
    public DateTime? DataAtualizacao { get; set; }

    // Navegação
    public CategoriaInsumo Categoria { get; set; } = null!;
    public UnidadeMedida UnidadeMedida { get; set; } = null!;
}

