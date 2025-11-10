namespace SGR.Api.Models.Tenant.Entities;

/// <summary>
/// Categoria de receita do tenant (ex: Entrada, Prato Principal, Sobremesa, etc.)
/// </summary>
public class CategoriaReceita
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public bool IsAtivo { get; set; }
    public string? UsuarioCriacao { get; set; }
    public string? UsuarioAtualizacao { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime? DataAtualizacao { get; set; }

    // Navegação
    public ICollection<Receita> Receitas { get; set; } = new List<Receita>();
}

