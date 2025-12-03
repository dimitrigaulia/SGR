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

    public bool IsAtivo { get; set; }
}

