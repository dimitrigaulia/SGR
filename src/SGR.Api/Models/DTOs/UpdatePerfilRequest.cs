using System.ComponentModel.DataAnnotations;

namespace SGR.Api.Models.DTOs;

public class UpdatePerfilRequest
{
    [Required]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    [Required]
    public bool IsAtivo { get; set; }
}

