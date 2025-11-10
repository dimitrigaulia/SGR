namespace SGR.Api.Models.Tenant.Entities;

/// <summary>
/// Perfil de usuário do tenant (perfis específicos de cada tenant)
/// </summary>
public class TenantPerfil
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public bool IsAtivo { get; set; }
    public string? UsuarioCriacao { get; set; }
    public string? UsuarioAtualizacao { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime? DataAtualizacao { get; set; }

    // Navegação
    public ICollection<TenantUsuario> Usuarios { get; set; } = new List<TenantUsuario>();
}

