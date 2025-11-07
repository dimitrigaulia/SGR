using System.Text.Json.Serialization;

namespace SGR.Api.Models.DTOs;

/// <summary>
/// DTO para resposta da API de consulta de CNPJ (BrasilApi)
/// </summary>
public class CnpjDataResponse
{
    [JsonPropertyName("cnpj")]
    public string? Cnpj { get; set; }

    [JsonPropertyName("razao_social")]
    public string? RazaoSocial { get; set; }

    [JsonPropertyName("nome_fantasia")]
    public string? NomeFantasia { get; set; }

    [JsonPropertyName("descricao_situacao_cadastral")]
    public string? DescricaoSituacaoCadastral { get; set; }

    [JsonPropertyName("data_inicio_atividade")]
    public string? DataInicioAtividade { get; set; }

    [JsonPropertyName("logradouro")]
    public string? Logradouro { get; set; }

    [JsonPropertyName("numero")]
    public string? Numero { get; set; }

    [JsonPropertyName("bairro")]
    public string? Bairro { get; set; }

    [JsonPropertyName("municipio")]
    public string? Municipio { get; set; }

    [JsonPropertyName("uf")]
    public string? Uf { get; set; }

    [JsonPropertyName("cep")]
    public string? Cep { get; set; }

    [JsonPropertyName("natureza_juridica")]
    public string? NaturezaJuridica { get; set; }
}

