using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SGR.Api.Data;
using SGR.Api.Models.Tenant.DTOs;
using SGR.Api.Models.Tenant.Entities;
using SGR.Api.Services.Tenant.Implementations;

namespace SGR.Tests;

public class FichaTecnicaServiceTests
{
    private const long CategoriaReceitaId = 1;
    private const long CategoriaInsumoId = 1;
    private const long UnidadeGramaId = 1;
    private const long UnidadeUnId = 2;
    private const long UnidadeMlId = 3;

    [Fact]
    public async Task RendimentoFinal_considera_unidades_com_peso_por_unidade()
    {
        await using var context = CreateContext();
        context.Insumos.AddRange(
            new Insumo
            {
                Id = 1,
                Nome = "Pao",
                CategoriaId = CategoriaInsumoId,
                UnidadeCompraId = UnidadeGramaId,
                QuantidadePorEmbalagem = 1000m,
                CustoUnitario = 10m,
                PesoPorUnidade = 50m,
                IsAtivo = true
            },
            new Insumo
            {
                Id = 2,
                Nome = "Salsicha",
                CategoriaId = CategoriaInsumoId,
                UnidadeCompraId = UnidadeGramaId,
                QuantidadePorEmbalagem = 1000m,
                CustoUnitario = 20m,
                PesoPorUnidade = 45m,
                IsAtivo = true
            },
            new Insumo
            {
                Id = 3,
                Nome = "Mostarda",
                CategoriaId = CategoriaInsumoId,
                UnidadeCompraId = UnidadeGramaId,
                QuantidadePorEmbalagem = 1000m,
                CustoUnitario = 5m,
                IsAtivo = true
            }
        );
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var request = new CreateFichaTecnicaRequest
        {
            CategoriaId = CategoriaReceitaId,
            Nome = "Hot Dog",
            Itens = new List<CreateFichaTecnicaItemRequest>
            {
                new()
                {
                    TipoItem = "Insumo",
                    InsumoId = 1,
                    Quantidade = 1m,
                    UnidadeMedidaId = UnidadeUnId,
                    Ordem = 1
                },
                new()
                {
                    TipoItem = "Insumo",
                    InsumoId = 2,
                    Quantidade = 2m,
                    UnidadeMedidaId = UnidadeUnId,
                    Ordem = 2
                },
                new()
                {
                    TipoItem = "Insumo",
                    InsumoId = 3,
                    Quantidade = 10m,
                    UnidadeMedidaId = UnidadeGramaId,
                    Ordem = 3
                }
            }
        };

        var result = await service.CreateAsync(request, "teste");

        Assert.NotNull(result.RendimentoFinal);
        Assert.Equal(150m, result.RendimentoFinal.Value);
    }

    [Fact]
    public async Task PrecoMesa_calcula_por_porcao_quando_porcao_venda_definida()
    {
        await using var context = CreateContext();
        context.Insumos.Add(new Insumo
        {
            Id = 1,
            Nome = "Carne",
            CategoriaId = CategoriaInsumoId,
            UnidadeCompraId = UnidadeGramaId,
            QuantidadePorEmbalagem = 1000m,
            CustoUnitario = 100m,
            IsAtivo = true
        });
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var request = new CreateFichaTecnicaRequest
        {
            CategoriaId = CategoriaReceitaId,
            Nome = "Teste Porcao",
            PorcaoVendaQuantidade = 50m,
            PrecoSugeridoVenda = 15m,
            PorcaoVendaUnidadeMedidaId = UnidadeGramaId,
            Itens = new List<CreateFichaTecnicaItemRequest>
            {
                new()
                {
                    TipoItem = "Insumo",
                    InsumoId = 1,
                    Quantidade = 100m,
                    UnidadeMedidaId = UnidadeGramaId,
                    Ordem = 1
                }
            }
        };

        var result = await service.CreateAsync(request, "teste");

        Assert.Equal(5m, Math.Round(result.CustoPorPorcaoVenda ?? 0m, 2));
        Assert.Equal(15m, Math.Round(result.PrecoSugeridoVenda ?? 0m, 2));
    }

