# Script PowerShell para executar testes de validação da Ficha Técnica
# Este script cria um projeto console temporário e executa os testes

Write-Host "=== TESTE DE VALIDAÇÃO DA FICHA TÉCNICA ===" -ForegroundColor Cyan
Write-Host ""

# Salvar diretório raiz
$rootDir = Get-Location

# Verificar se estamos no diretório raiz do projeto
if (-not (Test-Path "src\SGR.Api\SGR.Api.csproj")) {
    Write-Host "ERRO: Execute este script a partir do diretório raiz do projeto (onde está SGR.slnx)" -ForegroundColor Red
    exit 1
}

# Criar diretório temporário para o projeto de teste
$testDir = "scripts\TesteFichaTecnicaApp"
if (Test-Path $testDir) {
    Remove-Item -Path $testDir -Recurse -Force
}
New-Item -ItemType Directory -Path $testDir -Force | Out-Null

Write-Host "1. Criando projeto console temporário..." -ForegroundColor Yellow

# Criar projeto
Set-Location $testDir
dotnet new console --force | Out-Null

# Encontrar o arquivo .csproj criado
$csprojFile = Get-ChildItem -Filter *.csproj -Recurse | Select-Object -First 1
if ($null -eq $csprojFile) {
    Write-Host "ERRO: Não foi possível encontrar o arquivo .csproj criado" -ForegroundColor Red
    Set-Location $rootDir
    exit 1
}

# Mudar para o diretório do projeto se necessário
$projectDir = $csprojFile.DirectoryName
if ($projectDir -ne (Get-Location).Path) {
    Set-Location $projectDir
}

# Calcular o caminho relativo para o SGR.Api
$relativePath = (Resolve-Path -Relative $rootDir\src\SGR.Api\SGR.Api.csproj)

# Adicionar referências necessárias
Write-Host "2. Adicionando referências..." -ForegroundColor Yellow
dotnet add reference $relativePath | Out-Null

# Criar arquivo Program.cs
$programContent = @'
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SGR.Api.Data;
using SGR.Api.Models.Tenant.DTOs;
using SGR.Api.Models.Tenant.Entities;
using SGR.Api.Services.Tenant.Implementations;

