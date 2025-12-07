using System.ComponentModel.DataAnnotations;

namespace SGR.Api.Models.Tenant.DTOs;

public class UpdateReceitaRequest
{
    [Required(ErrorMessage = "Nome Ç¸ obrigatï¿½ï¿½rio")]
    [MaxLength(200, ErrorMessage = "Nome deve ter no mÇ­ximo 200 caracteres")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "Categoria Ç¸ obrigatï¿½ï¿½ria")]
    public long CategoriaId { get; set; }

    [MaxLength(5000, ErrorMessage = "Descriï¿½ï¿½Çœo deve ter no mÇ­ximo 5000 caracteres")]
    public string? Descricao { get; set; }

    [MaxLength(2000, ErrorMessage = "Instruï¿½ï¿½ï¿½ï¿½es de empratamento devem ter no mÇ­ximo 2000 caracteres")]
    public string? InstrucoesEmpratamento { get; set; }

    [Required(ErrorMessage = "Rendimento Ç¸ obrigatï¿½ï¿½rio")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Rendimento deve ser maior que zero")]
    public decimal Rendimento { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Peso por porï¿½ï¿½Çœo deve ser maior que zero")]
    public decimal? PesoPorPorcao { get; set; }


    [Required(ErrorMessage = "Fator de rendimento Ç¸ obrigatï¿½ï¿½rio")]
    [Range(0.01, 10.0, ErrorMessage = "Fator de rendimento deve estar entre 0.01 e 10.0")]
    public decimal FatorRendimento { get; set; } = 1.0m;

    /// <summary>
    /// Sinal do IC: '+' para ganho, '-' para perda.
    /// </summary>
    [RegularExpression(@"^\+|-$", ErrorMessage = "Sinal do IC deve ser '+' ou '-'")]
    public string? IcSinal { get; set; }

    /// <summary>
    /// Valor inteiro do IC (%). Se informado junto com IcSinal, o serviÃ§o converte para FatorRendimento.
    /// Ex: '-' e 5 => FatorRendimento = 0,95
    /// </summary>
    [Range(0, 999, ErrorMessage = "IC deve estar entre 0 e 999%")]
    public int? IcValor { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Tempo de preparo deve ser maior que zero")]
    public int? TempoPreparo { get; set; }

    [MaxLength(20, ErrorMessage = "VersÇœo deve ter no mÇ­ximo 20 caracteres")]
    public string? Versao { get; set; } = "1.0";

    [MaxLength(500, ErrorMessage = "Path da imagem deve ter no mÇ­ximo 500 caracteres")]
    public string? PathImagem { get; set; }

    [Required(ErrorMessage = "A receita deve ter pelo menos um item")]
    [MinLength(1, ErrorMessage = "A receita deve ter pelo menos um item")]
    public List<UpdateReceitaItemRequest> Itens { get; set; } = new List<UpdateReceitaItemRequest>();

    public bool IsAtivo { get; set; }
}