    [Fact]
    public async Task PrecoMesa_usa_rendimento_porcoes_numero_quando_sem_porcao()
    {
        await using var context = CreateContext();
        context.Insumos.Add(new Insumo
        {
            Id = 1,
            Nome = "Massa",
            CategoriaId = CategoriaInsumoId,
            UnidadeCompraId = UnidadeGramaId,
            QuantidadePorEmbalagem = 1m,
            CustoUnitario = 200m,
            IsAtivo = true
        });
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var request = new CreateFichaTecnicaRequest
        {
            CategoriaId = CategoriaReceitaId,
            Nome = "Teste Rendimento",
            RendimentoPorcoesNumero = 10m,
            PrecoSugeridoVenda = 40m,
            Itens = new List<CreateFichaTecnicaItemRequest>
            {
                new()
                {
                    TipoItem = "Insumo",
                    InsumoId = 1,
                    Quantidade = 1m,
                    UnidadeMedidaId = UnidadeGramaId,
                    Ordem = 1
                }
            }
        };

        var result = await service.CreateAsync(request, "teste");

        Assert.Equal(20m, Math.Round(result.CustoPorPorcaoVenda ?? 0m, 2));
        Assert.Equal(40m, Math.Round(result.PrecoSugeridoVenda ?? 0m, 2));
    }

