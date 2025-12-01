using System.ComponentModel.DataAnnotations;

namespace SGR.Api.Models.Tenant.DTOs;

public class CreateInsumoRequest
{
	[Required(ErrorMessage = "Nome é obrigatório")]
	[MaxLength(200, ErrorMessage = "Nome deve ter no máximo 200 caracteres")]
	public string Nome { get; set; } = string.Empty;

	[Required(ErrorMessage = "Categoria é obrigatória")]
	public long CategoriaId { get; set; }

	[Required(ErrorMessage = "Unidade de compra é obrigatória")]
	public long UnidadeCompraId { get; set; }

	[Required(ErrorMessage = "Unidade de uso é obrigatória")]
	public long UnidadeUsoId { get; set; }

	[Required(ErrorMessage = "Quantidade por embalagem é obrigatória")]
	[Range(0.0001, double.MaxValue, ErrorMessage = "Quantidade por embalagem deve ser maior que zero")]
	public decimal QuantidadePorEmbalagem { get; set; }

	[Range(0, double.MaxValue, ErrorMessage = "Custo unitário deve ser maior ou igual a zero")]
	public decimal CustoUnitario { get; set; }

	[Range(1.0, double.MaxValue, ErrorMessage = "Fator de correção deve ser maior ou igual a 1.0")]
	public decimal FatorCorrecao { get; set; } = 1.0m;

	/// <summary>
	/// IPC (%) inteiro. Se informado, o serviço converte em FatorCorrecao.
	/// Ex: 0 = 1,00 (sem perda); 10 = 1,10 (10% de perda)
	/// </summary>
	[Range(0, 999, ErrorMessage = "IPC deve estar entre 0 e 999%")]
	public int? IpcValor { get; set; }

	public string? Descricao { get; set; }

	[MaxLength(500, ErrorMessage = "Path da imagem deve ter no máximo 500 caracteres")]
	public string? PathImagem { get; set; }

	public bool IsAtivo { get; set; } = true;
}
