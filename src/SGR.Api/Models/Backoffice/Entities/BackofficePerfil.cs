namespace SGR.Api.Models.Backoffice.Entities;

/// <summary>
/// Perfil de usuário do backoffice (administradores do sistema)
/// </summary>
public class BackofficePerfil
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public bool IsAtivo { get; set; }
    public string? UsuarioCriacao { get; set; }
    public string? UsuarioAtualizacao { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime? DataAtualizacao { get; set; }

    // Navegação
    public ICollection<BackofficeUsuario> Usuarios { get; set; } = new List<BackofficeUsuario>();
}

