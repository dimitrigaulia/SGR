using Npgsql;

var connectionString = "Host=localhost;Port=5432;Database=sgr_tenants;Username=postgres;Password=Dimi@1997;";

await using var connection = new NpgsqlConnection(connectionString);
await connection.OpenAsync();

// Script SQL para adicionar a coluna em todos os schemas
var sql = @"
DO $$
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
END $$;
";

await using var command = new NpgsqlCommand(sql, connection);
command.CommandTimeout = 300; // 5 minutos

try
{
    await command.ExecuteNonQueryAsync();
    Console.WriteLine("Script executado com sucesso!");
}
catch (Exception ex)
{
    Console.WriteLine($"Erro ao executar script: {ex.Message}");
    throw;
}



