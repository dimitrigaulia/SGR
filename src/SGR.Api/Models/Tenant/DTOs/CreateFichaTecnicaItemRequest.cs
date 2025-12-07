using System.ComponentModel.DataAnnotations;

namespace SGR.Api.Models.Tenant.DTOs;

public class CreateFichaTecnicaItemRequest
{
    [Required(ErrorMessage = "TipoItem é obrigatório")]
    [RegularExpression("^(Receita|Insumo)$", ErrorMessage = "TipoItem deve ser 'Receita' ou 'Insumo'")]
    public string TipoItem { get; set; } = string.Empty;

    public long? ReceitaId { get; set; }
    public long? InsumoId { get; set; }

    [Required(ErrorMessage = "Quantidade é obrigatória")]
    [Range(0.0001, double.MaxValue, ErrorMessage = "Quantidade deve ser maior que zero")]
    public decimal Quantidade { get; set; }

    [Required(ErrorMessage = "UnidadeMedidaId é obrigatório")]
    public long UnidadeMedidaId { get; set; }

    public bool ExibirComoQB { get; set; }

    [Required(ErrorMessage = "Ordem é obrigatória")]
    [Range(1, int.MaxValue, ErrorMessage = "Ordem deve ser maior que zero")]
    public int Ordem { get; set; }

    public string? Observacoes { get; set; }
}



