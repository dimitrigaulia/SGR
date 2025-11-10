using System.ComponentModel.DataAnnotations;

namespace SGR.Api.Models.Backoffice.DTOs;

public class UpdateBackofficePerfilRequest
{
    [Required]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    [Required]
    public bool IsAtivo { get; set; }
}

