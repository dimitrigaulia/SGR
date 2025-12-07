using System.ComponentModel.DataAnnotations;

namespace SGR.Api.Models.Tenant.DTOs;

public class UpdateUnidadeMedidaRequest
{
    [Required(ErrorMessage = "Nome Ã© obrigatÃ³rio")]
    [MaxLength(50, ErrorMessage = "Nome deve ter no mÃ¡ximo 50 caracteres")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "Sigla Ã© obrigatÃ³ria")]
    [MaxLength(10, ErrorMessage = "Sigla deve ter no mÃ¡ximo 10 caracteres")]
    public string Sigla { get; set; } = string.Empty;

    public bool IsAtivo { get; set; }
}

