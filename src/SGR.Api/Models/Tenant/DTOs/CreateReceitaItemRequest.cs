using System.ComponentModel.DataAnnotations;

namespace SGR.Api.Models.Tenant.DTOs;

public class CreateReceitaItemRequest
{
    [Required(ErrorMessage = "Insumo é obrigatório")]
    public long InsumoId { get; set; }

    [Required(ErrorMessage = "Quantidade é obrigatória")]
    [Range(0.0001, double.MaxValue, ErrorMessage = "Quantidade deve ser maior que zero")]
    public decimal Quantidade { get; set; }

    [Required(ErrorMessage = "UnidadeMedidaId é obrigatório")]
    public long UnidadeMedidaId { get; set; }

    public bool ExibirComoQB { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Ordem deve ser maior que zero")]
    public int Ordem { get; set; } = 1;

    [MaxLength(500, ErrorMessage = "Observações deve ter no máximo 500 caracteres")]
    public string? Observacoes { get; set; }
}

