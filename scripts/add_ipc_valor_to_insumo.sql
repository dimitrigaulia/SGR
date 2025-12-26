-- Script para adicionar a coluna IPCValor na tabela Insumo de todos os schemas de tenant
-- Execute este script no banco sgr_tenants

DO $$
DECLARE
    schema_record RECORD;
    sql_command TEXT;
BEGIN
    -- Loop através de todos os schemas que seguem o padrão de tenant (ex: vangoghbar_1)
    -- Excluindo schemas padrão do PostgreSQL
    FOR schema_record IN 
        SELECT schema_name 
        FROM information_schema.schemata 
        WHERE schema_name NOT IN ('information_schema', 'pg_catalog', 'pg_toast', 'pg_temp_1', 'pg_toast_temp_1', 'public')
        AND schema_name NOT LIKE 'pg_%'
    LOOP
        -- Verificar se a tabela Insumo existe neste schema
        IF EXISTS (
            SELECT 1 
            FROM information_schema.tables 
            WHERE table_schema = schema_record.schema_name 
            AND table_name = 'Insumo'
        ) THEN
            -- Verificar se a coluna IPCValor já existe
            IF NOT EXISTS (
                SELECT 1 
                FROM information_schema.columns 
                WHERE table_schema = schema_record.schema_name 
                AND table_name = 'Insumo' 
                AND column_name = 'IPCValor'
            ) THEN
                -- Adicionar a coluna IPCValor
                sql_command := format('ALTER TABLE "%s"."Insumo" ADD COLUMN "IPCValor" integer;', schema_record.schema_name);
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



