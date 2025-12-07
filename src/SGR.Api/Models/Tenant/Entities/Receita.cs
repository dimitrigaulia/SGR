namespace SGR.Api.Models.Tenant.Entities;

/// <summary>
/// Receita do tenant
/// </summary>
public class Receita
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public long CategoriaId { get; set; }
    public string? Descricao { get; set; } // Instruções de preparo
    public string? InstrucoesEmpratamento { get; set; } // Instruções de como servir/empratar
    public decimal Rendimento { get; set; } // Quantas porções rende
    public decimal? PesoPorPorcao { get; set; } // Peso em gramas por porção
    public decimal FatorRendimento { get; set; } = 1.0m; // Fator de perdas no preparo (0-1: perdas, >1: ganho raro). Ex: 0.95 = 5% de perda
    public int? TempoPreparo { get; set; } // Tempo em minutos
    public string? Versao { get; set; } = "1.0"; // Versão da receita (ex: "1.0", "2.1")
    public decimal CustoTotal { get; set; } // Calculado automaticamente
    public decimal CustoPorPorcao { get; set; } // Calculado automaticamente (CustoTotal / Rendimento)
    public string? PathImagem { get; set; }
    public bool IsAtivo { get; set; }
    public string? UsuarioCriacao { get; set; }
    public DateTime DataCriacao { get; set; }
    public string? UsuarioAtualizacao { get; set; }
    public DateTime? DataAtualizacao { get; set; }

    // Navegação
    public CategoriaReceita Categoria { get; set; } = null!;
    public ICollection<ReceitaItem> Itens { get; set; } = new List<ReceitaItem>();
}

