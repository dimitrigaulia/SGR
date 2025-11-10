using System.ComponentModel.DataAnnotations;

namespace SGR.Api.Models.Backoffice.DTOs;

public class CreateBackofficePerfilRequest
{
    [Required]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    public bool IsAtivo { get; set; } = true;
}