var configuration = new ConfigurationBuilder()
    .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "src", "SGR.Api"))
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile("appsettings.Development.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var configConnectionString = configuration.GetConnectionString("ConfigConnection");
var tenantsConnectionString = configuration.GetConnectionString("TenantsConnection");

if (string.IsNullOrEmpty(configConnectionString) || string.IsNullOrEmpty(tenantsConnectionString))
{
    Console.WriteLine("ERRO: ConnectionStrings não configuradas no appsettings.json");
    return;
}

var configOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseNpgsql(configConnectionString)
    .Options;

var tenantOptions = new DbContextOptionsBuilder<TenantDbContext>()
    .UseNpgsql(tenantsConnectionString)
    .Options;

using var configContext = new ApplicationDbContext(configOptions);
using var tenantContext = new TenantDbContext(tenantOptions);

var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<FichaTecnicaService>();

Console.WriteLine("=== TESTE DE VALIDAÇÃO DA FICHA TÉCNICA ===\n");

// 1. Buscar tenant
Console.WriteLine("1. Buscando tenant de teste...");
var tenant = await configContext.Tenants
    .Where(t => t.IsAtivo)
    .FirstOrDefaultAsync();

if (tenant == null)
{
    Console.WriteLine("ERRO: Nenhum tenant ativo encontrado. Crie um tenant primeiro.");
    return;
}

Console.WriteLine($"   Tenant encontrado: {tenant.NomeFantasia} (Schema: {tenant.NomeSchema})\n");

// 2. Configurar schema
tenantContext.SetSchema(tenant.NomeSchema);
using var connection = tenantContext.Database.GetDbConnection();
await connection.OpenAsync();
using var command = connection.CreateCommand();
command.CommandText = $"SET search_path TO \"{tenant.NomeSchema}\", public;";
await command.ExecuteNonQueryAsync();

// 3. Verificar/criar dados
Console.WriteLine("2. Verificando dados necessários...");

var unidadeGR = await tenantContext.UnidadesMedida
    .FirstOrDefaultAsync(u => u.Sigla.ToUpper() == "GR");

if (unidadeGR == null)
{
    Console.WriteLine("   Criando unidade GR...");
    unidadeGR = new UnidadeMedida
    {
        Nome = "Grama",
        Sigla = "GR",
        IsAtivo = true,
        UsuarioCriacao = "Sistema",
        DataCriacao = DateTime.UtcNow
    };
    tenantContext.UnidadesMedida.Add(unidadeGR);
    await tenantContext.SaveChangesAsync();
    Console.WriteLine("   Unidade GR criada.");
}

var categoriaInsumo = await tenantContext.CategoriaInsumos
    .Where(c => c.IsAtivo)
    .FirstOrDefaultAsync();

if (categoriaInsumo == null)
{
    Console.WriteLine("ERRO: Nenhuma categoria de insumo encontrada.");
    return;
}

var categoriaReceita = await tenantContext.CategoriasReceita
    .Where(c => c.IsAtivo)
    .FirstOrDefaultAsync();

if (categoriaReceita == null)
{
    Console.WriteLine("ERRO: Nenhuma categoria de receita encontrada.");
    return;
}

Console.WriteLine("   Dados básicos verificados.\n");

// 4. Criar insumos
Console.WriteLine("3. Criando insumos da FAROFA DA VOVÓ...");

var insumosData = new[]
{
    new { Nome = "Farinha", Quantidade = 500m, CustoKg = 14.00m },
    new { Nome = "Sal", Quantidade = 2m, CustoKg = 3.89m },
    new { Nome = "Calabresa", Quantidade = 150m, CustoKg = 22.14m },
    new { Nome = "Salsa", Quantidade = 10m, CustoKg = 40.00m },
    new { Nome = "Cenoura", Quantidade = 20m, CustoKg = 7.69m },
    new { Nome = "Pimentão", Quantidade = 20m, CustoKg = 19.69m },
    new { Nome = "Manteiga", Quantidade = 50m, CustoKg = 42.32m },
    new { Nome = "Cebola", Quantidade = 25m, CustoKg = 6.65m }
};

var insumosIds = new Dictionary<string, long>();

foreach (var item in insumosData)
{
    var insumoExistente = await tenantContext.Insumos
        .FirstOrDefaultAsync(i => i.Nome == item.Nome && i.IsAtivo);

    if (insumoExistente == null)
    {
        var insumo = new Insumo
        {
            Nome = item.Nome,
            CategoriaId = categoriaInsumo.Id,
            UnidadeCompraId = unidadeGR.Id,
            UnidadeUsoId = unidadeGR.Id,
            QuantidadePorEmbalagem = 1000m,
            CustoUnitario = item.CustoKg,
            FatorCorrecao = 1.0m,
            IsAtivo = true,
            UsuarioCriacao = "Sistema",
            DataCriacao = DateTime.UtcNow
        };
        tenantContext.Insumos.Add(insumo);
        await tenantContext.SaveChangesAsync();
        insumosIds[item.Nome] = insumo.Id;
        Console.WriteLine($"   Insumo '{item.Nome}' criado (ID: {insumo.Id})");
    }
    else
    {
        insumosIds[item.Nome] = insumoExistente.Id;
        Console.WriteLine($"   Insumo '{item.Nome}' já existe (ID: {insumoExistente.Id})");
    }
}

Console.WriteLine();

// 5. Criar ficha técnica
Console.WriteLine("4. Criando ficha técnica FAROFA DA VOVÓ...");

var fichaService = new FichaTecnicaService(tenantContext, logger);

var createRequest = new CreateFichaTecnicaRequest
{
    Nome = "FAROFA DA VOVÓ",
    CategoriaId = categoriaReceita.Id,
    PorcaoVendaQuantidade = 100m,
    PorcaoVendaUnidadeMedidaId = unidadeGR.Id,
    IndiceContabil = 3m,
    Itens = insumosData.Select((item, index) => new CreateFichaTecnicaItemRequest
    {
        TipoItem = "Insumo",
        InsumoId = insumosIds[item.Nome],
        Quantidade = item.Quantidade,
        UnidadeMedidaId = unidadeGR.Id,
        Ordem = index + 1
    }).ToList()
};

FichaTecnicaDto? fichaDto = null;
try
{
    fichaDto = await fichaService.CreateAsync(createRequest, "Sistema");
    Console.WriteLine($"   Ficha técnica criada com sucesso (ID: {fichaDto.Id})\n");
}
catch (Exception ex)
{
    Console.WriteLine($"   ERRO ao criar ficha técnica: {ex.Message}");
    if (ex.InnerException != null)
        Console.WriteLine($"   InnerException: {ex.InnerException.Message}");
    return;
}

// 6. Validar cálculos
Console.WriteLine("5. VALIDAÇÃO DOS CÁLCULOS:\n");

var tolerancia = 0.0001m;

var custoTotalEsperado = 13.55863m;
var pesoTotalBaseEsperado = 777m;
var rendimentoFinalEsperado = 777m;
var custoPorUnidadeEsperado = 0.01744997m;
var custoPorPorcaoEsperado = 1.744997m;
var precoMesaEsperado = 5.234992m;
var precoPlano12Esperado = 5.957421m;
var precoPlano23Esperado = 6.80549m;

var resultados = new List<(string Nome, bool Ok)>();

Console.WriteLine($"   CustoTotal: {fichaDto.CustoTotal:F5} (esperado: {custoTotalEsperado:F5})");
var custoTotalOk = Math.Abs(fichaDto.CustoTotal - custoTotalEsperado) <= tolerancia;
Console.WriteLine($"   {(custoTotalOk ? "✓" : "✗")} {(custoTotalOk ? "OK" : "FALHOU")}\n");
resultados.Add(("CustoTotal", custoTotalOk));

if (fichaDto.PesoTotalBase.HasValue)
{
    Console.WriteLine($"   PesoTotalBase: {fichaDto.PesoTotalBase:F0} (esperado: {pesoTotalBaseEsperado:F0})");
    var pesoOk = Math.Abs(fichaDto.PesoTotalBase.Value - pesoTotalBaseEsperado) <= 1;
    Console.WriteLine($"   {(pesoOk ? "✓" : "✗")} {(pesoOk ? "OK" : "FALHOU")}\n");
    resultados.Add(("PesoTotalBase", pesoOk));
}
else
{
    Console.WriteLine($"   PesoTotalBase: NULL (esperado: {pesoTotalBaseEsperado:F0})");
    Console.WriteLine($"   ✗ FALHOU (campo não calculado)\n");
    resultados.Add(("PesoTotalBase", false));
}

if (fichaDto.RendimentoFinal.HasValue)
{
    Console.WriteLine($"   RendimentoFinal: {fichaDto.RendimentoFinal:F0} (esperado: {rendimentoFinalEsperado:F0})");
    var rendimentoOk = Math.Abs(fichaDto.RendimentoFinal.Value - rendimentoFinalEsperado) <= 1;
    Console.WriteLine($"   {(rendimentoOk ? "✓" : "✗")} {(rendimentoOk ? "OK" : "FALHOU")}\n");
    resultados.Add(("RendimentoFinal", rendimentoOk));
}
else
{
    Console.WriteLine($"   RendimentoFinal: NULL (esperado: {rendimentoFinalEsperado:F0})");
    Console.WriteLine($"   ✗ FALHOU\n");
    resultados.Add(("RendimentoFinal", false));
}

Console.WriteLine($"   CustoPorUnidade: {fichaDto.CustoPorUnidade:F8} (esperado: {custoPorUnidadeEsperado:F8})");
var custoUnidadeOk = Math.Abs(fichaDto.CustoPorUnidade - custoPorUnidadeEsperado) <= tolerancia;
Console.WriteLine($"   {(custoUnidadeOk ? "✓" : "✗")} {(custoUnidadeOk ? "OK" : "FALHOU")}\n");
resultados.Add(("CustoPorUnidade", custoUnidadeOk));

if (fichaDto.CustoPorPorcaoVenda.HasValue)
{
    Console.WriteLine($"   CustoPorPorcaoVenda: {fichaDto.CustoPorPorcaoVenda:F6} (esperado: {custoPorPorcaoEsperado:F6})");
    var custoPorcaoOk = Math.Abs(fichaDto.CustoPorPorcaoVenda.Value - custoPorPorcaoEsperado) <= tolerancia;
    Console.WriteLine($"   {(custoPorcaoOk ? "✓" : "✗")} {(custoPorcaoOk ? "OK" : "FALHOU")}\n");
    resultados.Add(("CustoPorPorcaoVenda", custoPorcaoOk));
}
else
{
    Console.WriteLine($"   CustoPorPorcaoVenda: NULL (esperado: {custoPorPorcaoEsperado:F6})");
    Console.WriteLine($"   ✗ FALHOU\n");
    resultados.Add(("CustoPorPorcaoVenda", false));
}

if (fichaDto.PrecoMesaSugerido.HasValue)
{
    Console.WriteLine($"   PrecoMesaSugerido: {fichaDto.PrecoMesaSugerido:F6} (esperado: {precoMesaEsperado:F6})");
    var precoMesaOk = Math.Abs(fichaDto.PrecoMesaSugerido.Value - precoMesaEsperado) <= tolerancia;
    Console.WriteLine($"   {(precoMesaOk ? "✓" : "✗")} {(precoMesaOk ? "OK" : "FALHOU")}\n");
    resultados.Add(("PrecoMesaSugerido", precoMesaOk));
}
else
{
    Console.WriteLine($"   PrecoMesaSugerido: NULL (esperado: {precoMesaEsperado:F6})");
    Console.WriteLine($"   ✗ FALHOU\n");
    resultados.Add(("PrecoMesaSugerido", false));
}

var canalPlano12 = fichaDto.Canais?.FirstOrDefault(c => c.NomeExibicao == "Plano 12%");
if (canalPlano12 != null)
{
    Console.WriteLine($"   Canal Plano 12% - PrecoVenda: {canalPlano12.PrecoVenda:F6} (esperado: {precoPlano12Esperado:F6})");
    var plano12Ok = Math.Abs(canalPlano12.PrecoVenda - precoPlano12Esperado) <= tolerancia;
    Console.WriteLine($"   {(plano12Ok ? "✓" : "✗")} {(plano12Ok ? "OK" : "FALHOU")}\n");
    resultados.Add(("Canal Plano 12%", plano12Ok));
}
else
{
    Console.WriteLine($"   Canal Plano 12% não encontrado");
    Console.WriteLine($"   ✗ FALHOU\n");
    resultados.Add(("Canal Plano 12%", false));
}

var canalPlano23 = fichaDto.Canais?.FirstOrDefault(c => c.NomeExibicao == "Plano 23%");
if (canalPlano23 != null)
{
    Console.WriteLine($"   Canal Plano 23% - PrecoVenda: {canalPlano23.PrecoVenda:F5} (esperado: {precoPlano23Esperado:F5})");
    var plano23Ok = Math.Abs(canalPlano23.PrecoVenda - precoPlano23Esperado) <= 0.0001m;
    Console.WriteLine($"   {(plano23Ok ? "✓" : "✗")} {(plano23Ok ? "OK" : "FALHOU")}\n");
    resultados.Add(("Canal Plano 23%", plano23Ok));
}
else
{
    Console.WriteLine($"   Canal Plano 23% não encontrado");
    Console.WriteLine($"   ✗ FALHOU\n");
    resultados.Add(("Canal Plano 23%", false));
}

// Resumo
Console.WriteLine("\n=== RESUMO ===");
var total = resultados.Count;
var aprovados = resultados.Count(r => r.Ok);
var reprovados = total - aprovados;

Console.WriteLine($"Total de validações: {total}");
Console.WriteLine($"Aprovados: {aprovados} ✓");
Console.WriteLine($"Reprovados: {reprovados} ✗");

if (reprovados > 0)
{
    Console.WriteLine("\nValidações que falharam:");
    foreach (var (nome, ok) in resultados.Where(r => !r.Ok))
    {
        Console.WriteLine($"  - {nome}");
    }
}

Console.WriteLine("\n=== TESTE CONCLUÍDO ===");
'@

Set-Content -Path "Program.cs" -Value $programContent

# Compilar e executar
Write-Host "3. Compilando projeto..." -ForegroundColor Yellow
dotnet build | Out-Null

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERRO: Falha na compilação" -ForegroundColor Red
    Set-Location $rootDir
    exit 1
}

Write-Host "4. Executando testes...\n" -ForegroundColor Yellow
dotnet run

Set-Location $rootDir
Write-Host ""
Write-Host "Limpeza: Removendo projeto temporário..." -ForegroundColor Yellow
Remove-Item -Path $testDir -Recurse -Force
Write-Host "Concluído!" -ForegroundColor Green

