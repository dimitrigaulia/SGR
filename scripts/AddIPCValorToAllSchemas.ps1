# Script PowerShell para adicionar coluna IPCValor em todos os schemas de tenant
# Execute este script a partir da raiz do projeto

$connectionString = "Host=localhost;Port=5432;Database=sgr_tenants;Username=postgres;Password=Dimi@1997;"

$sql = @"
DO `$`$
DECLARE
    schema_record RECORD;
    sql_command TEXT;
BEGIN
    FOR schema_record IN 
        SELECT schema_name 
        FROM information_schema.schemata 
        WHERE schema_name NOT IN ('information_schema', 'pg_catalog', 'pg_toast', 'pg_temp_1', 'pg_toast_temp_1', 'public')
        AND schema_name NOT LIKE 'pg_%'
    LOOP
        IF EXISTS (
            SELECT 1 
            FROM information_schema.tables 
            WHERE table_schema = schema_record.schema_name 
            AND table_name = 'Insumo'
        ) THEN
            IF NOT EXISTS (
                SELECT 1 
                FROM information_schema.columns 
                WHERE table_schema = schema_record.schema_name 
                AND table_name = 'Insumo' 
                AND column_name = 'IPCValor'
            ) THEN
                sql_command := format('ALTER TABLE ""%s"".""Insumo"" ADD COLUMN ""IPCValor"" integer;', schema_record.schema_name);
                EXECUTE sql_command;
                
                RAISE NOTICE 'Coluna IPCValor adicionada no schema: %', schema_record.schema_name;
            ELSE
                RAISE NOTICE 'Coluna IPCValor já existe no schema: %', schema_record.schema_name;
            END IF;
        ELSE
            RAISE NOTICE 'Tabela Insumo não encontrada no schema: %', schema_record.schema_name;
        END IF;
    END LOOP;
    
    RAISE NOTICE 'Script concluído!';
END `$`$;
"@

# Executar via dotnet ef
Write-Host "Executando script SQL para adicionar coluna IPCValor em todos os schemas..."
cd src\SGR.Api
dotnet ef dbcontext info --context TenantDbContext | Out-Null

# Usar Npgsql diretamente via .NET
$projectPath = Join-Path $PSScriptRoot "..\src\SGR.Api\SGR.Api.csproj"
if (-not (Test-Path $projectPath)) {
    $projectPath = "src\SGR.Api\SGR.Api.csproj"
}

Write-Host "Criando script temporário C#..."
$tempScript = @"
using Npgsql;
using System;

var connectionString = "$connectionString";
await using var connection = new NpgsqlConnection(connectionString);
await connection.OpenAsync();

var sql = @"$sql";

await using var command = new NpgsqlCommand(sql, connection);
command.CommandTimeout = 300;

try {
    await command.ExecuteNonQueryAsync();
    Console.WriteLine("Script executado com sucesso!");
} catch (Exception ex) {
    Console.WriteLine(`$"Erro: {ex.Message}");
    throw;
}
"@

$tempFile = [System.IO.Path]::GetTempFileName() + ".cs"
$tempFile = $tempFile -replace "\.tmp", ".cs"
Set-Content -Path $tempFile -Value $tempScript

Write-Host "Compilando e executando script..."
try {
    dotnet run --project $projectPath --no-build 2>&1 | Out-Null
    Write-Host "Tentando executar via dotnet ef..."
    
    # Alternativa: criar um endpoint temporário ou usar o método MigrateAllTenantSchemasAsync
    Write-Host "Por favor, execute o SQL manualmente ou use o método MigrateAllTenantSchemasAsync adaptado."
    Write-Host "SQL está salvo em: scripts/add_ipc_valor_to_insumo.sql"
} finally {
    if (Test-Path $tempFile) {
        Remove-Item $tempFile -ErrorAction SilentlyContinue
    }
}



