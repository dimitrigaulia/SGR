using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SGR.Api.Models.Tenant.DTOs;

namespace SGR.Api.Services.Common;

/// <summary>
/// Serviço para geração de PDFs
/// </summary>
public class PdfService
{
    public PdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    /// <summary>
    /// Gera PDF de uma receita
    /// </summary>
    public byte[] GenerateReceitaPdf(ReceitaDto receita)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Header()
                    .Text($"Receita: {receita.Nome}")
                    .SemiBold()
                    .FontSize(18)
                    .AlignCenter()
                    .FontColor(Colors.Blue.Darken2);

                page.Content()
                    .Column(column =>
                    {
                        column.Spacing(10);

                        // Informações gerais
                        column.Item().PaddingBottom(5).Column(infoColumn =>
                        {
                            infoColumn.Item().Text($"Categoria: {receita.CategoriaNome ?? "N/A"}");
                            infoColumn.Item().Text($"Rendimento: {receita.Rendimento} porções");
                            if (receita.PesoPorPorcao.HasValue)
                            {
                                infoColumn.Item().Text($"Peso por porção: {receita.PesoPorPorcao.Value:F2} g");
                            }
                        });

                        // Custos
                        column.Item().PaddingVertical(5).Column(custoColumn =>
                        {
                            custoColumn.Item().Text($"Custo total: {FormatarMoeda(receita.CustoTotal)}").SemiBold();
                            custoColumn.Item().Text($"Custo por porção: {FormatarMoeda(receita.CustoPorPorcao)}").SemiBold();
                        });

                        // Tabela de itens
                        column.Item().PaddingTop(10).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(0.5f); // #
                                columns.RelativeColumn(2.5f); // Insumo
                                columns.RelativeColumn(1.5f); // Quantidade
                                columns.RelativeColumn(1.5f); // Custo
                            });

