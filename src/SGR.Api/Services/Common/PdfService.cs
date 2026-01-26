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
                    .Column(headerColumn =>
                    {
                        headerColumn.Item()
                            .Text($"Receita: {receita.Nome}")
                            .SemiBold()
                            .FontSize(18)
                            .AlignCenter()
                            .FontColor(Colors.Blue.Darken2);
                        
                        if (!string.IsNullOrWhiteSpace(receita.Versao))
                        {
                            headerColumn.Item()
                                .Text($"Versão {receita.Versao}")
                                .FontSize(10)
                                .AlignCenter()
                                .FontColor(Colors.Grey.Darken1);
                        }
                    });

                page.Content()
                    .Column(column =>
                    {
                        column.Spacing(10);

                        // Cards de resumo operacional
                        column.Item().PaddingBottom(5).Row(row =>
                        {
                            row.RelativeItem().BorderColor(Colors.Grey.Lighten2).Border(1).Padding(8).Column(card =>
                            {
                                card.Item().Text("Categoria").FontSize(9).FontColor(Colors.Grey.Darken1);
                                card.Item().Text(receita.CategoriaNome ?? "N/A").FontSize(11).SemiBold();
                            });
                            
                            row.RelativeItem().BorderColor(Colors.Grey.Lighten2).Border(1).Padding(8).Column(card =>
                            {
                                card.Item().Text("Rendimento").FontSize(9).FontColor(Colors.Grey.Darken1);
                                card.Item().Text($"{receita.Rendimento:F2} porções").FontSize(11).SemiBold();
                            });
                            
                            row.RelativeItem().BorderColor(Colors.Grey.Lighten2).Border(1).Padding(8).Column(card =>
                            {
                                card.Item().Text("Peso por Porção").FontSize(9).FontColor(Colors.Grey.Darken1);
                                var peso = receita.PesoPorPorcao.HasValue 
                                    ? FormatarQuantidade(receita.PesoPorPorcao.Value, "GR") 
                                    : "-";
                                card.Item().Text(peso).FontSize(11).SemiBold();
                            });
                            
                            row.RelativeItem().BorderColor(Colors.Grey.Lighten2).Border(1).Padding(8).Column(card =>
                            {
                                card.Item().Text("Tempo de Preparo").FontSize(9).FontColor(Colors.Grey.Darken1);
                                var tempo = receita.TempoPreparo.HasValue 
                                    ? $"{receita.TempoPreparo.Value} min" 
                                    : "-";
                                card.Item().Text(tempo).FontSize(11).SemiBold();
                            });
                        });

                        // IC (Índice de Cocção) - informação importante
                        if (!string.IsNullOrWhiteSpace(receita.IcSinal) && receita.IcValor.HasValue && receita.IcValor.Value > 0)
                        {
                            column.Item().PaddingVertical(5).Row(row =>
                            {
                                row.RelativeItem().BorderColor(Colors.Orange.Lighten2).Border(1).Padding(8).Column(card =>
                                {
                                    card.Item().Text("Índice de Cocção (IC)").FontSize(9).FontColor(Colors.Grey.Darken1);
                                    var icTexto = receita.IcSinal == "-" ? "Perda" : "Ganho";
                                    card.Item().Text($"{icTexto}: {receita.IcValor.Value:F2}% (Fator: {receita.FatorRendimento:F4})").FontSize(10).SemiBold();
                                });
                            });
                        }

                        // Custos
                        column.Item().PaddingVertical(5).Row(row =>
                        {
                            row.RelativeItem().Background(Colors.Blue.Lighten4).Padding(8).Column(card =>
                            {
                                card.Item().Text("Custo Total").FontSize(9).FontColor(Colors.Grey.Darken2);
                                card.Item().Text(FormatarMoeda(receita.CustoTotal)).FontSize(14).SemiBold().FontColor(Colors.Blue.Darken2);
                            });
                            
                            row.RelativeItem().Background(Colors.Green.Lighten4).Padding(8).Column(card =>
                            {
                                card.Item().Text("Custo por Porção").FontSize(9).FontColor(Colors.Grey.Darken2);
                                card.Item().Text(FormatarMoeda(receita.CustoPorPorcao)).FontSize(14).SemiBold().FontColor(Colors.Green.Darken2);
                            });
                        });

                        // Ingredientes agrupados por tipo
                        column.Item().PaddingTop(10).Text("Ingredientes").SemiBold().FontSize(14).FontColor(Colors.Blue.Darken2);
                        
                        // Agrupar por tipo de unidade
                        var itensPeso = receita.Itens.Where(i => i.UnidadeMedidaSigla?.ToUpper() == "GR" || i.UnidadeMedidaSigla?.ToUpper() == "KG").OrderBy(i => i.Ordem).ToList();
                        var itensVolume = receita.Itens.Where(i => i.UnidadeMedidaSigla?.ToUpper() == "ML" || i.UnidadeMedidaSigla?.ToUpper() == "L").OrderBy(i => i.Ordem).ToList();
                        var itensUnidade = receita.Itens.Where(i => i.UnidadeMedidaSigla?.ToUpper() == "UN").OrderBy(i => i.Ordem).ToList();

                        if (itensPeso.Any())
                        {
                            column.Item().PaddingTop(8).Column(section =>
                            {
                                section.Item().Text("Peso (GR)").SemiBold().FontSize(11).FontColor(Colors.Grey.Darken2);
                                section.Item().PaddingTop(3).Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(3f);    // Insumo
                                        columns.RelativeColumn(1.2f);  // Quantidade
                                        columns.RelativeColumn(1.2f);  // Custo/unidade
                                        columns.RelativeColumn(1f);    // Custo item
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().Element(CellStyleLight).Text("Insumo").FontSize(9).SemiBold();
                                        header.Cell().Element(CellStyleLight).Text("Quantidade").FontSize(9).SemiBold();
                                        header.Cell().Element(CellStyleLight).Text("Custo/un uso").FontSize(9).SemiBold();
                                        header.Cell().Element(CellStyleLight).Text("Custo").FontSize(9).SemiBold();
                                    });

                                    foreach (var item in itensPeso)
                                    {
                                        var qtdDisplay = item.ExibirComoQB ? "QB" : FormatarQuantidade(item.Quantidade, item.UnidadeMedidaSigla);
                                        var custoPorUnidade = item.CustoPorUnidadeUso.HasValue 
                                            ? FormatarMoeda(item.CustoPorUnidadeUso.Value) + "/g" 
                                            : "-";
                                        
                                        table.Cell().Element(CellStyleLight).Text(item.InsumoNome ?? "").FontSize(9);
                                        table.Cell().Element(CellStyleLight).Text(qtdDisplay).FontSize(9);
                                        table.Cell().Element(CellStyleLight).Text(custoPorUnidade).FontSize(8);
                                        table.Cell().Element(CellStyleLight).Text(FormatarMoeda(item.CustoItem)).FontSize(9);
                                    }
                                });
                            });
                        }

                        if (itensVolume.Any())
                        {
                            column.Item().PaddingTop(8).Column(section =>
                            {
                                section.Item().Text("Volume (ML)").SemiBold().FontSize(11).FontColor(Colors.Grey.Darken2);
                                section.Item().PaddingTop(3).Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(3f);
                                        columns.RelativeColumn(1.2f);
                                        columns.RelativeColumn(1.2f);
                                        columns.RelativeColumn(1f);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().Element(CellStyleLight).Text("Insumo").FontSize(9).SemiBold();
                                        header.Cell().Element(CellStyleLight).Text("Quantidade").FontSize(9).SemiBold();
                                        header.Cell().Element(CellStyleLight).Text("Custo/un uso").FontSize(9).SemiBold();
                                        header.Cell().Element(CellStyleLight).Text("Custo").FontSize(9).SemiBold();
                                    });

                                    foreach (var item in itensVolume)
                                    {
                                        var qtdDisplay = item.ExibirComoQB ? "QB" : FormatarQuantidade(item.Quantidade, item.UnidadeMedidaSigla);
                                        var custoPorUnidade = item.CustoPorUnidadeUso.HasValue 
                                            ? FormatarMoeda(item.CustoPorUnidadeUso.Value) + "/mL" 
                                            : "-";
                                        
                                        table.Cell().Element(CellStyleLight).Text(item.InsumoNome ?? "").FontSize(9);
                                        table.Cell().Element(CellStyleLight).Text(qtdDisplay).FontSize(9);
                                        table.Cell().Element(CellStyleLight).Text(custoPorUnidade).FontSize(8);
                                        table.Cell().Element(CellStyleLight).Text(FormatarMoeda(item.CustoItem)).FontSize(9);
                                    }
                                });
                            });
                        }

                        if (itensUnidade.Any())
                        {
                            column.Item().PaddingTop(8).Column(section =>
                            {
                                section.Item().Text("Unidade (UN)").SemiBold().FontSize(11).FontColor(Colors.Grey.Darken2);
                                section.Item().PaddingTop(3).Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(3f);
                                        columns.RelativeColumn(1.2f);
                                        columns.RelativeColumn(1.2f);
                                        columns.RelativeColumn(1f);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().Element(CellStyleLight).Text("Insumo").FontSize(9).SemiBold();
                                        header.Cell().Element(CellStyleLight).Text("Quantidade").FontSize(9).SemiBold();
                                        header.Cell().Element(CellStyleLight).Text("Custo/un uso").FontSize(9).SemiBold();
                                        header.Cell().Element(CellStyleLight).Text("Custo").FontSize(9).SemiBold();
                                    });

                                    foreach (var item in itensUnidade)
                                    {
                                        var qtdDisplay = item.ExibirComoQB ? "QB" : FormatarQuantidade(item.Quantidade, item.UnidadeMedidaSigla);
                                        var custoPorUnidade = item.CustoPorUnidadeUso.HasValue 
                                            ? FormatarMoeda(item.CustoPorUnidadeUso.Value) + "/un" 
                                            : "-";
                                        
                                        table.Cell().Element(CellStyleLight).Text(item.InsumoNome ?? "").FontSize(9);
                                        table.Cell().Element(CellStyleLight).Text(qtdDisplay).FontSize(9);
                                        table.Cell().Element(CellStyleLight).Text(custoPorUnidade).FontSize(8);
                                        table.Cell().Element(CellStyleLight).Text(FormatarMoeda(item.CustoItem)).FontSize(9);
                                    }
                                });
                            });
                        }

                        // Observações dos itens (se houver)
                        var itensComObs = receita.Itens.Where(i => !string.IsNullOrWhiteSpace(i.Observacoes)).ToList();
                        if (itensComObs.Any())
                        {
                            column.Item().PaddingTop(10).Column(obsSection =>
                            {
                                obsSection.Item().Text("Observações dos Ingredientes").SemiBold().FontSize(11).FontColor(Colors.Grey.Darken2);
                                foreach (var item in itensComObs)
                                {
                                    obsSection.Item().PaddingTop(3).Text(text =>
                                    {
                                        text.Span($"• {item.InsumoNome}: ").SemiBold().FontSize(9);
                                        text.Span(item.Observacoes).FontSize(9);
                                    });
                                }
                            });
                        }

                        // Modo de preparo
                        if (!string.IsNullOrWhiteSpace(receita.Descricao))
                        {
                            column.Item().PaddingTop(15).Column(prepColumn =>
                            {
                                prepColumn.Item().Text("Modo de Preparo").SemiBold().FontSize(14).FontColor(Colors.Blue.Darken2);
                                prepColumn.Item().PaddingTop(5).Text(receita.Descricao).FontSize(10).LineHeight(1.4f);
                            });
                        }

                        // Instruções de Empratamento
                        if (!string.IsNullOrWhiteSpace(receita.InstrucoesEmpratamento))
                        {
                            column.Item().PaddingTop(10).Column(emprataColumn =>
                            {
                                emprataColumn.Item().Text("Empratamento").SemiBold().FontSize(12).FontColor(Colors.Grey.Darken2);
                                emprataColumn.Item().PaddingTop(5).Text(receita.InstrucoesEmpratamento).FontSize(10).LineHeight(1.4f);
                            });
                        }

                        // Conservação
                        if (!string.IsNullOrWhiteSpace(receita.Conservacao))
                        {
                            column.Item().PaddingTop(10).Column(consColumn =>
                            {
                                consColumn.Item().Text("Conservação / Armazenamento").SemiBold().FontSize(12).FontColor(Colors.Grey.Darken2);
                                consColumn.Item().PaddingTop(5).Text(receita.Conservacao).FontSize(10).LineHeight(1.4f);
                            });
                        }
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(x =>
                    {
                        x.Span("Gerado em: ").FontSize(8).FontColor(Colors.Grey.Medium);
                        x.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm")).SemiBold().FontSize(8).FontColor(Colors.Grey.Medium);
                        if (receita.IsAtivo)
                        {
                            x.Span(" • ").FontSize(8).FontColor(Colors.Grey.Medium);
                            x.Span("Ativo").FontSize(8).FontColor(Colors.Green.Medium);
                        }
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
                    .Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text($"Ficha Técnica: {ficha.Nome}")
                                .SemiBold()
                                .FontSize(18)
                                .FontColor(Colors.Blue.Darken2);
                            if (!string.IsNullOrWhiteSpace(ficha.Codigo))
                            {
                                col.Item().Text($"Código: {ficha.Codigo}")
                                    .FontSize(10)
                                    .FontColor(Colors.Grey.Darken1);
                            }
                        });
                        row.ConstantItem(80).AlignRight().Column(col =>
                        {
                            col.Item().Text(ficha.IsAtivo ? "ATIVA" : "INATIVA")
                                .FontSize(10)
                                .SemiBold()
                                .FontColor(ficha.IsAtivo ? Colors.Green.Darken2 : Colors.Red.Darken1);
                        });
                    });

                page.Content()
                    .Column(column =>
                    {
                        column.Spacing(10);

                        // Seção 1: Informações de Produção (Lote)
                        column.Item().PaddingBottom(5).Column(producao =>
                        {
                            producao.Item().Text("📦 Produção (Rendimento do Lote)")
                                .FontSize(14)
                                .SemiBold()
                                .FontColor(Colors.Blue.Darken1);
                            
                            producao.Item().PaddingTop(5).Padding(10).Background(Colors.Blue.Lighten4).Border(1).BorderColor(Colors.Blue.Lighten2).Row(row =>
                            {
                                if (ficha.TempoPreparo.HasValue && ficha.TempoPreparo.Value > 0)
                                {
                                    row.RelativeItem().Column(item =>
                                    {
                                        item.Item().Text("⏱ Tempo").FontSize(9).FontColor(Colors.Grey.Darken2);
                                        item.Item().Text($"{ficha.TempoPreparo.Value} min/lote").FontSize(11).SemiBold();
                                    });
                                }
                                
                                if (!string.IsNullOrWhiteSpace(ficha.RendimentoPorcoes))
                                {
                                    row.RelativeItem(2).Column(item =>
                                    {
                                        item.Item().Text("Lote rende").FontSize(9).FontColor(Colors.Grey.Darken2);
                                        item.Item().Text(ficha.RendimentoPorcoes).FontSize(11).SemiBold();
                                    });
                                }
                                
                                if (ficha.RendimentoPorcoesNumero.HasValue && ficha.RendimentoPorcoesNumero.Value > 0)
                                {
                                    row.RelativeItem().Column(item =>
                                    {
                                        item.Item().Text("Unidades no lote").FontSize(9).FontColor(Colors.Grey.Darken2);
                                        item.Item().Text(ficha.RendimentoPorcoesNumero.Value.ToString("0.##")).FontSize(11).SemiBold();
                                    });
                                }
                            });
                        });

                        // Seção 2: Resumo de Custos e Pesos
                        column.Item().PaddingTop(5).PaddingBottom(5).Column(resumo =>
                        {
                            resumo.Item().Text("💰 Custos e Pesos")
                                .FontSize(14)
                                .SemiBold()
                                .FontColor(Colors.Blue.Darken1);
                            
                            resumo.Item().PaddingTop(5).Row(row =>
                            {
                                row.RelativeItem().Column(item =>
                                {
                                    item.Item().Text("Custo Total do Lote").FontSize(9).FontColor(Colors.Grey.Darken1);
                                    item.Item().Text(FormatarMoeda(ficha.CustoTotal)).FontSize(13).SemiBold().FontColor(Colors.Red.Darken1);
                                });
                                row.RelativeItem().Column(item =>
                                {
                                    item.Item().Text("Peso Total Base").FontSize(9).FontColor(Colors.Grey.Darken1);
                                    var pesoDisplay = ficha.PesoTotalBase.HasValue 
                                        ? $"{FormatarQuantidade(ficha.PesoTotalBase.Value, ObterUnidadePorcao(ficha))}"
                                        : "-";
                                    item.Item().Text(pesoDisplay).FontSize(11).SemiBold();
                                });
                                row.RelativeItem().Column(item =>
                                {
                                    item.Item().Text("Peso Final (IC/IPC)").FontSize(9).FontColor(Colors.Grey.Darken1);
                                    var rendimentoDisplay = ficha.RendimentoFinal.HasValue 
                                        ? $"{FormatarQuantidade(ficha.RendimentoFinal.Value, ObterUnidadePorcao(ficha))}"
                                        : "-";
                                    item.Item().Text(rendimentoDisplay).FontSize(11).SemiBold();
                                });
                                row.RelativeItem().Column(item =>
                                {
                                    item.Item().Text("Custo/kg final").FontSize(9).FontColor(Colors.Grey.Darken1);
                                    var custoKgLDisplay = ficha.CustoKgL.HasValue 
                                        ? $"{FormatarMoeda(ficha.CustoKgL.Value)}/{ObterUnidadePreco(ficha)}"
                                        : "-";
                                    item.Item().Text(custoKgLDisplay).FontSize(10).SemiBold();
                                });
                            });
                        });

                        // Seção 3: Porção de Venda (1 venda = quanto?)
                        column.Item().PaddingTop(5).PaddingBottom(5).Column(porcao =>
                        {
                            porcao.Item().Text("🍽 Porção de Venda (1 venda = quanto?)")
                                .FontSize(14)
                                .SemiBold()
                                .FontColor(Colors.Blue.Darken1);
                            
                            porcao.Item().PaddingTop(5).Padding(10).Background(Colors.Green.Lighten4).Border(1).BorderColor(Colors.Green.Lighten2).Row(row =>
                            {
                                row.RelativeItem().Column(item =>
                                {
                                    item.Item().Text("1 Porção de venda").FontSize(9).FontColor(Colors.Grey.Darken2);
                                    var porcaoDisplay = ficha.PorcaoVendaQuantidade.HasValue 
                                        ? $"{FormatarQuantidade(ficha.PorcaoVendaQuantidade.Value, ObterUnidadePorcao(ficha))}"
                                        : "-";
                                    item.Item().Text(porcaoDisplay).FontSize(12).SemiBold();
                                });
                                row.RelativeItem().Column(item =>
                                {
                                    item.Item().Text("Custo por porção").FontSize(9).FontColor(Colors.Grey.Darken2);
                                    var custoPorcaoDisplay = ficha.CustoPorPorcaoVenda.HasValue 
                                        ? FormatarMoeda(ficha.CustoPorPorcaoVenda.Value)
                                        : "-";
                                    item.Item().Text(custoPorcaoDisplay).FontSize(12).SemiBold();
                                });
                                row.RelativeItem().Column(item =>
                                {
                                    item.Item().Text("Markup mesa").FontSize(9).FontColor(Colors.Grey.Darken2);
                                    var markupDisplay = ficha.IndiceContabil.HasValue 
                                        ? ficha.IndiceContabil.Value.ToString("F2")
                                        : "-";
                                    item.Item().Text(markupDisplay).FontSize(12).SemiBold();
                                });
                                row.RelativeItem().Column(item =>
                                {
                                    item.Item().Text("Preço mesa").FontSize(9).FontColor(Colors.Grey.Darken2);
                                    var precoMesaDisplay = ficha.PrecoMesaSugerido.HasValue 
                                        ? FormatarMoeda(ficha.PrecoMesaSugerido.Value)
                                        : "-";
                                    item.Item().Text(precoMesaDisplay).FontSize(13).SemiBold().FontColor(Colors.Green.Darken2);
                                });
                                
                                // Porções no lote (estimativa)
                                if (ficha.PorcaoVendaQuantidade.HasValue && ficha.PorcaoVendaQuantidade.Value > 0 
                                    && ficha.RendimentoFinal.HasValue && ficha.RendimentoFinal.Value > 0)
                                {
                                    var porcoesNoLote = ficha.RendimentoFinal.Value / ficha.PorcaoVendaQuantidade.Value;
                                    row.RelativeItem().Column(item =>
                                    {
                                        item.Item().Text("Porções no lote").FontSize(9).FontColor(Colors.Grey.Darken2);
                                        item.Item().Text(porcoesNoLote.ToString("0.##")).FontSize(11).SemiBold();
                                    });
                                }
                            });
                        });

                        // Seção 4: Índices de Cocção (IC/IPC) - se existirem
                        if ((ficha.ICOperador != null && ficha.ICValor.HasValue && ficha.ICValor.Value > 0) 
                            || (ficha.IPCValor.HasValue && ficha.IPCValor.Value > 0 && ficha.IPCValor.Value != 100))
                        {
                            column.Item().PaddingTop(5).PaddingBottom(5).Column(indices =>
                            {
                                indices.Item().Text("📊 Índices de Cocção e Partes Comestíveis")
                                    .FontSize(12)
                                    .SemiBold()
                                    .FontColor(Colors.Orange.Darken1);
                                
                                indices.Item().PaddingTop(3).Row(row =>
                                {
                                    if (ficha.ICOperador != null && ficha.ICValor.HasValue && ficha.ICValor.Value > 0)
                                    {
                                        row.RelativeItem().Column(item =>
                                        {
                                            item.Item().Text("IC (Índice de Cocção)").FontSize(8).FontColor(Colors.Grey.Darken1);
                                            var icTexto = ficha.ICOperador == '+' ? "Aumenta" : "Diminui";
                                            item.Item().Text($"{icTexto} {ficha.ICValor.Value}%").FontSize(10).SemiBold();
                                        });
                                    }
                                    
                                    if (ficha.IPCValor.HasValue && ficha.IPCValor.Value > 0 && ficha.IPCValor.Value != 100)
                                    {
                                        row.RelativeItem().Column(item =>
                                        {
                                            item.Item().Text("IPC (Partes Comestíveis)").FontSize(8).FontColor(Colors.Grey.Darken1);
                                            item.Item().Text($"{ficha.IPCValor.Value}% aproveitável").FontSize(10).SemiBold();
                                        });
                                    }
                                });
                            });
                        }

                        // Seção 5: Tabela de Composição (Ingredientes/Componentes)
                        if (ficha.Itens.Any())
                        {
                            column.Item().PaddingTop(10).Column(section =>
                            {
                                section.Item().PaddingBottom(5).Text("🥘 Composição (Ingredientes/Componentes)")
                                    .FontSize(14)
                                    .SemiBold()
                                    .FontColor(Colors.Blue.Darken1);
                                
                                section.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.ConstantColumn(25); // #
                                        columns.RelativeColumn(0.8f); // Tipo
                                        columns.RelativeColumn(2f); // Item
                                        columns.RelativeColumn(1.2f); // Quantidade + Unidade
                                        columns.RelativeColumn(1f); // Custo item (R$)
                                        columns.RelativeColumn(1.5f); // Observações
                                    });

                                    // Cabeçalho
                                    table.Header(header =>
                                    {
                                        header.Cell().Element(CellStyleLight).Text("#").FontSize(9).SemiBold();
                                        header.Cell().Element(CellStyleLight).Text("Tipo").FontSize(9).SemiBold();
                                        header.Cell().Element(CellStyleLight).Text("Item").FontSize(9).SemiBold();
                                        header.Cell().Element(CellStyleLight).Text("Quantidade").FontSize(9).SemiBold();
                                        header.Cell().Element(CellStyleLight).Text("Custo").FontSize(9).SemiBold();
                                        header.Cell().Element(CellStyleLight).Text("Obs").FontSize(9).SemiBold();
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
                                            quantidadeDisplay = $"{item.Quantidade:F0}x";
                                        }
                                        else
                                        {
                                            quantidadeDisplay = FormatarQuantidade(item.Quantidade, item.UnidadeMedidaSigla);
                                        }
                                        
                                        var nomeDisplay = item.TipoItem == "Receita" 
                                            ? (item.ReceitaNome ?? "") 
                                            : (item.InsumoNome ?? "");

                                        table.Cell().Element(CellStyleLight).Text(item.Ordem.ToString()).FontSize(9);
                                        table.Cell().Element(CellStyleLight).Text(item.TipoItem).FontSize(9);
                                        table.Cell().Element(CellStyleLight).Text(nomeDisplay).FontSize(9);
                                        table.Cell().Element(CellStyleLight).Text(quantidadeDisplay).FontSize(9);
                                        table.Cell().Element(CellStyleLight).Text(FormatarMoeda(item.CustoItem)).FontSize(9);
                                        table.Cell().Element(CellStyleLight).Text(item.Observacoes ?? "-").FontSize(8);
                                    }

                                    // Rodapé com total
                                    table.Footer(footer =>
                                    {
                                        footer.Cell().Element(CellStyle).Text("").SemiBold();
                                        footer.Cell().Element(CellStyle).Text("").SemiBold();
                                        footer.Cell().Element(CellStyle).Text("").SemiBold();
                                        footer.Cell().Element(CellStyle).Text("TOTAL").FontSize(10).SemiBold();
                                        footer.Cell().Element(CellStyle).Text(FormatarMoeda(ficha.CustoTotal)).FontSize(10).SemiBold();
                                        footer.Cell().Element(CellStyle).Text("");
                                    });
                                });
                            });
                        }

                        // Seção 6: Descrição Comercial (se existir)
                        if (!string.IsNullOrWhiteSpace(ficha.DescricaoComercial))
                        {
                            column.Item().PaddingTop(10).Column(section =>
                            {
                                section.Item().Text("📝 Descrição Comercial")
                                    .FontSize(12)
                                    .SemiBold()
                                    .FontColor(Colors.Blue.Darken1);
                                
                                section.Item().PaddingTop(3).Padding(8).Background(Colors.Grey.Lighten3).Text(ficha.DescricaoComercial)
                                    .FontSize(9)
                                    .LineHeight(1.4f);
                            });
                        }

                        // Seção 7: Canais de Venda
                        if (ficha.Canais.Any())
                        {
                            column.Item().PaddingTop(10).Column(section =>
                            {
                                section.Item().PaddingBottom(5).Text("🛒 Canais de Venda")
                                    .FontSize(14)
                                    .SemiBold()
                                    .FontColor(Colors.Blue.Darken1);
                                
                                section.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(1.5f); // Canal
                                        columns.RelativeColumn(1.5f); // Nome exibição
                                        columns.RelativeColumn(1.2f); // Modo
                                        columns.RelativeColumn(1f); // Preço Canal
                                        columns.RelativeColumn(0.8f); // Taxa %
                                    });

                                    // Cabeçalho
                                    table.Header(header =>
                                    {
                                        header.Cell().Element(CellStyleLight).Text("Canal").FontSize(9).SemiBold();
                                        header.Cell().Element(CellStyleLight).Text("Nome exibição").FontSize(9).SemiBold();
                                        header.Cell().Element(CellStyleLight).Text("Modo").FontSize(9).SemiBold();
                                        header.Cell().Element(CellStyleLight).Text("Preço").FontSize(9).SemiBold();
                                        header.Cell().Element(CellStyleLight).Text("Taxa %").FontSize(9).SemiBold();
                                    });

                                    // Linhas de dados
                                    foreach (var canal in ficha.Canais)
                                    {
                                        var modo = ObterModoCanal(canal);
                                        var porcentagem = (canal.TaxaPercentual ?? 0m) + (canal.ComissaoPercentual ?? 0m);

                                        table.Cell().Element(CellStyleLight).Text(canal.Canal).FontSize(9);
                                        table.Cell().Element(CellStyleLight).Text(canal.NomeExibicao ?? "-").FontSize(9);
                                        table.Cell().Element(CellStyleLight).Text(modo).FontSize(8);
                                        table.Cell().Element(CellStyleLight).Text(FormatarMoeda(canal.PrecoVenda)).FontSize(9);
                                        table.Cell().Element(CellStyleLight).Text(porcentagem.ToString("F2") + "%").FontSize(9);
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
                        x.Span(" | ").FontSize(8).FontColor(Colors.Grey.Medium);
                        x.Span(ficha.IsAtivo ? "FICHA ATIVA" : "FICHA INATIVA")
                            .SemiBold()
                            .FontSize(8)
                            .FontColor(ficha.IsAtivo ? Colors.Green.Darken1 : Colors.Red.Darken1);
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
    /// Estilo para células de tabela (versão mais leve)
    /// </summary>
    private static IContainer CellStyleLight(IContainer container)
    {
        return container
            .BorderBottom(0.5f)
            .BorderColor(Colors.Grey.Lighten2)
            .PaddingVertical(4)
            .PaddingHorizontal(5);
    }

    /// <summary>
    /// Formata um valor decimal como moeda brasileira
    /// </summary>
    private static string FormatarMoeda(decimal valor)
    {
        return $"R$ {valor:N2}";
    }

    private static string FormatarQuantidade(decimal quantidade, string? sigla)
    {
        if (string.IsNullOrWhiteSpace(sigla))
        {
            return quantidade.ToString("0.##");
        }

        var siglaUpper = sigla.ToUpperInvariant();
        var valor = quantidade;
        var siglaExibicao = sigla;

        if (siglaUpper == "KG")
        {
            valor = quantidade * 1000m;
            siglaExibicao = "g";
        }
        else if (siglaUpper == "L")
        {
            valor = quantidade * 1000m;
            siglaExibicao = "mL";
        }
        else if (siglaUpper == "GR")
        {
            siglaExibicao = "g";
        }
        else if (siglaUpper == "ML")
        {
            siglaExibicao = "mL";
        }

        var formato = siglaUpper == "UN" ? "0" : "0.##";
        return $"{valor.ToString(formato)} {siglaExibicao}".Trim();
    }
}



