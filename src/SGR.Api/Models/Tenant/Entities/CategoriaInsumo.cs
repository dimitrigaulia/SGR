namespace SGR.Api.Models.Tenant.Entities;

/// <summary>
/// Categoria de insumo do tenant (ex: Hortifruti, Carnes, Limpeza, etc.)
/// </summary>
public class CategoriaInsumo
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public bool IsAtivo { get; set; }
    public string? UsuarioCriacao { get; set; }
    public string? UsuarioAtualizacao { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime? DataAtualizacao { get; set; }

    // NavegaÃ§Ã£o
    public ICollection<Insumo> Insumos { get; set; } = new List<Insumo>();
}