                            // Cabeçalho
                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("#").SemiBold();
                                header.Cell().Element(CellStyle).Text("Insumo").SemiBold();
                                header.Cell().Element(CellStyle).Text("Quantidade").SemiBold();
                                header.Cell().Element(CellStyle).Text("Custo do item").SemiBold();
                            });

                            // Linhas de dados
                            for (int i = 0; i < receita.Itens.Count; i++)
                            {
                                var item = receita.Itens[i];
                                var quantidadeDisplay = item.ExibirComoQB 
                                    ? "QB" 
                                    : $"{item.Quantidade:F4} {item.UnidadeMedidaSigla ?? ""}";

                                table.Cell().Element(CellStyle).Text((i + 1).ToString());
                                table.Cell().Element(CellStyle).Text(item.InsumoNome ?? "");
                                table.Cell().Element(CellStyle).Text(quantidadeDisplay);
                                table.Cell().Element(CellStyle).Text(FormatarMoeda(item.CustoItem));
                            }
                        });

                        // Modo de preparo
                        if (!string.IsNullOrWhiteSpace(receita.Descricao))
                        {
                            column.Item().PaddingTop(15).Column(prepColumn =>
                            {
                                prepColumn.Item().Text("Modo de preparo").SemiBold().FontSize(12);
                                prepColumn.Item().PaddingTop(5).Text(receita.Descricao);
                            });
                        }
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(x =>
                    {
                        x.Span("Gerado em: ").FontSize(8).FontColor(Colors.Grey.Medium);
                        x.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm")).SemiBold().FontSize(8).FontColor(Colors.Grey.Medium);
                    });
            });
        })
        .GeneratePdf();
    }

    /// <summary>
    /// Gera PDF de uma ficha técnica
    /// </summary>
    public byte[] GenerateFichaTecnicaPdf(FichaTecnicaDto ficha)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Header()
                    .Text($"Ficha Técnica: {ficha.Nome}")
                    .SemiBold()
                    .FontSize(18)
                    .AlignCenter()
                    .FontColor(Colors.Blue.Darken2);

                page.Content()
                    .Column(column =>
                    {
                        column.Spacing(10);

                        // Informações gerais
                        column.Item().PaddingBottom(5).Column(infoColumn =>
                        {
                            if (!string.IsNullOrWhiteSpace(ficha.CategoriaNome))
                            {
                                infoColumn.Item().Text($"Receita: {ficha.CategoriaNome}");
                            }
                            infoColumn.Item().Text($"Custo técnico por porção: {FormatarMoeda(ficha.CustoPorUnidade)}").SemiBold();
                            if (ficha.PrecoSugeridoVenda.HasValue)
                            {
                                infoColumn.Item().Text($"Preço sugerido (por porção): {FormatarMoeda(ficha.PrecoSugeridoVenda.Value)}").SemiBold();
                            }
                        });

                        // Tabela de itens
                        if (ficha.Itens.Any())
                        {
                            column.Item().PaddingTop(10).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(1.2f); // Tipo
                                    columns.RelativeColumn(2.5f); // Nome
                                    columns.RelativeColumn(1.3f); // Quantidade
                                });

                                // Cabeçalho
                                table.Header(header =>
                                {
                                    header.Cell().Element(CellStyle).Text("Tipo").SemiBold();
                                    header.Cell().Element(CellStyle).Text("Nome").SemiBold();
                                    header.Cell().Element(CellStyle).Text("Quantidade").SemiBold();
                                });

                                // Linhas de dados
                                foreach (var item in ficha.Itens)
                                {
                                    var quantidadeDisplay = item.ExibirComoQB 
                                        ? "QB" 
                                        : $"{item.Quantidade:F4} {item.UnidadeMedidaSigla ?? ""}";
                                    var nomeDisplay = item.TipoItem == "Receita" 
                                        ? (item.ReceitaNome ?? "") 
                                        : (item.InsumoNome ?? "");

                                    table.Cell().Element(CellStyle).Text(item.TipoItem);
                                    table.Cell().Element(CellStyle).Text(nomeDisplay);
                                    table.Cell().Element(CellStyle).Text(quantidadeDisplay);
                                }
                            });
                        }

                        // Tabela de canais
                        if (ficha.Canais.Any())
                        {
                            column.Item().PaddingTop(15).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(1.2f); // Canal
                                    columns.RelativeColumn(1.8f); // Nome
                                    columns.RelativeColumn(1.2f); // Preço venda
                                    columns.RelativeColumn(1f); // Taxa (%)
                                    columns.RelativeColumn(1f); // Comissão (%)
                                    columns.RelativeColumn(1f); // Margem (%)
                                });

                                // Cabeçalho
                                table.Header(header =>
                                {
                                    header.Cell().Element(CellStyle).Text("Canal").SemiBold();
                                    header.Cell().Element(CellStyle).Text("Nome").SemiBold();
                                    header.Cell().Element(CellStyle).Text("Preço venda").SemiBold();
                                    header.Cell().Element(CellStyle).Text("Taxa (%)").SemiBold();
                                    header.Cell().Element(CellStyle).Text("Comissão (%)").SemiBold();
                                    header.Cell().Element(CellStyle).Text("Margem (%)").SemiBold();
                                });

                                // Linhas de dados
                                foreach (var canal in ficha.Canais)
                                {
                                    table.Cell().Element(CellStyle).Text(canal.Canal);
                                    table.Cell().Element(CellStyle).Text(canal.NomeExibicao ?? "");
                                    table.Cell().Element(CellStyle).Text(FormatarMoeda(canal.PrecoVenda));
                                    table.Cell().Element(CellStyle).Text(canal.TaxaPercentual?.ToString("F2") ?? "-");
                                    table.Cell().Element(CellStyle).Text(canal.ComissaoPercentual?.ToString("F2") ?? "-");
                                    table.Cell().Element(CellStyle).Text(canal.MargemCalculadaPercentual?.ToString("F2") ?? "-");
                                }
                            });
                        }
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(x =>
                    {
                        x.Span("Gerado em: ").FontSize(8).FontColor(Colors.Grey.Medium);
                        x.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm")).SemiBold().FontSize(8).FontColor(Colors.Grey.Medium);
                    });
            });
        })
        .GeneratePdf();
    }

    /// <summary>
    /// Estilo para células de tabela
    /// </summary>
    private static IContainer CellStyle(IContainer container)
    {
        return container
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten1)
            .PaddingVertical(5)
            .PaddingHorizontal(5);
    }

    /// <summary>
    /// Formata um valor decimal como moeda brasileira
    /// </summary>
    private static string FormatarMoeda(decimal valor)
    {
        return $"R$ {valor:N2}";
    }
}
