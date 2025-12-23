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

                        // Conservação
                        if (!string.IsNullOrWhiteSpace(receita.Conservacao))
                        {
                            column.Item().PaddingTop(10).Column(consColumn =>
                            {
                                consColumn.Item().Text("Conservação").SemiBold().FontSize(12);
                                consColumn.Item().PaddingTop(5).Text(receita.Conservacao);
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

                        // Seção 1: Resumo (informações principais)
                        column.Item().PaddingBottom(10).Column(resumo =>
                        {
                            resumo.Item().Row(row =>
                            {
                                row.RelativeItem().Column(item =>
                                {
                                    item.Item().Text("Custo Total:").FontSize(9).FontColor(Colors.Grey.Darken1);
                                    item.Item().Text(FormatarMoeda(ficha.CustoTotal)).FontSize(12).SemiBold();
                                });
                                row.RelativeItem().Column(item =>
                                {
                                    item.Item().Text("Peso Total Base:").FontSize(9).FontColor(Colors.Grey.Darken1);
                                    var pesoDisplay = ficha.PesoTotalBase.HasValue 
                                        ? $"{ficha.PesoTotalBase.Value:F0} {ObterUnidadePorcao(ficha)}"
                                        : "-";
                                    item.Item().Text(pesoDisplay).FontSize(10).SemiBold();
                                });
                                row.RelativeItem().Column(item =>
                                {
                                    item.Item().Text("Rendimento Final:").FontSize(9).FontColor(Colors.Grey.Darken1);
                                    var rendimentoDisplay = ficha.RendimentoFinal.HasValue 
                                        ? $"{ficha.RendimentoFinal.Value:F0} {ObterUnidadePorcao(ficha)}"
                                        : "-";
                                    item.Item().Text(rendimentoDisplay).FontSize(10).SemiBold();
                                });
                                row.RelativeItem().Column(item =>
                                {
                                    item.Item().Text("Custo por kg/L:").FontSize(9).FontColor(Colors.Grey.Darken1);
                                    var custoKgLDisplay = ficha.CustoKgL.HasValue 
                                        ? $"{FormatarMoeda(ficha.CustoKgL.Value)}/{ObterUnidadePreco(ficha)}"
                                        : "-";
                                    item.Item().Text(custoKgLDisplay).FontSize(10).SemiBold();
                                });
                            });
                            resumo.Item().PaddingTop(5).Row(row =>
                            {
                                row.RelativeItem().Column(item =>
                                {
                                    item.Item().Text("Porção:").FontSize(9).FontColor(Colors.Grey.Darken1);
                                    var porcaoDisplay = ficha.PorcaoVendaQuantidade.HasValue 
                                        ? $"{ficha.PorcaoVendaQuantidade.Value:F2} {ObterUnidadePorcao(ficha)}"
                                        : "-";
                                    item.Item().Text(porcaoDisplay).FontSize(10).SemiBold();
                                });
                                row.RelativeItem().Column(item =>
                                {
                                    item.Item().Text("Custo por porção:").FontSize(9).FontColor(Colors.Grey.Darken1);
                                    var custoPorcaoDisplay = ficha.CustoPorPorcaoVenda.HasValue 
                                        ? FormatarMoeda(ficha.CustoPorPorcaoVenda.Value)
                                        : "-";
                                    item.Item().Text(custoPorcaoDisplay).FontSize(10).SemiBold();
                                });
                                row.RelativeItem().Column(item =>
                                {
                                    item.Item().Text("Markup mesa:").FontSize(9).FontColor(Colors.Grey.Darken1);
                                    var markupDisplay = ficha.IndiceContabil.HasValue 
                                        ? ficha.IndiceContabil.Value.ToString("F2")
                                        : "-";
                                    item.Item().Text(markupDisplay).FontSize(10).SemiBold();
                                });
                                row.RelativeItem().Column(item =>
                                {
                                    item.Item().Text("Preço mesa:").FontSize(9).FontColor(Colors.Grey.Darken1);
                                    var precoMesaDisplay = ficha.PrecoMesaSugerido.HasValue 
                                        ? FormatarMoeda(ficha.PrecoMesaSugerido.Value)
                                        : "-";
                                    item.Item().Text(precoMesaDisplay).FontSize(12).SemiBold();
                                });
                            });
                        });

                        // Seção 2: Tabela de Composição
                        if (ficha.Itens.Any())
                        {
                            column.Item().PaddingTop(10).Column(section =>
                            {
                                section.Item().PaddingBottom(5).Text("Composição").FontSize(14).SemiBold();
                                
                                section.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.ConstantColumn(30); // #
                                        columns.RelativeColumn(1f); // Tipo
                                        columns.RelativeColumn(2f); // Item
                                        columns.RelativeColumn(1.5f); // Quantidade + Unidade
                                        columns.RelativeColumn(1.2f); // Custo item (R$)
                                        columns.RelativeColumn(1.5f); // Observações
                                    });

                                    // Cabeçalho
                                    table.Header(header =>
                                    {
                                        header.Cell().Element(CellStyle).Text("#").SemiBold();
                                        header.Cell().Element(CellStyle).Text("Tipo").SemiBold();
                                        header.Cell().Element(CellStyle).Text("Item").SemiBold();
                                        header.Cell().Element(CellStyle).Text("Quantidade + Unidade").SemiBold();
                                        header.Cell().Element(CellStyle).Text("Custo item (R$)").SemiBold();
                                        header.Cell().Element(CellStyle).Text("Observações").SemiBold();
                                    });

                                    // Linhas de dados
                                    foreach (var item in ficha.Itens)
                                    {
                                        string quantidadeDisplay;
                                        if (item.ExibirComoQB)
                                        {
                                            quantidadeDisplay = "QB";
                                        }
                                        else if (item.TipoItem == "Receita")
                                        {
                                            // Para Receita, exibir como "1x" sem unidade
                                            quantidadeDisplay = $"{item.Quantidade:F0}x";
                                        }
                                        else
                                        {
                                            quantidadeDisplay = $"{item.Quantidade:F4} {item.UnidadeMedidaSigla ?? ""}";
                                        }
                                        
                                        var nomeDisplay = item.TipoItem == "Receita" 
                                            ? (item.ReceitaNome ?? "") 
                                            : (item.InsumoNome ?? "");

                                        table.Cell().Element(CellStyle).Text(item.Ordem.ToString());
                                        table.Cell().Element(CellStyle).Text(item.TipoItem);
                                        table.Cell().Element(CellStyle).Text(nomeDisplay);
                                        table.Cell().Element(CellStyle).Text(quantidadeDisplay);
                                        table.Cell().Element(CellStyle).Text(FormatarMoeda(item.CustoItem));
                                        table.Cell().Element(CellStyle).Text(item.Observacoes ?? "-");
                                    }

                                    // Rodapé com total
                                    table.Footer(footer =>
                                    {
                                        footer.Cell().Element(CellStyle).Text("Total").SemiBold();
                                        footer.Cell().Element(CellStyle);
                                        footer.Cell().Element(CellStyle);
                                        footer.Cell().Element(CellStyle);
                                        footer.Cell().Element(CellStyle).Text(FormatarMoeda(ficha.CustoTotal)).SemiBold();
                                        footer.Cell().Element(CellStyle);
                                    });
                                });

                                // Peso Total Base abaixo da tabela
                                if (ficha.PesoTotalBase.HasValue)
                                {
                                    section.Item().PaddingTop(5).Text($"Peso Total Base: {ficha.PesoTotalBase.Value:F4} {ObterUnidadePorcao(ficha)}")
                                        .FontSize(9).FontColor(Colors.Grey.Darken1);
                                }
                            });
                        }

                        // Seção 3: Precificação Mesa
                        column.Item().PaddingTop(15).Column(section =>
                        {
                            section.Item().PaddingBottom(5).Text("Precificação Mesa").FontSize(14).SemiBold();
                            
                            section.Item().Padding(10).Background(Colors.Grey.Lighten4).Border(1).BorderColor(Colors.Grey.Lighten2).Row(row =>
                            {
                                row.RelativeItem().Column(item =>
                                {
                                    item.Item().Text("Custo por unidade (base)").FontSize(9).FontColor(Colors.Grey.Darken1);
                                    item.Item().Text(FormatarMoeda(ficha.CustoPorUnidade)).FontSize(12).SemiBold();
                                });
                                row.RelativeItem().Column(item =>
                                {
                                    item.Item().Text("Custo por porção").FontSize(9).FontColor(Colors.Grey.Darken1);
                                    var custoPorcaoDisplay = ficha.CustoPorPorcaoVenda.HasValue 
                                        ? FormatarMoeda(ficha.CustoPorPorcaoVenda.Value)
                                        : "-";
                                    item.Item().Text(custoPorcaoDisplay).FontSize(12).SemiBold();
                                });
                                row.RelativeItem().Column(item =>
                                {
                                    item.Item().Text("Markup (IndiceContabil)").FontSize(9).FontColor(Colors.Grey.Darken1);
                                    var markupDisplay = ficha.IndiceContabil.HasValue 
                                        ? ficha.IndiceContabil.Value.ToString("F2")
                                        : "-";
                                    item.Item().Text(markupDisplay).FontSize(12).SemiBold();
                                });
                                row.RelativeItem().Column(item =>
                                {
                                    item.Item().Text("Preço Mesa Sugerido").FontSize(9).FontColor(Colors.Grey.Darken1);
                                    var precoMesaDisplay = ficha.PrecoMesaSugerido.HasValue 
                                        ? FormatarMoeda(ficha.PrecoMesaSugerido.Value)
                                        : "-";
                                    item.Item().Text(precoMesaDisplay).FontSize(14).SemiBold();
                                });
                            });

                            section.Item().PaddingTop(8).Text("Fórmula: Preço mesa = custo por porção × markup")
                                .FontSize(9).FontColor(Colors.Grey.Darken1).Italic();
                        });

                        // Seção 4: Tabela de Canais
                        if (ficha.Canais.Any())
                        {
                            column.Item().PaddingTop(15).Column(section =>
                            {
                                section.Item().PaddingBottom(5).Text("Canais").FontSize(14).SemiBold();
                                
                                section.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(1f); // Canal
                                        columns.RelativeColumn(1.5f); // Nome exibição
                                        columns.RelativeColumn(1f); // Modo
                                        columns.RelativeColumn(1f); // Base (Preço Mesa)
                                        columns.RelativeColumn(1f); // Preço Canal
                                        columns.RelativeColumn(1f); // Porcentagem (%)
                                    });

                                    // Cabeçalho
                                    table.Header(header =>
                                    {
                                        header.Cell().Element(CellStyle).Text("Canal").SemiBold();
                                        header.Cell().Element(CellStyle).Text("Nome exibição").SemiBold();
                                        header.Cell().Element(CellStyle).Text("Modo").SemiBold();
                                        header.Cell().Element(CellStyle).Text("Base (Preço Mesa)").SemiBold();
                                        header.Cell().Element(CellStyle).Text("Preço Canal").SemiBold();
                                        header.Cell().Element(CellStyle).Text("Porcentagem (%)").SemiBold();
                                    });

                                    // Linhas de dados
                                    foreach (var canal in ficha.Canais)
                                    {
                                        var modo = ObterModoCanal(canal);
                                        var basePrecoMesa = ficha.PrecoMesaSugerido.HasValue 
                                            ? FormatarMoeda(ficha.PrecoMesaSugerido.Value)
                                            : "-";
                                        var porcentagem = (canal.TaxaPercentual ?? 0m) + (canal.ComissaoPercentual ?? 0m);

                                        table.Cell().Element(CellStyle).Text(canal.Canal);
                                        table.Cell().Element(CellStyle).Text(canal.NomeExibicao ?? "-");
                                        table.Cell().Element(CellStyle).Text(modo);
                                        table.Cell().Element(CellStyle).Text(basePrecoMesa);
                                        table.Cell().Element(CellStyle).Text(FormatarMoeda(canal.PrecoVenda));
                                        table.Cell().Element(CellStyle).Text(porcentagem.ToString("F2") + "%");
                                    }
                                });
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
    /// Determina o modo do canal (Auto Multiplicador, Auto Gross-up, ou Preço definido)
    /// </summary>
    private static string ObterModoCanal(FichaTecnicaCanalDto canal)
    {
        if (canal.Multiplicador.HasValue && canal.Multiplicador.Value > 0)
        {
            return "Auto (Multiplicador)";
        }
        if ((canal.TaxaPercentual ?? 0) + (canal.ComissaoPercentual ?? 0) > 0)
        {
            return "Auto (Gross-up)";
        }
        if (canal.PrecoVenda > 0)
        {
            return "Preço definido";
        }
        return "-";
    }

    /// <summary>
    /// Obtém a unidade da porção (g ou mL)
    /// </summary>
    private static string ObterUnidadePorcao(FichaTecnicaDto ficha)
    {
        var sigla = ficha.PorcaoVendaUnidadeMedidaSigla?.ToUpper() ?? "";
        if (sigla == "GR") return "g";
        if (sigla == "ML") return "mL";
        return "GR/ML";
    }

    /// <summary>
    /// Obtém a unidade de preço (kg ou L)
    /// </summary>
    private static string ObterUnidadePreco(FichaTecnicaDto ficha)
    {
        var sigla = ficha.PorcaoVendaUnidadeMedidaSigla?.ToUpper() ?? "";
        if (sigla == "GR") return "kg";
        if (sigla == "ML") return "L";
        return "kg ou L";
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




