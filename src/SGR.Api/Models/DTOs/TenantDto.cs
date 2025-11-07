namespace SGR.Api.Models.DTOs;

public class TenantDto
{
    public long Id { get; set; }
    public string RazaoSocial { get; set; } = string.Empty;
    public string NomeFantasia { get; set; } = string.Empty;
    public long TipoPessoaId { get; set; }
    public string TipoPessoaNome { get; set; } = string.Empty; // "Pessoa Física" ou "Pessoa Jurídica"
    public string CpfCnpj { get; set; } = string.Empty;
    public string Subdominio { get; set; } = string.Empty;
    public string NomeSchema { get; set; } = string.Empty;
    public long CategoriaId { get; set; }
    public string? CategoriaNome { get; set; }
    public decimal FatorContabil { get; set; }
    public bool IsAtivo { get; set; }
    public string? UsuarioAtualizacao { get; set; }
    public DateTime? DataAtualizacao { get; set; }
}

