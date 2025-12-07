using System.ComponentModel.DataAnnotations;

namespace SGR.Api.Models.Tenant.DTOs;

public class UpdateFichaTecnicaItemRequest
{
    [Required(ErrorMessage = "TipoItem Ã© obrigatÃ³rio")]
    [RegularExpression("^(Receita|Insumo)$", ErrorMessage = "TipoItem deve ser 'Receita' ou 'Insumo'")]
    public string TipoItem { get; set; } = string.Empty;

    public long? ReceitaId { get; set; }
    public long? InsumoId { get; set; }

    [Required(ErrorMessage = "Quantidade Ã© obrigatÃ³ria")]
    [Range(0.0001, double.MaxValue, ErrorMessage = "Quantidade deve ser maior que zero")]
    public decimal Quantidade { get; set; }

    [Required(ErrorMessage = "UnidadeMedidaId Ã© obrigatÃ³rio")]
    public long UnidadeMedidaId { get; set; }

    public bool ExibirComoQB { get; set; }

    [Required(ErrorMessage = "Ordem Ã© obrigatÃ³ria")]
    [Range(1, int.MaxValue, ErrorMessage = "Ordem deve ser maior que zero")]
    public int Ordem { get; set; }

    public string? Observacoes { get; set; }
}



