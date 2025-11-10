using System.ComponentModel.DataAnnotations;

namespace SGR.Api.Models.Tenant.DTOs;

public class UpdateInsumoRequest
{
    [Required(ErrorMessage = "Nome é obrigatório")]
    [MaxLength(200, ErrorMessage = "Nome deve ter no máximo 200 caracteres")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "Categoria é obrigatória")]
    public long CategoriaId { get; set; }

    [Required(ErrorMessage = "Unidade de medida é obrigatória")]
    public long UnidadeMedidaId { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Custo unitário deve ser maior ou igual a zero")]
    public decimal CustoUnitario { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Estoque mínimo deve ser maior ou igual a zero")]
    public decimal? EstoqueMinimo { get; set; }

    public string? Descricao { get; set; }

    [MaxLength(50, ErrorMessage = "Código de barras deve ter no máximo 50 caracteres")]
    public string? CodigoBarras { get; set; }

    [MaxLength(500, ErrorMessage = "Path da imagem deve ter no máximo 500 caracteres")]
    public string? PathImagem { get; set; }

    public bool IsAtivo { get; set; }
}

