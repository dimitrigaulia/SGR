namespace SGR.Api.Models.DTOs;

public class CategoriaTenantDto
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public bool IsAtivo { get; set; }
}

