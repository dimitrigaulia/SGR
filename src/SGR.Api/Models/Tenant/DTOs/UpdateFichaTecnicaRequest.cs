using System.ComponentModel.DataAnnotations;

namespace SGR.Api.Models.Tenant.DTOs;

public class UpdateFichaTecnicaRequest
{
    [Required(ErrorMessage = "CategoriaId é obrigatório")]
    public long CategoriaId { get; set; }

    public long? ReceitaPrincipalId { get; set; }

    [Required(ErrorMessage = "Nome é obrigatório")]
    [MaxLength(200, ErrorMessage = "Nome deve ter no máximo 200 caracteres")]
    public string Nome { get; set; } = string.Empty;

    [MaxLength(50, ErrorMessage = "Código deve ter no máximo 50 caracteres")]
    public string? Codigo { get; set; }

    [MaxLength(5000, ErrorMessage = "Descrição comercial deve ter no máximo 5000 caracteres")]
    public string? DescricaoComercial { get; set; }

    public decimal? RendimentoFinal { get; set; }
    public decimal? IndiceContabil { get; set; }
    
    [RegularExpression("^[+-]$", ErrorMessage = "ICOperador deve ser '+' ou '-'")]
    public char? ICOperador { get; set; }
    
    [Range(0, 9999, ErrorMessage = "ICValor deve estar entre 0 e 9999")]
    public int? ICValor { get; set; }
    
    [Range(0, 999, ErrorMessage = "IPCValor deve estar entre 0 e 999")]
    public int? IPCValor { get; set; }
    
    public decimal? MargemAlvoPercentual { get; set; }

    public decimal? PorcaoVendaQuantidade { get; set; }
    public long? PorcaoVendaUnidadeMedidaId { get; set; }
    public decimal? RendimentoPorcoes { get; set; }
    public int? TempoPreparo { get; set; }

    [Required(ErrorMessage = "Itens são obrigatórios")]
    [MinLength(1, ErrorMessage = "A ficha técnica deve ter pelo menos um item")]
    public List<UpdateFichaTecnicaItemRequest> Itens { get; set; } = new();

    public bool IsAtivo { get; set; }

    public List<UpdateFichaTecnicaCanalRequest> Canais { get; set; } = new();
}

public class UpdateFichaTecnicaCanalRequest
{
    public long? Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Canal { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? NomeExibicao { get; set; }

    [Range(0, double.MaxValue)]
    public decimal PrecoVenda { get; set; }

    public decimal? TaxaPercentual { get; set; }
    public decimal? ComissaoPercentual { get; set; }
    public decimal? Multiplicador { get; set; }
    public string? Observacoes { get; set; }

    public bool IsAtivo { get; set; } = true;
}
