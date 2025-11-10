using System.ComponentModel.DataAnnotations;

namespace SGR.Api.Models.Tenant.DTOs;

public class UpdateReceitaRequest
{
    [Required(ErrorMessage = "Nome é obrigatório")]
    [MaxLength(200, ErrorMessage = "Nome deve ter no máximo 200 caracteres")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "Categoria é obrigatória")]
    public long CategoriaId { get; set; }

    [MaxLength(5000, ErrorMessage = "Descrição deve ter no máximo 5000 caracteres")]
    public string? Descricao { get; set; }

    [MaxLength(2000, ErrorMessage = "Instruções de empratamento devem ter no máximo 2000 caracteres")]
    public string? InstrucoesEmpratamento { get; set; }

    [Required(ErrorMessage = "Rendimento é obrigatório")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Rendimento deve ser maior que zero")]
    public decimal Rendimento { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Peso por porção deve ser maior que zero")]
    public decimal? PesoPorPorcao { get; set; }

    [Range(0, 100, ErrorMessage = "Tolerância de peso deve estar entre 0 e 100%")]
    public decimal? ToleranciaPeso { get; set; }

    [Required(ErrorMessage = "Fator de rendimento é obrigatório")]
    [Range(0.01, 10.0, ErrorMessage = "Fator de rendimento deve estar entre 0.01 e 10.0")]
    public decimal FatorRendimento { get; set; } = 1.0m;

    [Range(1, int.MaxValue, ErrorMessage = "Tempo de preparo deve ser maior que zero")]
    public int? TempoPreparo { get; set; }

    [MaxLength(20, ErrorMessage = "Versão deve ter no máximo 20 caracteres")]
    public string? Versao { get; set; } = "1.0";

    [MaxLength(500, ErrorMessage = "Path da imagem deve ter no máximo 500 caracteres")]
    public string? PathImagem { get; set; }

    [Required(ErrorMessage = "A receita deve ter pelo menos um item")]
    [MinLength(1, ErrorMessage = "A receita deve ter pelo menos um item")]
    public List<UpdateReceitaItemRequest> Itens { get; set; } = new List<UpdateReceitaItemRequest>();

    public bool IsAtivo { get; set; }
}

