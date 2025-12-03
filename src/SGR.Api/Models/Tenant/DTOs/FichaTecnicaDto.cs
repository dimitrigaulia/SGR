namespace SGR.Api.Models.Tenant.DTOs;

public class FichaTecnicaDto
{
    public long Id { get; set; }
    public long CategoriaId { get; set; }
    public string? CategoriaNome { get; set; }

    public string Nome { get; set; } = string.Empty;
    public string? Codigo { get; set; }
    public string? DescricaoComercial { get; set; }

    public decimal CustoTotal { get; set; }
    public decimal CustoPorUnidade { get; set; }
    public decimal? RendimentoFinal { get; set; }
    public decimal? IndiceContabil { get; set; }
    public decimal? PrecoSugeridoVenda { get; set; }
    public char? ICOperador { get; set; }
    public int? ICValor { get; set; }
    public int? IPCValor { get; set; }
    public decimal? MargemAlvoPercentual { get; set; }

    public bool IsAtivo { get; set; }

    public string? UsuarioCriacao { get; set; }
    public string? UsuarioAtualizacao { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime? DataAtualizacao { get; set; }

    public List<FichaTecnicaItemDto> Itens { get; set; } = new List<FichaTecnicaItemDto>();
    public List<FichaTecnicaCanalDto> Canais { get; set; } = new List<FichaTecnicaCanalDto>();
}
