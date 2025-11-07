namespace SGR.Api.Models.Entities;

/// <summary>
/// Categoria do Tenant (ex: Alimentos, Bebidas, etc.)
/// Armazenado no banco sgr_config
/// </summary>
public class CategoriaTenant
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public bool IsAtivo { get; set; } = true;
    
    // Campos de auditoria
    public string? UsuarioCriacao { get; set; }
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    public string? UsuarioAtualizacao { get; set; }
    public DateTime? DataAtualizacao { get; set; }
}

