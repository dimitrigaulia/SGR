namespace SGR.Api.Models.Tenant.Entities;

/// <summary>
/// Insumo do tenant (matÃ©rias-primas, produtos comprados, etc.)
/// </summary>
public class Insumo
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public long CategoriaId { get; set; }
    public long UnidadeCompraId { get; set; } // Unidade em que o insumo Ã© comprado (ex: Caixa, Saco 5kg)
    public long UnidadeUsoId { get; set; } // Unidade usada nas receitas (ex: kg, unidade)
	public decimal QuantidadePorEmbalagem { get; set; } // Quantidade na unidade de compra (ex: 12 unidades, 5 kg)
	public decimal CustoUnitario { get; set; } // Custo por unidade de compra
	public decimal FatorCorrecao { get; set; } = 1.0m; // Fator de perda/preparo (1.0 = sem perda, 1.10 = 10% de perda)
	public int? IPCValor { get; set; } // Índice de Partes Comestíveis: quantidade aproveitável na mesma unidade de uso (ex: 1000gr comprados, 650gr aproveitáveis = IPC 650)
    public string? Descricao { get; set; }
    public string? PathImagem { get; set; }
    public bool IsAtivo { get; set; }
    public string? UsuarioCriacao { get; set; }
    public DateTime DataCriacao { get; set; }
    public string? UsuarioAtualizacao { get; set; }
    public DateTime? DataAtualizacao { get; set; }

    // Propriedades calculadas
    /// <summary>
    /// Fator de ajuste pelo IPC: QuantidadePorEmbalagem / IPCValor
    /// Exemplo: 1000gr comprados, 650gr aproveitáveis → 1000 / 650 = 1,538
    /// Se IPCValor for null ou 0, retorna 1 (assume 100% aproveitável)
    /// </summary>
    public decimal QuantidadeAjustadaIPC
    {
        get
        {
            if (QuantidadePorEmbalagem <= 0) return 0;
            var divisor = IPCValor ?? 1;
            if (divisor <= 0) divisor = 1;
            return QuantidadePorEmbalagem / divisor;
        }
    }

    /// <summary>
    /// Custo por unidade de uso alternativo: CustoUnitario * (QuantidadePorEmbalagem / IPCValor)
    /// Exemplo: R$ 10,00 * (1000 / 650) = R$ 15,38
    /// </summary>
    public decimal CustoPorUnidadeUsoAlternativo
    {
        get
        {
            return CustoUnitario * QuantidadeAjustadaIPC;
        }
    }

    // NavegaÃ§Ã£o
    public CategoriaInsumo Categoria { get; set; } = null!;
    public UnidadeMedida UnidadeCompra { get; set; } = null!;
    public UnidadeMedida UnidadeUso { get; set; } = null!;
}

