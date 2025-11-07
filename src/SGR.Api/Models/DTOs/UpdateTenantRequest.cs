using System.ComponentModel.DataAnnotations;

namespace SGR.Api.Models.DTOs;

public class UpdateTenantRequest
{
    [Required(ErrorMessage = "A razão social é obrigatória")]
    [MaxLength(200, ErrorMessage = "A razão social deve ter no máximo 200 caracteres")]
    public string RazaoSocial { get; set; } = string.Empty;

    [Required(ErrorMessage = "O nome fantasia é obrigatório")]
    [MaxLength(200, ErrorMessage = "O nome fantasia deve ter no máximo 200 caracteres")]
    public string NomeFantasia { get; set; } = string.Empty;

    [Required(ErrorMessage = "O tipo de pessoa é obrigatório")]
    [Range(1, 2, ErrorMessage = "O tipo de pessoa deve ser 1 (Pessoa Física) ou 2 (Pessoa Jurídica)")]
    public long TipoPessoaId { get; set; }

    [Required(ErrorMessage = "O CPF/CNPJ é obrigatório")]
    [MaxLength(18, ErrorMessage = "O CPF/CNPJ deve ter no máximo 18 caracteres")]
    public string CpfCnpj { get; set; } = string.Empty;

    [Required(ErrorMessage = "A categoria é obrigatória")]
    public long CategoriaId { get; set; }

    [Required(ErrorMessage = "O fator contábil é obrigatório")]
    [Range(0.0001, 9999.9999, ErrorMessage = "O fator contábil deve estar entre 0,0001 e 9999,9999")]
    public decimal FatorContabil { get; set; } = 1.0m;

    public bool IsAtivo { get; set; }
}

