using System.ComponentModel.DataAnnotations;

namespace SGR.Api.Models.Tenant.DTOs;

public class CreateTenantUsuarioRequest
{
    [Required]
    public long PerfilId { get; set; }

    public bool IsAtivo { get; set; } = true;

    [Required]
    [MaxLength(200)]
    public string NomeCompleto { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Senha { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? PathImagem { get; set; }
}

