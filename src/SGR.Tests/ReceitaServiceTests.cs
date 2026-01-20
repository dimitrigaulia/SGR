using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SGR.Api.Data;
using SGR.Api.Exceptions;
using SGR.Api.Models.Tenant.DTOs;
using SGR.Api.Models.Tenant.Entities;
using SGR.Api.Services.Tenant.Implementations;

namespace SGR.Tests;

public class ReceitaServiceTests
{
    private const long CategoriaReceitaId = 1;
    private const long CategoriaInsumoId = 1;
    private const long UnidadeGramaId = 1;
    private const long UnidadeUnId = 2;
    private const long UnidadeMlId = 3;

    [Fact]
    public async Task Custo_com_unidade_un_usa_peso_por_unidade()
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
            PesoPorUnidade = 50m,
            IsAtivo = true
        });
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var request = new CreateReceitaRequest
        {
            CategoriaId = CategoriaReceitaId,
            Nome = "Teste UN",
            Rendimento = 1m,
            Itens = new List<CreateReceitaItemRequest>
            {
                new()
                {
                    InsumoId = 1,
                    Quantidade = 2m,
                    UnidadeMedidaId = UnidadeUnId,
                    Ordem = 1
                }
            }
        };

        var result = await service.CreateAsync(request, "teste");

        Assert.Equal(10m, Math.Round(result.CustoTotal, 2));
    }

    [Fact]
    public async Task Erro_quando_un_sem_peso_por_unidade()
    {
        await using var context = CreateContext();
        context.Insumos.Add(new Insumo
        {
            Id = 1,
            Nome = "Queijo",
            CategoriaId = CategoriaInsumoId,
            UnidadeCompraId = UnidadeGramaId,
            QuantidadePorEmbalagem = 1000m,
            CustoUnitario = 100m,
            IsAtivo = true
        });
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var request = new CreateReceitaRequest
        {
            CategoriaId = CategoriaReceitaId,
            Nome = "Teste UN sem peso",
            Rendimento = 1m,
            Itens = new List<CreateReceitaItemRequest>
            {
                new()
                {
                    InsumoId = 1,
                    Quantidade = 1m,
                    UnidadeMedidaId = UnidadeUnId,
                    Ordem = 1
                }
            }
        };

        var ex = await Assert.ThrowsAsync<BusinessException>(() => service.CreateAsync(request, "teste"));

        Assert.Contains("esta em UN mas nao possui PesoPorUnidade", ex.Message);
    }

    [Fact]
    public async Task Custo_gr_mantem_regra_atual()
    {
        await using var context = CreateContext();
        context.Insumos.Add(new Insumo
        {
            Id = 1,
            Nome = "Massa",
            CategoriaId = CategoriaInsumoId,
            UnidadeCompraId = UnidadeGramaId,
            QuantidadePorEmbalagem = 1000m,
            CustoUnitario = 100m,
            IsAtivo = true
        });
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var request = new CreateReceitaRequest
        {
            CategoriaId = CategoriaReceitaId,
            Nome = "Teste GR",
            Rendimento = 1m,
            Itens = new List<CreateReceitaItemRequest>
            {
                new()
                {
                    InsumoId = 1,
                    Quantidade = 100m,
                    UnidadeMedidaId = UnidadeGramaId,
                    Ordem = 1
                }
            }
        };

        var result = await service.CreateAsync(request, "teste");

        Assert.Equal(10m, Math.Round(result.CustoTotal, 2));
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

    private static ReceitaService CreateService(TenantDbContext context)
    {
        var loggerFactory = new LoggerFactory();
        return new ReceitaService(context, loggerFactory.CreateLogger<ReceitaService>());
    }
}
