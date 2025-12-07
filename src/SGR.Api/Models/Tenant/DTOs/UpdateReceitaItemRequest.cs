using System.ComponentModel.DataAnnotations;

namespace SGR.Api.Models.Tenant.DTOs;

public class UpdateReceitaItemRequest
{
    [Required(ErrorMessage = "Insumo Ã© obrigatÃ³rio")]
    public long InsumoId { get; set; }

    [Required(ErrorMessage = "Quantidade Ã© obrigatÃ³ria")]
    [Range(0.0001, double.MaxValue, ErrorMessage = "Quantidade deve ser maior que zero")]
    public decimal Quantidade { get; set; }

    [Required(ErrorMessage = "UnidadeMedidaId Ã© obrigatÃ³rio")]
    public long UnidadeMedidaId { get; set; }

    public bool ExibirComoQB { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Ordem deve ser maior que zero")]
    public int Ordem { get; set; } = 1;

    [MaxLength(500, ErrorMessage = "ObservaÃ§Ãµes deve ter no mÃ¡ximo 500 caracteres")]
    public string? Observacoes { get; set; }
}

