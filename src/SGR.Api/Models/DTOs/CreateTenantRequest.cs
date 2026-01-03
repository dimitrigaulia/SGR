using System.ComponentModel.DataAnnotations;

namespace SGR.Api.Models.DTOs;

public class CreateTenantRequest
{
    [Required(ErrorMessage = "A razÃ£o social Ã© obrigatÃ³ria")]
    [MaxLength(200, ErrorMessage = "A razÃ£o social deve ter no mÃ¡ximo 200 caracteres")]
    public string RazaoSocial { get; set; } = string.Empty;

    [Required(ErrorMessage = "O nome fantasia Ã© obrigatÃ³rio")]
    [MaxLength(200, ErrorMessage = "O nome fantasia deve ter no mÃ¡ximo 200 caracteres")]
    public string NomeFantasia { get; set; } = string.Empty;

    [Required(ErrorMessage = "O tipo de pessoa Ã© obrigatÃ³rio")]
    [Range(1, 2, ErrorMessage = "O tipo de pessoa deve ser 1 (Pessoa FÃ­sica) ou 2 (Pessoa JurÃ­dica)")]
    public long TipoPessoaId { get; set; } // 1 = PF, 2 = PJ

    [Required(ErrorMessage = "O CPF/CNPJ Ã© obrigatÃ³rio")]
    [MaxLength(18, ErrorMessage = "O CPF/CNPJ deve ter no mÃ¡ximo 18 caracteres")]
    public string CpfCnpj { get; set; } = string.Empty;

    [Required(ErrorMessage = "O subdomÃ­nio Ã© obrigatÃ³rio")]
    [MaxLength(50, ErrorMessage = "O subdomÃ­nio deve ter no mÃ¡ximo 50 caracteres")]
    [RegularExpression(@"^[a-z0-9]+$", ErrorMessage = "O subdomÃ­nio deve conter apenas letras minÃºsculas e nÃºmeros")]
    public string Subdominio { get; set; } = string.Empty;

    [Required(ErrorMessage = "A categoria Ã© obrigatÃ³ria")]
    public long CategoriaId { get; set; }

    [Required(ErrorMessage = "Os dados do administrador sÃ£o obrigatÃ³rios")]
    public CreateAdminRequest Admin { get; set; } = new();
}

public class CreateAdminRequest
{
    [Required(ErrorMessage = "O nome completo Ã© obrigatÃ³rio")]
    [MaxLength(200, ErrorMessage = "O nome completo deve ter no mÃ¡ximo 200 caracteres")]
    public string NomeCompleto { get; set; } = string.Empty;

    [Required(ErrorMessage = "O email Ã© obrigatÃ³rio")]
    [EmailAddress(ErrorMessage = "O email deve ser vÃ¡lido")]
    [MaxLength(200, ErrorMessage = "O email deve ter no mÃ¡ximo 200 caracteres")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "A senha Ã© obrigatÃ³ria")]
    [MinLength(6, ErrorMessage = "A senha deve ter no mÃ­nimo 6 caracteres")]
    [MaxLength(100, ErrorMessage = "A senha deve ter no mÃ¡ximo 100 caracteres")]
    public string Senha { get; set; } = string.Empty;

    [Required(ErrorMessage = "A confirmaÃ§Ã£o de senha Ã© obrigatÃ³ria")]
    [Compare("Senha", ErrorMessage = "A confirmaÃ§Ã£o de senha deve ser igual Ã  senha")]
    public string ConfirmarSenha { get; set; } = string.Empty;
}

