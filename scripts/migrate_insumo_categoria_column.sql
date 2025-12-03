-- Script de migração para renomear coluna CategoriaInsumoId para CategoriaId
-- Execute este script no banco de dados sgr_tenants para corrigir schemas existentes

-- Para cada schema de tenant, execute:
DO $$
DECLARE
    schema_record RECORD;
BEGIN
    -- Iterar sobre todos os schemas que começam com um padrão de tenant
    -- Ajuste o filtro conforme necessário
    FOR schema_record IN 
        SELECT schema_name 
        FROM information_schema.schemata 
        WHERE schema_name NOT IN ('pg_catalog', 'information_schema', 'pg_toast', 'pg_temp_1', 'pg_toast_temp_1')
        AND schema_name NOT LIKE 'pg_%'
    LOOP
        BEGIN
            -- Tentar renomear a coluna se existir
            EXECUTE format('
                DO $inner$
                BEGIN
                    IF EXISTS (
                        SELECT 1 
                        FROM information_schema.columns 
                        WHERE table_schema = %L 
                        AND table_name = ''Insumo'' 
                        AND column_name = ''CategoriaInsumoId''
                    ) THEN
                        ALTER TABLE %I."Insumo" 
                        RENAME COLUMN "CategoriaInsumoId" TO "CategoriaId";
                        RAISE NOTICE ''Coluna renomeada no schema: %'', %L;
                    ELSE
                        RAISE NOTICE ''Coluna não encontrada no schema: %'', %L;
                    END IF;
                END $inner$;
            ', schema_record.schema_name, schema_record.schema_name, schema_record.schema_name, schema_record.schema_name);
        EXCEPTION
            WHEN OTHERS THEN
                RAISE NOTICE 'Erro ao processar schema %: %', schema_record.schema_name, SQLERRM;
        END;
    END LOOP;
END $$;

-- Para executar em um schema específico, use:
-- ALTER TABLE "nome_do_schema"."Insumo" RENAME COLUMN "CategoriaInsumoId" TO "CategoriaId";

