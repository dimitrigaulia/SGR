using System.ComponentModel.DataAnnotations;

namespace SGR.Api.Models.Tenant.DTOs;

public class CreateInsumoRequest
{
	[Required(ErrorMessage = "Nome Ã© obrigatÃ³rio")]
	[MaxLength(200, ErrorMessage = "Nome deve ter no mÃ¡ximo 200 caracteres")]
	public string Nome { get; set; } = string.Empty;

	[Required(ErrorMessage = "Categoria Ã© obrigatÃ³ria")]
	public long CategoriaId { get; set; }

	[Required(ErrorMessage = "Unidade de medida Ã© obrigatÃ³ria")]
	public long UnidadeCompraId { get; set; }

	[Required(ErrorMessage = "Quantidade por embalagem Ã© obrigatÃ³ria")]
	[Range(0.0001, double.MaxValue, ErrorMessage = "Quantidade por embalagem deve ser maior que zero")]
	public decimal QuantidadePorEmbalagem { get; set; }

	[Range(0, double.MaxValue, ErrorMessage = "Custo unitÃ¡rio deve ser maior ou igual a zero")]
	public decimal CustoUnitario { get; set; }

	[Range(1.0, double.MaxValue, ErrorMessage = "Fator de correÃ§Ã£o deve ser maior ou igual a 1.0")]
	public decimal FatorCorrecao { get; set; } = 1.0m;

	/// <summary>
	/// IPC (quantidade aproveitável): quantidade aproveitável na mesma unidade de medida.
	/// Ex: 1000gr comprados, 650gr aproveitáveis = IPC 650
	/// </summary>
	[Range(0.0001, 9999.9999, ErrorMessage = "IPC deve estar entre 0,0001 e 9999,9999")]
	public decimal? IpcValor { get; set; }

	public string? Descricao { get; set; }

	[MaxLength(500, ErrorMessage = "Path da imagem deve ter no mÃ¡ximo 500 caracteres")]
	public string? PathImagem { get; set; }

	public bool IsAtivo { get; set; } = true;
}
