using System.ComponentModel.DataAnnotations;

namespace SGR.Api.Models.DTOs;

public class LoginRequest
{
    [Required(ErrorMessage = "Email Ã© obrigatÃ³rio")]
    [EmailAddress(ErrorMessage = "Email invÃ¡lido")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Senha Ã© obrigatÃ³ria")]
    public string Senha { get; set; } = string.Empty;
}


