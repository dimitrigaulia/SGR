namespace SGR.Api.Models.Entities;

/// <summary>
/// Tipo de pessoa (Pessoa Física ou Pessoa Jurídica)
/// Existe no schema de cada tenant
/// </summary>
public class TipoPessoa
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty; // "Pessoa Física" ou "Pessoa Jurídica"
}

