namespace SGR.Api.Models.Tenant.DTOs;

public class FichaTecnicaItemDto
{
    public long Id { get; set; }
    public long FichaTecnicaId { get; set; }
    public string TipoItem { get; set; } = string.Empty; // "Receita" ou "Insumo"
    public long? ReceitaId { get; set; }
    public string? ReceitaNome { get; set; }
    public long? InsumoId { get; set; }
    public string? InsumoNome { get; set; }
    public decimal Quantidade { get; set; }
    public long UnidadeMedidaId { get; set; }
    public string? UnidadeMedidaNome { get; set; }
    public string? UnidadeMedidaSigla { get; set; }
    public bool ExibirComoQB { get; set; }
    public int Ordem { get; set; }
    public string? Observacoes { get; set; }
    public decimal CustoItem { get; set; }
    
    // Campos para exibir peso real dos itens
    public decimal? PesoPorUnidadeGml { get; set; } // Para insumos UN, peso unitário em g/ml
    public decimal PesoItemGml { get; set; } // Peso total do item em g/ml (quantidade × peso unitário)
    
    public string? UsuarioCriacao { get; set; }
    public string? UsuarioAtualizacao { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime? DataAtualizacao { get; set; }
}



