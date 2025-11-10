using System.ComponentModel.DataAnnotations;

namespace SGR.Api.Models.Tenant.DTOs;

public class UpdateUnidadeMedidaRequest
{
    [Required(ErrorMessage = "Nome é obrigatório")]
    [MaxLength(50, ErrorMessage = "Nome deve ter no máximo 50 caracteres")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "Sigla é obrigatória")]
    [MaxLength(10, ErrorMessage = "Sigla deve ter no máximo 10 caracteres")]
    public string Sigla { get; set; } = string.Empty;

    [MaxLength(20, ErrorMessage = "Tipo deve ter no máximo 20 caracteres")]
    public string? Tipo { get; set; }

    public long? UnidadeBaseId { get; set; }

    [Range(0.000001, double.MaxValue, ErrorMessage = "Fator de conversão deve ser maior que zero")]
    public decimal? FatorConversaoBase { get; set; }

    public bool IsAtivo { get; set; }
}

