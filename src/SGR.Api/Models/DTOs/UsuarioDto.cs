namespace SGR.Api.Models.DTOs;

public class UsuarioDto
{
    public long Id { get; set; }
    public long PerfilId { get; set; }
    public string? PerfilNome { get; set; }
    public bool IsAtivo { get; set; }
    public string NomeCompleto { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PathImagem { get; set; }
    public string? UsuarioAtualizacao { get; set; }
    public DateTime? DataAtualizacao { get; set; }
}