    private static TenantDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new TenantDbContext(options);
        SeedBaseData(context);
        return context;
    }

    private static void SeedBaseData(TenantDbContext context)
    {
        context.UnidadesMedida.AddRange(
            new UnidadeMedida
            {
                Id = UnidadeGramaId,
                Nome = "Grama",
                Sigla = "GR",
                IsAtivo = true
            },
            new UnidadeMedida
            {
                Id = UnidadeUnId,
                Nome = "Unidade",
                Sigla = "UN",
                IsAtivo = true
            },
            new UnidadeMedida
            {
                Id = UnidadeMlId,
                Nome = "Mililitro",
                Sigla = "ML",
                IsAtivo = true
            }
        );

        context.CategoriaInsumos.Add(new CategoriaInsumo
        {
            Id = CategoriaInsumoId,
            Nome = "Insumos",
            IsAtivo = true
        });

        context.CategoriasReceita.Add(new CategoriaReceita
        {
            Id = CategoriaReceitaId,
            Nome = "Categoria",
            IsAtivo = true
        });

        context.SaveChanges();
    }

    private static FichaTecnicaService CreateService(TenantDbContext context)
    {
        var loggerFactory = new LoggerFactory();
        return new FichaTecnicaService(context, loggerFactory.CreateLogger<FichaTecnicaService>());
    }

    [Fact]
    public async Task GetAllAsync_preenche_custoPorPorcaoVenda_modo_unidade()
    {
        await using var context = CreateContext();
        context.Insumos.Add(new Insumo
        {
            Id = 1,
            Nome = "Produto",
            CategoriaId = CategoriaInsumoId,
            UnidadeCompraId = UnidadeGramaId,
            QuantidadePorEmbalagem = 1m,
            CustoUnitario = 100m,
            IsAtivo = true
        });
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var request = new CreateFichaTecnicaRequest
        {
            CategoriaId = CategoriaReceitaId,
            Nome = "Teste CustoPorPorcaoVenda Unidade",
            RendimentoPorcoesNumero = 1m,
            Itens = new List<CreateFichaTecnicaItemRequest>
            {
                new()
                {
                    TipoItem = "Insumo",
                    InsumoId = 1,
                    Quantidade = 1m,
                    UnidadeMedidaId = UnidadeGramaId,
                    Ordem = 1
                }
            }
        };

        await service.CreateAsync(request, "teste");

        // Teste GetAllAsync
        var result = await service.GetAllAsync(null, 1, 10, null, null);
        
        Assert.NotEmpty(result.Items);
        var ficha = result.Items.First();
        Assert.NotNull(ficha.CustoPorPorcaoVenda);
        Assert.Equal(100m, ficha.CustoPorPorcaoVenda.Value);
    }

    [Fact]
    public async Task GetAllAsync_preenche_custoPorPorcaoVenda_modo_porcao()
    {
        await using var context = CreateContext();
        context.Insumos.Add(new Insumo
        {
            Id = 1,
            Nome = "Produto",
            CategoriaId = CategoriaInsumoId,
            UnidadeCompraId = UnidadeGramaId,
            QuantidadePorEmbalagem = 1000m,
            CustoUnitario = 100m,
            IsAtivo = true
        });
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var request = new CreateFichaTecnicaRequest
        {
            CategoriaId = CategoriaReceitaId,
            Nome = "Teste CustoPorPorcaoVenda Porcao",
            PorcaoVendaQuantidade = 400m,
            PorcaoVendaUnidadeMedidaId = UnidadeGramaId,
            Itens = new List<CreateFichaTecnicaItemRequest>
            {
                new()
                {
                    TipoItem = "Insumo",
                    InsumoId = 1,
                    Quantidade = 1000m,
                    UnidadeMedidaId = UnidadeGramaId,
                    Ordem = 1
                }
            }
        };

        await service.CreateAsync(request, "teste");

        // Teste GetAllAsync
        var result = await service.GetAllAsync(null, 1, 10, null, null);
        
        Assert.NotEmpty(result.Items);
        var ficha = result.Items.First();
        Assert.NotNull(ficha.CustoPorPorcaoVenda);
        // Esperado: CustoUnitario/QuantidadePorEmbalagem * Quantidade = (100/1000)*1000 = 100 (CustoTotal)
        // CustoPorPorcaoVenda = (CustoTotal / RendimentoFinal) * PorcaoVendaQuantidade = (100/1000)*400 = 40
        Assert.Equal(40m, Math.Round(ficha.CustoPorPorcaoVenda.Value, 2));
    }

    [Fact]
    public async Task GetAllAsync_ordena_por_custoPorPorcaoVenda_corretamente()
    {
        await using var context = CreateContext();
        context.Insumos.AddRange(
            new Insumo
            {
                Id = 1,
                Nome = "Produto A",
                CategoriaId = CategoriaInsumoId,
                UnidadeCompraId = UnidadeGramaId,
                QuantidadePorEmbalagem = 1m,
                CustoUnitario = 100m,
                IsAtivo = true
            },
            new Insumo
            {
                Id = 2,
                Nome = "Produto B",
                CategoriaId = CategoriaInsumoId,
                UnidadeCompraId = UnidadeGramaId,
                QuantidadePorEmbalagem = 1m,
                CustoUnitario = 200m,
                IsAtivo = true
            }
        );
        await context.SaveChangesAsync();

        var service = CreateService(context);
        
        // Ficha 1: RendimentoPorcoesNumero=1, CustoTotal=100 => custoPorPorcaoVenda=100
        await service.CreateAsync(new CreateFichaTecnicaRequest
        {
            CategoriaId = CategoriaReceitaId,
            Nome = "Ficha 1",
            RendimentoPorcoesNumero = 1m,
            Itens = new List<CreateFichaTecnicaItemRequest>
            {
                new() { TipoItem = "Insumo", InsumoId = 1, Quantidade = 1m, UnidadeMedidaId = UnidadeGramaId, Ordem = 1 }
            }
        }, "teste");

        // Ficha 2: RendimentoPorcoesNumero=1, CustoTotal=200 => custoPorPorcaoVenda=200
        await service.CreateAsync(new CreateFichaTecnicaRequest
        {
            CategoriaId = CategoriaReceitaId,
            Nome = "Ficha 2",
            RendimentoPorcoesNumero = 1m,
            Itens = new List<CreateFichaTecnicaItemRequest>
            {
                new() { TipoItem = "Insumo", InsumoId = 2, Quantidade = 1m, UnidadeMedidaId = UnidadeGramaId, Ordem = 1 }
            }
        }, "teste");

        // Ordenar por custoporunidade ASC
        var result = await service.GetAllAsync(null, 1, 10, "custoporunidade", "asc");
        
        var items = result.Items.ToList();
        Assert.Equal(2, items.Count);
        Assert.Equal(100m, items[0].CustoPorPorcaoVenda.Value);
        Assert.Equal(200m, items[1].CustoPorPorcaoVenda.Value);
    }
}
