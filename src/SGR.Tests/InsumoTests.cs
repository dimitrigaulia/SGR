using SGR.Api.Models.Tenant.Entities;

namespace SGR.Tests;

public class InsumoTests
{
    [Fact]
    public void CustoPorUnidadeLimpa_calcula_com_ipc_e_unidades()
    {
        var insumo = new Insumo
        {
            QuantidadePorEmbalagem = 1000m,
            IPCValor = 650m,
            CustoUnitario = 100m,
            UnidadesPorEmbalagem = 13m
        };

        var custo = insumo.CustoPorUnidadeLimpa;

        Assert.Equal(11.83m, Math.Round(custo, 2));
    }
}
