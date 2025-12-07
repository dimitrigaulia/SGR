namespace SGR.Api.Models.Tenant.Entities;

/// <summary>
/// UsuÃ¡rio do tenant (usuÃ¡rios especÃ­ficos de cada tenant)
/// </summary>
public class TenantUsuario
{
    public long Id { get; set; }
    public long PerfilId { get; set; }
    public bool IsAtivo { get; set; }
    public string NomeCompleto { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty; // Usado tambÃ©m para login
    public string SenhaHash { get; set; } = string.Empty;
    public string? PathImagem { get; set; }
    public string? UsuarioCriacao { get; set; }
    public DateTime DataCriacao { get; set; }
    public string? UsuarioAtualizacao { get; set; }
    public DateTime? DataAtualizacao { get; set; }

    // NavegaÃ§Ã£o
    public TenantPerfil Perfil { get; set; } = null!;
}

