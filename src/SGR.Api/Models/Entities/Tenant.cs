namespace SGR.Api.Models.Entities;

/// <summary>
/// Tenant do sistema (armazenado no banco sgr_config)
/// </summary>
public class Tenant
{
    public long Id { get; set; }
    public string RazaoSocial { get; set; } = string.Empty;
    public string NomeFantasia { get; set; } = string.Empty;
    public long TipoPessoaId { get; set; } // 1 = Pessoa FÃ­sica (PF), 2 = Pessoa JurÃ­dica (PJ)
    public string CpfCnpj { get; set; } = string.Empty; // CNPJ ou CPF (sem mÃ¡scara)
    public string Subdominio { get; set; } = string.Empty; // Ex: "vangoghbar" (apenas letras e nÃºmeros)
    public string NomeSchema { get; set; } = string.Empty; // Gerado: "{subdominio}_{id}" (ex: "vangoghbar_1")
    public long CategoriaId { get; set; } // ReferÃªncia Ã  CategoriaTenant
    public decimal FatorContabil { get; set; } = 1.0m; // Fator contÃ¡bil padrÃ£o
    public bool IsAtivo { get; set; } = true;
    
    // NavegaÃ§Ã£o
    public CategoriaTenant? Categoria { get; set; }
    
    // Campos de auditoria
    public string? UsuarioCriacao { get; set; }
    public DateTime DataCriacao { get; set; }
    public string? UsuarioAtualizacao { get; set; }
    public DateTime? DataAtualizacao { get; set; }
}

