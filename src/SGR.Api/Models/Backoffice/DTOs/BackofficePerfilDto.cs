namespace SGR.Api.Models.Backoffice.DTOs;

public class BackofficePerfilDto
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public bool IsAtivo { get; set; }
    public string? UsuarioCriacao { get; set; }
    public string? UsuarioAtualizacao { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime? DataAtualizacao { get; set; }
}

