using System.ComponentModel.DataAnnotations;

namespace SGR.Api.Models.DTOs;

public class UpdateTenantRequest
{
    [Required(ErrorMessage = "A razÃ£o social Ã© obrigatÃ³ria")]
    [MaxLength(200, ErrorMessage = "A razÃ£o social deve ter no mÃ¡ximo 200 caracteres")]
    public string RazaoSocial { get; set; } = string.Empty;

    [Required(ErrorMessage = "O nome fantasia Ã© obrigatÃ³rio")]
    [MaxLength(200, ErrorMessage = "O nome fantasia deve ter no mÃ¡ximo 200 caracteres")]
    public string NomeFantasia { get; set; } = string.Empty;

    [Required(ErrorMessage = "O tipo de pessoa Ã© obrigatÃ³rio")]
    [Range(1, 2, ErrorMessage = "O tipo de pessoa deve ser 1 (Pessoa FÃ­sica) ou 2 (Pessoa JurÃ­dica)")]
    public long TipoPessoaId { get; set; } // 1 = Pessoa FÃ­sica (PF), 2 = Pessoa JurÃ­dica (PJ)

    [Required(ErrorMessage = "O CPF/CNPJ Ã© obrigatÃ³rio")]
    [MaxLength(18, ErrorMessage = "O CPF/CNPJ deve ter no mÃ¡ximo 18 caracteres")]
    public string CpfCnpj { get; set; } = string.Empty;

    [Required(ErrorMessage = "A categoria Ã© obrigatÃ³ria")]
    public long CategoriaId { get; set; }

    [Required(ErrorMessage = "O fator contÃ¡bil Ã© obrigatÃ³rio")]
    [Range(0.0001, 9999.9999, ErrorMessage = "O fator contÃ¡bil deve estar entre 0,0001 e 9999,9999")]
    public decimal FatorContabil { get; set; } = 1.0m;

    public bool IsAtivo { get; set; }
}

