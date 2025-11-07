namespace SGR.Api.Models.Entities;

/// <summary>
/// Tenant do sistema (armazenado no banco sgr_config)
/// </summary>
public class Tenant
{
    public long Id { get; set; }
    public string RazaoSocial { get; set; } = string.Empty;
    public string NomeFantasia { get; set; } = string.Empty;
    public long TipoPessoaId { get; set; } // 1 = Pessoa Física (PF), 2 = Pessoa Jurídica (PJ)
    public string CpfCnpj { get; set; } = string.Empty; // CNPJ ou CPF (sem máscara)
    public string Subdominio { get; set; } = string.Empty; // Ex: "vangoghbar" (apenas letras e números)
    public string NomeSchema { get; set; } = string.Empty; // Gerado: "{subdominio}_{id}" (ex: "vangoghbar_1")
    public long CategoriaId { get; set; } // Referência à CategoriaTenant
    public decimal FatorContabil { get; set; } = 1.0m; // Fator contábil padrão
    public bool IsAtivo { get; set; } = true;
    
    // Navegação
    public CategoriaTenant? Categoria { get; set; }
    
    // Campos de auditoria
    public string? UsuarioCriacao { get; set; }
    public DateTime DataCriacao { get; set; }
    public string? UsuarioAtualizacao { get; set; }
    public DateTime? DataAtualizacao { get; set; }
}

