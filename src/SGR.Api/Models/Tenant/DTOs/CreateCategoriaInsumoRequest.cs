using System.ComponentModel.DataAnnotations;

namespace SGR.Api.Models.Tenant.DTOs;

public class CreateCategoriaInsumoRequest
{
    [Required(ErrorMessage = "Nome Ã© obrigatÃ³rio")]
    [MaxLength(100, ErrorMessage = "Nome deve ter no mÃ¡ximo 100 caracteres")]
    public string Nome { get; set; } = string.Empty;

    public bool IsAtivo { get; set; } = true;
}

