namespace SGR.Api.Models.Tenant.DTOs;

public class TenantPerfilDto
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public bool IsAtivo { get; set; }
    public string? UsuarioCriacao { get; set; }
    public string? UsuarioAtualizacao { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime? DataAtualizacao { get; set; }
}

