using System.ComponentModel.DataAnnotations;

namespace SGR.Api.Models.Tenant.DTOs;

public class UpdateCanalVendaRequest
{
    [Required(ErrorMessage = "Nome é obrigatório")]
    [MaxLength(100, ErrorMessage = "Nome deve ter no máximo 100 caracteres")]
    public string Nome { get; set; } = string.Empty;

    [Range(0, 100, ErrorMessage = "Taxa percentual padrão deve estar entre 0 e 100")]
    public decimal? TaxaPercentualPadrao { get; set; }

    public bool IsAtivo { get; set; }
}
