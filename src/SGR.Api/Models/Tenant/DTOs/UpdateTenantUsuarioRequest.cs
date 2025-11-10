using System.ComponentModel.DataAnnotations;

namespace SGR.Api.Models.Tenant.DTOs;

public class UpdateTenantUsuarioRequest
{
    [Required]
    public long PerfilId { get; set; }

    [Required]
    public bool IsAtivo { get; set; }

    [Required]
    [MaxLength(200)]
    public string NomeCompleto { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    // Opcional: quando informado, altera a senha
    [MinLength(6)]
    public string? NovaSenha { get; set; }

    [MaxLength(500)]
    public string? PathImagem { get; set; }
}

