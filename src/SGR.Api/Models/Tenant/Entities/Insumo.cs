namespace SGR.Api.Models.Tenant.Entities;

/// <summary>
/// Insumo do tenant (matÃ©rias-primas, produtos comprados, etc.)
/// </summary>
public class Insumo
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public long CategoriaId { get; set; }
    public long UnidadeCompraId { get; set; } // Unidade de medida do insumo (ex: g, mL, un)
	public decimal QuantidadePorEmbalagem { get; set; } // Quantidade na unidade de medida (ex: 12 unidades, 5 kg)
    public decimal? UnidadesPorEmbalagem { get; set; } // Quantidade de unidades contidas na embalagem (ex: 13 unidades em 1kg)
    public decimal? PesoPorUnidade { get; set; } // Peso/volume por unidade (em GR/ML) quando insumo for usado/consumido em UN
	public decimal CustoUnitario { get; set; } // Custo por unidade de compra
	public decimal FatorCorrecao { get; set; } = 1.0m; // Fator de perda/preparo (1.0 = sem perda, 1.10 = 10% de perda)
	public decimal? IPCValor { get; set; } // IPC (quantidade aproveitável): quantidade aproveitável na mesma unidade de medida (ex: 1000gr comprados, 650gr aproveitáveis = IPC 650)
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
    /// Custo por unidade de uso alternativo: CustoUnitario / IPCValor
    /// Exemplo: R$ 10,00 / 100 GR = R$ 0,10 / GR
    /// Se IPC não informado, calcula custo por unidade de compra: CustoUnitario / QuantidadePorEmbalagem
    /// </summary>
    public decimal CustoPorUnidadeUsoAlternativo
    {
        get
        {
            if (QuantidadePorEmbalagem <= 0 || CustoUnitario <= 0)
                return 0;
            
            // Se IPC informado, usar: CustoUnitario / IPCValor
            // IPC representa quantidade aproveitável na mesma unidade de uso
            if (IPCValor.HasValue && IPCValor.Value > 0)
            {
                return CustoUnitario / IPCValor.Value;
            }
            
            // Se IPC não informado, calcular custo por unidade de compra
            return CustoUnitario / QuantidadePorEmbalagem;
        }
    }

    /// <summary>
    /// Aproveitamento percentual (IPC / QuantidadePorEmbalagem * 100)
    /// </summary>
    public decimal? AproveitamentoPercentual
    {
        get
        {
            if (!IPCValor.HasValue || IPCValor.Value <= 0 || QuantidadePorEmbalagem <= 0)
            {
                return null;
            }

            return (IPCValor.Value / QuantidadePorEmbalagem) * 100m;
        }
    }

    /// <summary>
    /// Custo por unidade limpa (peça): (QuantidadePorEmbalagem / IPCValor) * (CustoUnitario / UnidadesPorEmbalagem)
    /// </summary>
    public decimal CustoPorUnidadeLimpa
    {
        get
        {
            if (!UnidadesPorEmbalagem.HasValue || UnidadesPorEmbalagem.Value <= 0)
            {
                return 0m;
            }

            var fatorBase = ObterFatorBase(UnidadeCompra?.Sigla);
            var quantidadeBase = QuantidadePorEmbalagem * fatorBase;

            if (quantidadeBase <= 0 || CustoUnitario <= 0)
            {
                return 0m;
            }

            var ipcValorBase = IPCValor.HasValue && IPCValor.Value > 0
                ? IPCValor.Value * fatorBase
                : quantidadeBase;

            if (ipcValorBase <= 0)
            {
                return 0m;
            }

            return (quantidadeBase / ipcValorBase) * (CustoUnitario / UnidadesPorEmbalagem.Value);
        }
    }

    // NavegaÃ§Ã£o
    public CategoriaInsumo Categoria { get; set; } = null!;
    public UnidadeMedida UnidadeCompra { get; set; } = null!;

    private static decimal ObterFatorBase(string? sigla)
    {
        if (string.IsNullOrWhiteSpace(sigla))
        {
            return 1m;
        }

        return sigla.Trim().ToUpperInvariant() switch
        {
            "KG" => 1000m,
            "L" => 1000m,
            "GR" => 1m,
            "ML" => 1m,
            _ => 1m
        };
    }
}

