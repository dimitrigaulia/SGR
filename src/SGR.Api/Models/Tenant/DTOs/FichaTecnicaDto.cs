namespace SGR.Api.Models.Tenant.DTOs;

public class FichaTecnicaDto
{
    public long Id { get; set; }

    public long ReceitaId { get; set; }
    public string ReceitaNome { get; set; } = string.Empty;

    public string Nome { get; set; } = string.Empty;
    public string? Codigo { get; set; }
    public string? DescricaoComercial { get; set; }

    public decimal? RendimentoFinal { get; set; }
    public decimal? IndiceContabil { get; set; }
    public decimal? PrecoSugeridoVenda { get; set; }
    public decimal? MargemAlvoPercentual { get; set; }

    /// <summary>
    /// Custo técnico total da receita associada
    /// </summary>
    public decimal CustoTecnicoTotal { get; set; }

    /// <summary>
    /// Custo técnico por porção da receita associada
    /// </summary>
    public decimal CustoTecnicoPorPorcao { get; set; }

    public bool IsAtivo { get; set; }

    public string? UsuarioCriacao { get; set; }
    public string? UsuarioAtualizacao { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime? DataAtualizacao { get; set; }

    public List<FichaTecnicaCanalDto> Canais { get; set; } = new List<FichaTecnicaCanalDto>();
}
