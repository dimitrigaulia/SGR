using System.ComponentModel.DataAnnotations;

namespace SGR.Api.Models.DTOs;

public class CreateTenantRequest
{
    [Required(ErrorMessage = "A razão social é obrigatória")]
    [MaxLength(200, ErrorMessage = "A razão social deve ter no máximo 200 caracteres")]
    public string RazaoSocial { get; set; } = string.Empty;

    [Required(ErrorMessage = "O nome fantasia é obrigatório")]
    [MaxLength(200, ErrorMessage = "O nome fantasia deve ter no máximo 200 caracteres")]
    public string NomeFantasia { get; set; } = string.Empty;

    [Required(ErrorMessage = "O tipo de pessoa é obrigatório")]
    [Range(1, 2, ErrorMessage = "O tipo de pessoa deve ser 1 (Pessoa Física) ou 2 (Pessoa Jurídica)")]
    public long TipoPessoaId { get; set; } // 1 = PF, 2 = PJ

    [Required(ErrorMessage = "O CPF/CNPJ é obrigatório")]
    [MaxLength(18, ErrorMessage = "O CPF/CNPJ deve ter no máximo 18 caracteres")]
    public string CpfCnpj { get; set; } = string.Empty;

    [Required(ErrorMessage = "O subdomínio é obrigatório")]
    [MaxLength(50, ErrorMessage = "O subdomínio deve ter no máximo 50 caracteres")]
    [RegularExpression(@"^[a-z0-9]+$", ErrorMessage = "O subdomínio deve conter apenas letras minúsculas e números")]
    public string Subdominio { get; set; } = string.Empty;

    [Required(ErrorMessage = "A categoria é obrigatória")]
    public long CategoriaId { get; set; }

    [Required(ErrorMessage = "O fator contábil é obrigatório")]
    [Range(0.0001, 9999.9999, ErrorMessage = "O fator contábil deve estar entre 0,0001 e 9999,9999")]
    public decimal FatorContabil { get; set; } = 1.0m;

    [Required(ErrorMessage = "Os dados do administrador são obrigatórios")]
    public CreateAdminRequest Admin { get; set; } = new();
}

public class CreateAdminRequest
{
    [Required(ErrorMessage = "O nome completo é obrigatório")]
    [MaxLength(200, ErrorMessage = "O nome completo deve ter no máximo 200 caracteres")]
    public string NomeCompleto { get; set; } = string.Empty;

    [Required(ErrorMessage = "O email é obrigatório")]
    [EmailAddress(ErrorMessage = "O email deve ser válido")]
    [MaxLength(200, ErrorMessage = "O email deve ter no máximo 200 caracteres")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "A senha é obrigatória")]
    [MinLength(6, ErrorMessage = "A senha deve ter no mínimo 6 caracteres")]
    [MaxLength(100, ErrorMessage = "A senha deve ter no máximo 100 caracteres")]
    public string Senha { get; set; } = string.Empty;

    [Required(ErrorMessage = "A confirmação de senha é obrigatória")]
    [Compare("Senha", ErrorMessage = "A confirmação de senha deve ser igual à senha")]
    public string ConfirmarSenha { get; set; } = string.Empty;
}

