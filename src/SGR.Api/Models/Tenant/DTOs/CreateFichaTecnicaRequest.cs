using System.ComponentModel.DataAnnotations;

namespace SGR.Api.Models.Tenant.DTOs;

public class CreateFichaTecnicaRequest
{
    [Required]
    public long ReceitaId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Nome { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Codigo { get; set; }

    [MaxLength(5000)]
    public string? DescricaoComercial { get; set; }

    public decimal? RendimentoFinal { get; set; }
    public decimal? IndiceContabil { get; set; }
    public decimal? MargemAlvoPercentual { get; set; }

    public bool IsAtivo { get; set; } = true;

    public List<CreateFichaTecnicaCanalRequest> Canais { get; set; } = new();
}

public class CreateFichaTecnicaCanalRequest
{
    [Required]
    [MaxLength(50)]
    public string Canal { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? NomeExibicao { get; set; }

    [Range(0, double.MaxValue)]
    public decimal PrecoVenda { get; set; }

    public decimal? TaxaPercentual { get; set; }
    public decimal? ComissaoPercentual { get; set; }
    public string? Observacoes { get; set; }

    public bool IsAtivo { get; set; } = true;
}

