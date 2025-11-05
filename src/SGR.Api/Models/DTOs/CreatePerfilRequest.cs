using System.ComponentModel.DataAnnotations;

namespace SGR.Api.Models.DTOs;

public class CreatePerfilRequest
{
    [Required]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    public bool IsAtivo { get; set; } = true;
}

