namespace SGR.Api.Models.DTOs;

/// <summary>
/// DTO para resposta da API de consulta de CNPJ (BrasilApi)
/// </summary>
public class CnpjDataResponse
{
    public string? Cnpj { get; set; }
    public string? RazaoSocial { get; set; }
    public string? NomeFantasia { get; set; }
    public string? DescricaoSituacaoCadastral { get; set; }
    public string? DataInicioAtividade { get; set; }
    public string? Logradouro { get; set; }
    public string? Numero { get; set; }
    public string? Bairro { get; set; }
    public string? Municipio { get; set; }
    public string? Uf { get; set; }
    public string? Cep { get; set; }
    public string? NaturezaJuridica { get; set; }
}

