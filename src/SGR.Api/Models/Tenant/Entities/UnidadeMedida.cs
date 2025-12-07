namespace SGR.Api.Models.Tenant.Entities;

/// <summary>
/// Unidade de medida do tenant (ex: Unidade, Grama, Mililitro)
/// </summary>
public class UnidadeMedida
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty; // Ex: "Unidade", "Grama", "Mililitro"
    public string Sigla { get; set; } = string.Empty; // Ex: "UN", "GR", "ML"
    public bool IsAtivo { get; set; }
    public string? UsuarioCriacao { get; set; }
    public string? UsuarioAtualizacao { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime? DataAtualizacao { get; set; }

    // NavegaÃ§Ã£o
    public ICollection<Insumo> Insumos { get; set; } = new List<Insumo>();
}

