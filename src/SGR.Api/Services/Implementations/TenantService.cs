using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SGR.Api.Data;
using SGR.Api.Exceptions;
using SGR.Api.Models.DTOs;
using SGR.Api.Models.Entities;
using SGR.Api.Services.Common;
using SGR.Api.Services.Interfaces;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Npgsql;
using TenantEntity = SGR.Api.Models.Entities.Tenant;

namespace SGR.Api.Services.Implementations;

public class TenantService : BaseService<ApplicationDbContext, TenantEntity, TenantDto, CreateTenantRequest, UpdateTenantRequest>, ITenantService
{
    private readonly TenantDbContext _tenantContext;
    private readonly ICpfCnpjValidationService _cpfCnpjValidationService;
    private readonly IConfiguration _configuration;

    public TenantService(
        ApplicationDbContext context,
        TenantDbContext tenantContext,
        ICpfCnpjValidationService cpfCnpjValidationService,
        IConfiguration configuration,
        ILogger<TenantService> logger) 
        : base(context, logger)
    {
        _tenantContext = tenantContext;
        _cpfCnpjValidationService = cpfCnpjValidationService;
        _configuration = configuration;
    }

    protected override Expression<Func<TenantEntity, TenantDto>> MapToDto()
    {
        return t => new TenantDto
        {
            Id = t.Id,
            RazaoSocial = t.RazaoSocial,
            NomeFantasia = t.NomeFantasia,
            TipoPessoaId = t.TipoPessoaId,
            TipoPessoaNome = t.TipoPessoaId == 1 ? "Pessoa Física" : "Pessoa Jurídica", // 1 = PF, 2 = PJ
            CpfCnpj = t.CpfCnpj,
            Subdominio = t.Subdominio,
            NomeSchema = t.NomeSchema,
            CategoriaId = t.CategoriaId,
            CategoriaNome = t.Categoria != null ? t.Categoria.Nome : null,
            IsAtivo = t.IsAtivo,
            UsuarioAtualizacao = t.UsuarioAtualizacao,
            DataAtualizacao = t.DataAtualizacao
        };
    }

    protected override TenantEntity MapToEntity(CreateTenantRequest request)
    {
        // Este método não deve ser usado diretamente - use CreateTenantAsync
        // Implementado apenas para satisfazer a classe abstrata
        return new TenantEntity
        {
            RazaoSocial = request.RazaoSocial,
            NomeFantasia = request.NomeFantasia,
            TipoPessoaId = request.TipoPessoaId,
            CpfCnpj = Regex.Replace(request.CpfCnpj, @"[^\d]", ""),
            Subdominio = request.Subdominio.ToLower(),
            CategoriaId = request.CategoriaId,
            IsAtivo = true
        };
    }

    public override async Task<TenantDto> CreateAsync(CreateTenantRequest request, string? usuarioCriacao)
    {
        // Redireciona para CreateTenantAsync que tem toda a lógica
        return await CreateTenantAsync(request, usuarioCriacao);
    }

    protected override void UpdateEntity(TenantEntity entity, UpdateTenantRequest request)
    {
        entity.RazaoSocial = request.RazaoSocial;
        entity.NomeFantasia = request.NomeFantasia;
        entity.TipoPessoaId = request.TipoPessoaId;
        entity.CpfCnpj = Regex.Replace(request.CpfCnpj, @"[^\d]", "");
        entity.CategoriaId = request.CategoriaId;
        entity.IsAtivo = request.IsAtivo;
    }

    protected override IQueryable<TenantEntity> ApplySearch(IQueryable<TenantEntity> query, string? search)
    {
        if (string.IsNullOrWhiteSpace(search)) return query;
        search = search.ToLower();
        return query.Where(t =>
            EF.Functions.ILike(t.RazaoSocial, $"%{search}%") ||
            EF.Functions.ILike(t.NomeFantasia, $"%{search}%") ||
            EF.Functions.ILike(t.Subdominio, $"%{search}%") ||
            EF.Functions.ILike(t.CpfCnpj, $"%{search}%") ||
            (t.Categoria != null && EF.Functions.ILike(t.Categoria.Nome, $"%{search}%")));
    }

    public override async Task<PagedResult<TenantDto>> GetAllAsync(string? search, int page, int pageSize, string? sort, string? order)
    {
        _logger.LogInformation("Buscando {EntityType} - PÃ¡gina: {Page}, Tamanho: {PageSize}, Busca: {Search}", 
            typeof(TenantEntity).Name, page, pageSize, search ?? "N/A");

        var query = _dbSet.Include(t => t.Categoria).AsQueryable();
        
        // Aplicar busca
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = ApplySearch(query, search);
        }

        // Aplicar ordenação
        query = ApplySorting(query, sort, order);

        var total = await query.CountAsync();
        var items = await query
            .Skip(Math.Max(0, (page - 1) * pageSize))
            .Take(pageSize)
            .Select(MapToDto())
            .ToListAsync();

        _logger.LogInformation("Encontrados {Total} registros de {EntityType}", total, typeof(TenantEntity).Name);

        return new PagedResult<TenantDto> { Items = items, Total = total };
    }

    public override async Task<TenantDto?> GetByIdAsync(long id)
    {
        _logger.LogInformation("Buscando {EntityType} por ID: {Id}", typeof(TenantEntity).Name, id);

        var entity = await _dbSet.Include(t => t.Categoria).FirstOrDefaultAsync(t => t.Id == id);
        if (entity == null)
        {
            _logger.LogWarning("{EntityType} com ID {Id} não encontrado", typeof(TenantEntity).Name, id);
            return null;
        }

        var mapper = MapToDto().Compile();
        return mapper(entity);
    }

    protected override IQueryable<TenantEntity> ApplySorting(IQueryable<TenantEntity> query, string? sort, string? order)
    {
        var ascending = string.Equals(order, "asc", StringComparison.OrdinalIgnoreCase);
        return (sort?.ToLower()) switch
        {
            "razaosocial" => ascending ? query.OrderBy(t => t.RazaoSocial) : query.OrderByDescending(t => t.RazaoSocial),
            "nomefantasia" => ascending ? query.OrderBy(t => t.NomeFantasia) : query.OrderByDescending(t => t.NomeFantasia),
            "subdominio" => ascending ? query.OrderBy(t => t.Subdominio) : query.OrderByDescending(t => t.Subdominio),
            "ativo" or "isativo" => ascending ? query.OrderBy(t => t.IsAtivo) : query.OrderByDescending(t => t.IsAtivo),
            _ => query.OrderBy(t => t.RazaoSocial)
        };
    }

    public async Task<TenantDto> CreateTenantAsync(CreateTenantRequest request, string? usuarioCriacao)
    {
        _logger.LogInformation("Iniciando criação de tenant: {Subdominio}", request.Subdominio);

        // 1. Validações
        await ValidateTenantRequestAsync(request);

        // 2. Criar banco sgr_tenants se não existir
        await EnsureTenantsDatabaseExistsAsync();

        // 3. Criar registro do Tenant no banco sgr_config
        var tenant = new TenantEntity
        {
            RazaoSocial = request.RazaoSocial,
            NomeFantasia = request.NomeFantasia,
            TipoPessoaId = request.TipoPessoaId,
            CpfCnpj = Regex.Replace(request.CpfCnpj, @"[^\d]", ""), // Remove mÃ¡scara
            Subdominio = request.Subdominio.ToLower(),
            CategoriaId = request.CategoriaId,
            IsAtivo = true
        };

        SetAuditFieldsOnCreate(tenant, request, usuarioCriacao);
        _dbSet.Add(tenant);
        await _context.SaveChangesAsync();

        // 4. Gerar NomeSchema e atualizar
        tenant.NomeSchema = $"{tenant.Subdominio}_{tenant.Id}";
        await _context.SaveChangesAsync();

        _logger.LogInformation("Tenant criado com ID {Id}, Schema: {Schema}", tenant.Id, tenant.NomeSchema);

        // 5. Criar schema no banco sgr_tenants
        await CreateTenantSchemaAsync(tenant.NomeSchema);

        // 6. Executar migrations no schema
        await RunMigrationsOnSchemaAsync(tenant.NomeSchema);

        // 7. Inicializar dados do tenant (Perfil Administrador)
        await InitializeTenantDataAsync(tenant.NomeSchema, usuarioCriacao);

        // 8. Criar usuário administrador
        await CreateAdminUserAsync(tenant.NomeSchema, request.Admin, usuarioCriacao);

        _logger.LogInformation("Tenant {Subdominio} criado com sucesso", request.Subdominio);

        var mapper = MapToDto().Compile();
        return mapper(tenant);
    }

    public async Task<TenantDto?> GetBySubdomainAsync(string subdomain)
    {
        var tenant = await _dbSet
            .Include(t => t.Categoria)
            .FirstOrDefaultAsync(t => t.Subdominio == subdomain.ToLower() && t.IsAtivo);
        if (tenant == null) return null;

        var mapper = MapToDto().Compile();
        return mapper(tenant);
    }

    public async Task<List<TenantDto>> GetActiveTenantsAsync()
    {
        var tenants = await _dbSet
            .Include(t => t.Categoria)
            .Where(t => t.IsAtivo)
            .OrderBy(t => t.NomeFantasia)
            .ToListAsync();

        var mapper = MapToDto().Compile();
        return tenants.Select(mapper).ToList();
    }

    /// <summary>
    /// Inativa um tenant (soft delete)
    /// </summary>
    public override async Task<bool> DeleteAsync(long id)
    {
        _logger.LogInformation("Inativando tenant - ID: {Id}", id);

        try
        {
            var tenant = await _dbSet.FindAsync(id);
            if (tenant == null)
            {
                _logger.LogWarning("Tenant com ID {Id} não encontrado para inativação", id);
                return false;
            }

            // Verificar se já está inativo
            if (!tenant.IsAtivo)
            {
                _logger.LogWarning("Tenant com ID {Id} já está inativo", id);
                return false;
            }

            // Inativar o tenant (soft delete)
            tenant.IsAtivo = false;
            var updateRequest = new UpdateTenantRequest
            {
                RazaoSocial = tenant.RazaoSocial,
                NomeFantasia = tenant.NomeFantasia,
                TipoPessoaId = tenant.TipoPessoaId,
                CpfCnpj = tenant.CpfCnpj,
                CategoriaId = tenant.CategoriaId,
                IsAtivo = false
            };
            SetAuditFieldsOnUpdate(tenant, updateRequest, null);
            
            await _context.SaveChangesAsync();

            _logger.LogInformation("Tenant inativado com sucesso - ID: {Id}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao inativar tenant - ID: {Id}", id);
            throw;
        }
    }

    /// <summary>
    /// Alterna o status ativo/inativo do tenant
    /// </summary>
    public async Task<bool> ToggleActiveAsync(long id, string? usuarioAtualizacao = null)
    {
        _logger.LogInformation("Alternando status do tenant - ID: {Id}", id);

        try
        {
            var tenant = await _dbSet.FindAsync(id);
            if (tenant == null)
            {
                _logger.LogWarning("Tenant com ID {Id} não encontrado", id);
                return false;
            }

            tenant.IsAtivo = !tenant.IsAtivo;
            var updateRequest = new UpdateTenantRequest
            {
                RazaoSocial = tenant.RazaoSocial,
                NomeFantasia = tenant.NomeFantasia,
                TipoPessoaId = tenant.TipoPessoaId,
                CpfCnpj = tenant.CpfCnpj,
                CategoriaId = tenant.CategoriaId,
                IsAtivo = tenant.IsAtivo
            };
            SetAuditFieldsOnUpdate(tenant, updateRequest, usuarioAtualizacao);
            
            await _context.SaveChangesAsync();

            _logger.LogInformation("Status do tenant alterado com sucesso - ID: {Id}, Novo status: {Status}", 
                id, tenant.IsAtivo ? "Ativo" : "Inativo");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao alternar status do tenant - ID: {Id}", id);
            throw;
        }
    }

    private async Task ValidateTenantRequestAsync(CreateTenantRequest request)
    {
        // Validar subdomínio único
        var subdomainExists = await _dbSet.AnyAsync(t => t.Subdominio == request.Subdominio.ToLower());
        if (subdomainExists)
        {
            throw new BusinessException($"O subdomínio '{request.Subdominio}' já está em uso");
        }

        // Validar formato do subdomínio (apenas letras minúsculas e números)
        if (!Regex.IsMatch(request.Subdominio, @"^[a-z0-9]+$"))
        {
            throw new BusinessException("O subdomínio deve conter apenas letras minúsculas e números");
        }

        // Validar CPF/CNPJ único
        var cpfCnpjClean = Regex.Replace(request.CpfCnpj, @"[^\d]", "");
        var cpfCnpjExists = await _dbSet.AnyAsync(t => t.CpfCnpj == cpfCnpjClean);
        if (cpfCnpjExists)
        {
            throw new BusinessException("CPF/CNPJ já cadastrado");
        }

        // Validar CPF/CNPJ via BrasilApi
        var isValid = await _cpfCnpjValidationService.ValidarAsync(request.CpfCnpj);
        if (!isValid)
        {
            throw new BusinessException("CPF/CNPJ inválido");
        }

        // Validar tipo de pessoa (1 = Pessoa Física, 2 = Pessoa Jurídica)
        if (request.TipoPessoaId != 1 && request.TipoPessoaId != 2)
        {
            throw new BusinessException("Tipo de pessoa deve ser 1 (Pessoa Física) ou 2 (Pessoa Jurídica)");
        }

        // Validar categoria existe e está ativa
        var categoriaExists = await _context.Set<CategoriaTenant>()
            .AnyAsync(c => c.Id == request.CategoriaId && c.IsAtivo);
        if (!categoriaExists)
        {
            throw new BusinessException("Categoria invÃ¡lida ou inativa");
        }
    }

    private async Task EnsureTenantsDatabaseExistsAsync()
    {
        var connectionString = _configuration.GetConnectionString("TenantsConnection");
        if (string.IsNullOrEmpty(connectionString))
            throw new InvalidOperationException("ConnectionString 'TenantsConnection' não configurada");

        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        var databaseName = builder.Database;

        // Conectar ao banco postgres para criar o banco se não existir
        builder.Database = "postgres";
        var masterConnectionString = builder.ConnectionString;

        try
        {
            using var connection = new NpgsqlConnection(masterConnectionString);
            await connection.OpenAsync();

            // Verificar se o banco existe
            var checkDbCommand = new NpgsqlCommand(
                $"SELECT 1 FROM pg_database WHERE datname = '{databaseName}'",
                connection);
            var dbExists = await checkDbCommand.ExecuteScalarAsync() != null;

            if (!dbExists)
            {
                _logger.LogInformation("Criando banco de dados {Database}", databaseName);
                var createDbCommand = new NpgsqlCommand(
                    $"CREATE DATABASE \"{databaseName}\"",
                    connection);
                await createDbCommand.ExecuteNonQueryAsync();
                _logger.LogInformation("Banco de dados {Database} criado com sucesso", databaseName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao verificar/criar banco de dados {Database}", databaseName);
            throw;
        }
    }

    private async Task CreateTenantSchemaAsync(string schemaName)
    {
        try
        {
            _logger.LogInformation("Criando schema {Schema}", schemaName);
            await _tenantContext.Database.ExecuteSqlRawAsync($"CREATE SCHEMA IF NOT EXISTS \"{schemaName}\"");
            _logger.LogInformation("Schema {Schema} criado com sucesso", schemaName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar schema {Schema}", schemaName);
            throw;
        }
    }

    private async Task RunMigrationsOnSchemaAsync(string schemaName)
    {
        try
        {
            _logger.LogInformation("Executando migrations no schema {Schema}", schemaName);
            
            // Configurar o schema no contexto
            _tenantContext.SetSchema(schemaName);
            
            // Executar migrações de estrutura antiga (se necessário)
            await MigrateOldSchemaAsync(schemaName);
            
            // Criar as tabelas diretamente via SQL (já que não temos migrations específicas ainda)
            await CreateTenantTablesAsync(schemaName);
            
            _logger.LogInformation("Migrations executadas com sucesso no schema {Schema}", schemaName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao executar migrations no schema {Schema}", schemaName);
            throw;
        }
    }

    /// <summary>
    /// Migra todos os schemas de tenant existentes para corrigir estruturas antigas
    /// </summary>
    public async Task MigrateAllTenantSchemasAsync()
    {
        try
        {
            _logger.LogInformation("Iniciando migração de todos os schemas de tenant...");
            
            // Buscar todos os tenants ativos
            var tenants = await _dbSet
                .Where(t => t.IsAtivo && !string.IsNullOrEmpty(t.NomeSchema))
                .ToListAsync();
            
            _logger.LogInformation("Encontrados {Count} tenant(s) para migração", tenants.Count);
            
            foreach (var tenant in tenants)
            {
                try
                {
                    _logger.LogInformation("Migrando schema {Schema} do tenant {Subdominio}", tenant.NomeSchema, tenant.Subdominio);
                    await MigrateOldSchemaAsync(tenant.NomeSchema);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao migrar schema {Schema} do tenant {Subdominio}", tenant.NomeSchema, tenant.Subdominio);
                    // Continua com os prÃ³ximos tenants mesmo se um falhar
                }
            }
            
            _logger.LogInformation("Migração de todos os schemas concluída");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao executar migração de todos os schemas de tenant");
            throw;
        }
    }

    private async Task MigrateOldSchemaAsync(string schemaName)
    {
        try
        {
            _logger.LogInformation("Verificando migrações de estrutura antiga no schema {Schema}", schemaName);
            
            // Usar uma abordagem direta: tentar renomear se existir
            var renameColumnSql = $@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 
                        FROM information_schema.columns 
                        WHERE table_schema = '{schemaName}' 
                        AND table_name = 'Insumo' 
                        AND column_name = 'CategoriaInsumoId'
                    ) THEN
                        ALTER TABLE ""{schemaName}"".""Insumo"" 
                        RENAME COLUMN ""CategoriaInsumoId"" TO ""CategoriaId"";
                        RAISE NOTICE 'Coluna CategoriaInsumoId renomeada para CategoriaId no schema %', '{schemaName}';
                    ELSE
                        RAISE NOTICE 'Coluna CategoriaInsumoId não encontrada no schema % (schema pode ser novo ou já migrado)', '{schemaName}';
                    END IF;

                    -- Indice (idempotente) somente se a coluna existir
                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = '{schemaName}'
                        AND table_name = 'FichaTecnica'
                        AND column_name = 'ReceitaPrincipalId'
                    ) THEN
                        EXECUTE 'CREATE INDEX IF NOT EXISTS ""IX_FichaTecnica_ReceitaPrincipalId_{schemaName}"" ON ""{schemaName}"".""FichaTecnica""(""ReceitaPrincipalId"")';
                    END IF;
                END $$;
            ";
            
            await _tenantContext.Database.ExecuteSqlRawAsync(renameColumnSql);

            // Adicionar/ajustar colunas novas (idempotente) para schemas existentes
            var addColumnsSql = $@"
                DO $$
                BEGIN
                    -- Receita.Conservacao
                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.tables
                        WHERE table_schema = '{schemaName}'
                        AND table_name = 'Receita'
                    ) AND NOT EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = '{schemaName}'
                        AND table_name = 'Receita'
                        AND column_name = 'Conservacao'
                    ) THEN
                        ALTER TABLE ""{schemaName}"".""Receita""
                        ADD COLUMN ""Conservacao"" TEXT;
                    END IF;

                    -- FichaTecnica.ReceitaPrincipalId
                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.tables
                        WHERE table_schema = '{schemaName}'
                        AND table_name = 'FichaTecnica'
                    ) AND NOT EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = '{schemaName}'
                        AND table_name = 'FichaTecnica'
                        AND column_name = 'ReceitaPrincipalId'
                    ) THEN
                        ALTER TABLE ""{schemaName}"".""FichaTecnica""
                        ADD COLUMN ""ReceitaPrincipalId"" BIGINT;
                    END IF;

                    -- FK FichaTecnica -> Receita (somente se tabelas existirem, coluna existir e constraint não existir)
                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = '{schemaName}'
                        AND table_name = 'FichaTecnica'
                        AND column_name = 'ReceitaPrincipalId'
                    ) AND EXISTS (
                        SELECT 1
                        FROM information_schema.tables
                        WHERE table_schema = '{schemaName}'
                        AND table_name = 'Receita'
                    ) AND NOT EXISTS (
                        SELECT 1
                        FROM pg_constraint c
                        JOIN pg_class t ON t.oid = c.conrelid
                        JOIN pg_namespace n ON n.oid = t.relnamespace
                        WHERE n.nspname = '{schemaName}'
                        AND t.relname = 'FichaTecnica'
                        AND c.conname = 'FK_FichaTecnica_Receita_ReceitaPrincipalId'
                    ) THEN
                        ALTER TABLE ""{schemaName}"".""FichaTecnica""
                        ADD CONSTRAINT ""FK_FichaTecnica_Receita_ReceitaPrincipalId""
                        FOREIGN KEY (""ReceitaPrincipalId"") REFERENCES ""{schemaName}"".""Receita""(""Id"") ON DELETE RESTRICT;
                    END IF;

                    -- FichaTecnica.TempoPreparo
                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.tables
                        WHERE table_schema = '{schemaName}'
                        AND table_name = 'FichaTecnica'
                    ) AND NOT EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = '{schemaName}'
                        AND table_name = 'FichaTecnica'
                        AND column_name = 'TempoPreparo'
                    ) THEN
                        ALTER TABLE ""{schemaName}"".""FichaTecnica""
                        ADD COLUMN ""TempoPreparo"" INTEGER;
                    END IF;

                    -- Insumo.IPCValor
                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.tables
                        WHERE table_schema = '{schemaName}'
                        AND table_name = 'Insumo'
                    ) AND NOT EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = '{schemaName}'
                        AND table_name = 'Insumo'
                        AND column_name = 'IPCValor'
                    ) THEN
                        ALTER TABLE ""{schemaName}"".""Insumo""
                        ADD COLUMN ""IPCValor"" DECIMAL(18, 4);
                    END IF;

                    -- FichaTecnica.RendimentoPorcoes: alterar de DECIMAL para VARCHAR(200) se necessário
                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = '{schemaName}'
                        AND table_name = 'FichaTecnica'
                        AND column_name = 'RendimentoPorcoes'
                        AND data_type = 'numeric'
                    ) THEN
                        ALTER TABLE ""{schemaName}"".""FichaTecnica""
                        ALTER COLUMN ""RendimentoPorcoes"" TYPE VARCHAR(200) USING ""RendimentoPorcoes""::
text;
                    END IF;

                    -- Criar tabela CanalVenda se não existir
                    IF NOT EXISTS (
                        SELECT 1
                        FROM information_schema.tables
                        WHERE table_schema = '{schemaName}'
                        AND table_name = 'CanalVenda'
                    ) THEN
                        CREATE TABLE ""{schemaName}"".""CanalVenda"" (
                            ""Id"" BIGSERIAL PRIMARY KEY,
                            ""Nome"" VARCHAR(100) NOT NULL,
                            ""TaxaPercentualPadrao"" DECIMAL(18, 4),
                            ""IsAtivo"" BOOLEAN NOT NULL DEFAULT true,
                            ""UsuarioCriacao"" VARCHAR(100),
                            ""UsuarioAtualizacao"" VARCHAR(100),
                            ""DataCriacao"" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc'),
                            ""DataAtualizacao"" TIMESTAMP WITH TIME ZONE
                        );
                        CREATE UNIQUE INDEX ""IX_CanalVenda_Nome_{schemaName}"" ON ""{schemaName}"".""CanalVenda""(""Nome"");
                        
                        -- Migrar dados existentes de FichaTecnicaCanal para CanalVenda
                        INSERT INTO ""{schemaName}"".""CanalVenda"" (""Nome"", ""IsAtivo"", ""UsuarioCriacao"", ""DataCriacao"")
                        SELECT DISTINCT 
                            COALESCE(""Canal"", 'Canal Desconhecido') as ""Nome"",
                            true as ""IsAtivo"",
                            'Sistema' as ""UsuarioCriacao"",
                            NOW() as ""DataCriacao""
                        FROM ""{schemaName}"".""FichaTecnicaCanal""
                        WHERE ""Canal"" IS NOT NULL AND ""Canal"" != ''
                        ON CONFLICT (""Nome"") DO NOTHING;
                    END IF;
                    
                    -- Simplificar tabela CanalVenda se já existir (remover campos antigos)
                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.tables
                        WHERE table_schema = '{schemaName}'
                        AND table_name = 'CanalVenda'
                    ) THEN
                        -- Remover índice único de Codigo se existir
                        IF EXISTS (
                            SELECT 1
                            FROM pg_indexes
                            WHERE schemaname = '{schemaName}'
                            AND tablename = 'CanalVenda'
                            AND indexname = 'IX_CanalVenda_Codigo_{schemaName}'
                        ) THEN
                            DROP INDEX ""{schemaName}"".""IX_CanalVenda_Codigo_{schemaName}"";
                        END IF;
                        
                        -- Remover colunas antigas se existirem
                        IF EXISTS (
                            SELECT 1
                            FROM information_schema.columns
                            WHERE table_schema = '{schemaName}'
                            AND table_name = 'CanalVenda'
                            AND column_name = 'Codigo'
                        ) THEN
                            ALTER TABLE ""{schemaName}"".""CanalVenda"" DROP COLUMN ""Codigo"";
                        END IF;
                        
                        IF EXISTS (
                            SELECT 1
                            FROM information_schema.columns
                            WHERE table_schema = '{schemaName}'
                            AND table_name = 'CanalVenda'
                            AND column_name = 'Descricao'
                        ) THEN
                            ALTER TABLE ""{schemaName}"".""CanalVenda"" DROP COLUMN ""Descricao"";
                        END IF;
                        
                        IF EXISTS (
                            SELECT 1
                            FROM information_schema.columns
                            WHERE table_schema = '{schemaName}'
                            AND table_name = 'CanalVenda'
                            AND column_name = 'ComissaoPercentualPadrao'
                        ) THEN
                            ALTER TABLE ""{schemaName}"".""CanalVenda"" DROP COLUMN ""ComissaoPercentualPadrao"";
                        END IF;
                        
                        IF EXISTS (
                            SELECT 1
                            FROM information_schema.columns
                            WHERE table_schema = '{schemaName}'
                            AND table_name = 'CanalVenda'
                            AND column_name = 'MultiplicadorPadrao'
                        ) THEN
                            ALTER TABLE ""{schemaName}"".""CanalVenda"" DROP COLUMN ""MultiplicadorPadrao"";
                        END IF;
                        
                        IF EXISTS (
                            SELECT 1
                            FROM information_schema.columns
                            WHERE table_schema = '{schemaName}'
                            AND table_name = 'CanalVenda'
                            AND column_name = 'Observacoes'
                        ) THEN
                            ALTER TABLE ""{schemaName}"".""CanalVenda"" DROP COLUMN ""Observacoes"";
                        END IF;
                        
                        -- Criar índice único de Nome se não existir
                        IF NOT EXISTS (
                            SELECT 1
                            FROM pg_indexes
                            WHERE schemaname = '{schemaName}'
                            AND tablename = 'CanalVenda'
                            AND indexname = 'IX_CanalVenda_Nome_{schemaName}'
                        ) THEN
                            CREATE UNIQUE INDEX ""IX_CanalVenda_Nome_{schemaName}"" ON ""{schemaName}"".""CanalVenda""(""Nome"");
                        END IF;
                        
                        -- Criar canais padrão se não existirem
                        INSERT INTO ""{schemaName}"".""CanalVenda"" (""Nome"", ""TaxaPercentualPadrao"", ""IsAtivo"", ""UsuarioCriacao"", ""DataCriacao"")
                        VALUES 
                            ('iFood 1', 13, true, 'Sistema', NOW() AT TIME ZONE 'utc'),
                            ('iFood 2', 25, true, 'Sistema', NOW() AT TIME ZONE 'utc'),
                            ('Balcão', 0, true, 'Sistema', NOW() AT TIME ZONE 'utc'),
                            ('Delivery Próprio', 0, true, 'Sistema', NOW() AT TIME ZONE 'utc')
                        ON CONFLICT (""Nome"") DO NOTHING;
                    END IF;

                    -- Adicionar coluna CanalVendaId em FichaTecnicaCanal se não existir
                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.tables
                        WHERE table_schema = '{schemaName}'
                        AND table_name = 'FichaTecnicaCanal'
                    ) AND NOT EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = '{schemaName}'
                        AND table_name = 'FichaTecnicaCanal'
                        AND column_name = 'CanalVendaId'
                    ) THEN
                        ALTER TABLE ""{schemaName}"".""FichaTecnicaCanal""
                        ADD COLUMN ""CanalVendaId"" BIGINT;
                        
                        -- Criar índice
                        CREATE INDEX ""IX_FichaTecnicaCanal_CanalVendaId_{schemaName}"" ON ""{schemaName}"".""FichaTecnicaCanal""(""CanalVendaId"");
                        
                        -- Adicionar foreign key
                        ALTER TABLE ""{schemaName}"".""FichaTecnicaCanal""
                        ADD CONSTRAINT ""FK_FichaTecnicaCanal_CanalVenda_{schemaName}""
                        FOREIGN KEY (""CanalVendaId"") REFERENCES ""{schemaName}"".""CanalVenda""(""Id"") ON DELETE RESTRICT;
                        
                        -- Atualizar FichaTecnicaCanal para referenciar os canais criados
                        UPDATE ""{schemaName}"".""FichaTecnicaCanal"" ftc
                        SET ""CanalVendaId"" = cv.""Id""
                        FROM ""{schemaName}"".""CanalVenda"" cv
                        WHERE ftc.""Canal"" = cv.""Nome""
                        AND ftc.""CanalVendaId"" IS NULL;
                    END IF;
                END $$;

                -- Índice (idempotente)
                -- (Índice movido para dentro do bloco DO para evitar erro quando a tabela ainda não existe)
            ";

            await _tenantContext.Database.ExecuteSqlRawAsync(addColumnsSql);
            _logger.LogInformation("Migração de estrutura antiga concluída no schema {Schema}", schemaName);
        }
        catch (Exception ex)
        {
            // Log mas não falha - pode ser que a coluna não exista (schema novo)
            _logger.LogWarning(ex, "Erro ao executar migração de estrutura antiga no schema {Schema} (pode ser normal se o schema for novo)", schemaName);
        }
    }

    private async Task CreateTenantTablesAsync(string schemaName)
    {
        var sql = $@"
            -- Tabela Perfil
            CREATE TABLE IF NOT EXISTS ""{schemaName}"".""Perfil"" (
                ""Id"" BIGSERIAL PRIMARY KEY,
                ""Nome"" VARCHAR(100) NOT NULL,
                ""IsAtivo"" BOOLEAN NOT NULL DEFAULT true,
                ""UsuarioCriacao"" VARCHAR(100),
                ""UsuarioAtualizacao"" VARCHAR(100),
                ""DataCriacao"" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc'),
                ""DataAtualizacao"" TIMESTAMP WITH TIME ZONE
            );

            -- Tabela Usuario
            CREATE TABLE IF NOT EXISTS ""{schemaName}"".""Usuario"" (
                ""Id"" BIGSERIAL PRIMARY KEY,
                ""PerfilId"" BIGINT NOT NULL,
                ""IsAtivo"" BOOLEAN NOT NULL DEFAULT true,
                ""NomeCompleto"" VARCHAR(200) NOT NULL,
                ""Email"" VARCHAR(200) NOT NULL,
                ""SenhaHash"" VARCHAR(500) NOT NULL,
                ""PathImagem"" VARCHAR(500),
                ""UsuarioCriacao"" VARCHAR(100),
                ""UsuarioAtualizacao"" VARCHAR(100),
                ""DataCriacao"" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc'),
                ""DataAtualizacao"" TIMESTAMP WITH TIME ZONE,
                CONSTRAINT ""FK_Usuario_Perfil_{schemaName}"" FOREIGN KEY (""PerfilId"") REFERENCES ""{schemaName}"".""Perfil""(""Id"") ON DELETE RESTRICT
            );

            -- Ãndice Ãºnico para Email
            CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Usuario_Email_{schemaName}"" ON ""{schemaName}"".""Usuario""(""Email"");

            -- Tabela CategoriaInsumo
            CREATE TABLE IF NOT EXISTS ""{schemaName}"".""CategoriaInsumo"" (
                ""Id"" BIGSERIAL PRIMARY KEY,
                ""Nome"" VARCHAR(100) NOT NULL,
                ""IsAtivo"" BOOLEAN NOT NULL DEFAULT true,
                ""UsuarioCriacao"" VARCHAR(100),
                ""UsuarioAtualizacao"" VARCHAR(100),
                ""DataCriacao"" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc'),
                ""DataAtualizacao"" TIMESTAMP WITH TIME ZONE
            );

            -- Ãndice Ãºnico para Nome da Categoria
            CREATE UNIQUE INDEX IF NOT EXISTS ""IX_CategoriaInsumo_Nome_{schemaName}"" ON ""{schemaName}"".""CategoriaInsumo""(""Nome"");

            -- Tabela UnidadeMedida
            CREATE TABLE IF NOT EXISTS ""{schemaName}"".""UnidadeMedida"" (
                ""Id"" BIGSERIAL PRIMARY KEY,
                ""Nome"" VARCHAR(50) NOT NULL,
                ""Sigla"" VARCHAR(10) NOT NULL,
                ""IsAtivo"" BOOLEAN NOT NULL DEFAULT true,
                ""UsuarioCriacao"" VARCHAR(100),
                ""UsuarioAtualizacao"" VARCHAR(100),
                ""DataCriacao"" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc'),
                ""DataAtualizacao"" TIMESTAMP WITH TIME ZONE
            );

            -- Ãndices Ãºnicos para Nome e Sigla da Unidade
            CREATE UNIQUE INDEX IF NOT EXISTS ""IX_UnidadeMedida_Nome_{schemaName}"" ON ""{schemaName}"".""UnidadeMedida""(""Nome"");
            CREATE UNIQUE INDEX IF NOT EXISTS ""IX_UnidadeMedida_Sigla_{schemaName}"" ON ""{schemaName}"".""UnidadeMedida""(""Sigla"");

            -- Tabela Insumo
            CREATE TABLE IF NOT EXISTS ""{schemaName}"".""Insumo"" (
                ""Id"" BIGSERIAL PRIMARY KEY,
                ""Nome"" VARCHAR(200) NOT NULL,
                ""CategoriaId"" BIGINT NOT NULL,
                ""UnidadeCompraId"" BIGINT NOT NULL,
                ""QuantidadePorEmbalagem"" DECIMAL(18, 4) NOT NULL,
                ""CustoUnitario"" DECIMAL(18, 4) NOT NULL DEFAULT 0,
                ""FatorCorrecao"" DECIMAL(18, 4) NOT NULL DEFAULT 1.0,
                ""IPCValor"" DECIMAL(18, 4),
                ""Descricao"" TEXT,
                ""PathImagem"" VARCHAR(500),
                ""IsAtivo"" BOOLEAN NOT NULL DEFAULT true,
                ""UsuarioCriacao"" VARCHAR(100),
                ""UsuarioAtualizacao"" VARCHAR(100),
                ""DataCriacao"" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc'),
                ""DataAtualizacao"" TIMESTAMP WITH TIME ZONE,
                CONSTRAINT ""FK_Insumo_CategoriaInsumo_{schemaName}"" FOREIGN KEY (""CategoriaId"") REFERENCES ""{schemaName}"".""CategoriaInsumo""(""Id"") ON DELETE RESTRICT,
                CONSTRAINT ""FK_Insumo_UnidadeCompra_{schemaName}"" FOREIGN KEY (""UnidadeCompraId"") REFERENCES ""{schemaName}"".""UnidadeMedida""(""Id"") ON DELETE RESTRICT
            );

            -- Tabela CategoriaReceita
            CREATE TABLE IF NOT EXISTS ""{schemaName}"".""CategoriaReceita"" (
                ""Id"" BIGSERIAL PRIMARY KEY,
                ""Nome"" VARCHAR(100) NOT NULL,
                ""IsAtivo"" BOOLEAN NOT NULL DEFAULT true,
                ""UsuarioCriacao"" VARCHAR(100),
                ""UsuarioAtualizacao"" VARCHAR(100),
                ""DataCriacao"" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc'),
                ""DataAtualizacao"" TIMESTAMP WITH TIME ZONE
            );
            CREATE UNIQUE INDEX IF NOT EXISTS ""IX_CategoriaReceita_Nome_{schemaName}"" ON ""{schemaName}"".""CategoriaReceita""(""Nome"");

            -- Tabela Receita
            CREATE TABLE IF NOT EXISTS ""{schemaName}"".""Receita"" (
                ""Id"" BIGSERIAL PRIMARY KEY,
                ""Nome"" VARCHAR(200) NOT NULL,
                ""CategoriaId"" BIGINT NOT NULL,
                ""Descricao"" TEXT,
                ""Conservacao"" TEXT,
                ""InstrucoesEmpratamento"" VARCHAR(2000),
                ""Rendimento"" DECIMAL(18, 4) NOT NULL,
                ""PesoPorPorcao"" DECIMAL(18, 4),
                ""FatorRendimento"" DECIMAL(18, 4) NOT NULL DEFAULT 1.0,
                ""TempoPreparo"" INTEGER,
                ""Versao"" VARCHAR(20) DEFAULT '1.0',
                ""CustoTotal"" DECIMAL(18, 4) NOT NULL DEFAULT 0,
                ""CustoPorPorcao"" DECIMAL(18, 4) NOT NULL DEFAULT 0,
                ""PathImagem"" VARCHAR(500),
                ""IsAtivo"" BOOLEAN NOT NULL DEFAULT true,
                ""UsuarioCriacao"" VARCHAR(100),
                ""UsuarioAtualizacao"" VARCHAR(100),
                ""DataCriacao"" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc'),
                ""DataAtualizacao"" TIMESTAMP WITH TIME ZONE,
                CONSTRAINT ""FK_Receita_CategoriaReceita_{schemaName}"" FOREIGN KEY (""CategoriaId"") REFERENCES ""{schemaName}"".""CategoriaReceita""(""Id"") ON DELETE RESTRICT
            );

            -- Tabela ReceitaItem
            CREATE TABLE IF NOT EXISTS ""{schemaName}"".""ReceitaItem"" (
                ""Id"" BIGSERIAL PRIMARY KEY,
                ""ReceitaId"" BIGINT NOT NULL,
                ""InsumoId"" BIGINT NOT NULL,
                ""Quantidade"" DECIMAL(18, 4) NOT NULL,
                ""UnidadeMedidaId"" BIGINT NOT NULL,
                ""ExibirComoQB"" BOOLEAN NOT NULL DEFAULT false,
                ""Ordem"" INTEGER NOT NULL,
                ""Observacoes"" TEXT,
                CONSTRAINT ""FK_ReceitaItem_Receita_{schemaName}"" FOREIGN KEY (""ReceitaId"") REFERENCES ""{schemaName}"".""Receita""(""Id"") ON DELETE CASCADE,
                CONSTRAINT ""FK_ReceitaItem_Insumo_{schemaName}"" FOREIGN KEY (""InsumoId"") REFERENCES ""{schemaName}"".""Insumo""(""Id"") ON DELETE RESTRICT,
                CONSTRAINT ""FK_ReceitaItem_UnidadeMedida_{schemaName}"" FOREIGN KEY (""UnidadeMedidaId"") REFERENCES ""{schemaName}"".""UnidadeMedida""(""Id"") ON DELETE RESTRICT
            );
            CREATE INDEX IF NOT EXISTS ""IX_ReceitaItem_ReceitaId_Ordem_{schemaName}"" ON ""{schemaName}"".""ReceitaItem""(""ReceitaId"", ""Ordem"");

            -- Tabela FichaTecnica
            CREATE TABLE IF NOT EXISTS ""{schemaName}"".""FichaTecnica"" (
                ""Id"" BIGSERIAL PRIMARY KEY,
                ""CategoriaId"" BIGINT NOT NULL,
                ""ReceitaPrincipalId"" BIGINT,
                ""Nome"" VARCHAR(200) NOT NULL,
                ""Codigo"" VARCHAR(50),
                ""DescricaoComercial"" TEXT,
                ""CustoTotal"" DECIMAL(18, 4) NOT NULL DEFAULT 0,
                ""CustoPorUnidade"" DECIMAL(18, 4) NOT NULL DEFAULT 0,
                ""RendimentoFinal"" DECIMAL(18, 4),
                ""IndiceContabil"" DECIMAL(18, 4),
                ""PrecoSugeridoVenda"" DECIMAL(18, 4),
                ""ICOperador"" CHAR(1),
                ""ICValor"" INTEGER,
                ""IPCValor"" DECIMAL(18, 4),
                ""MargemAlvoPercentual"" DECIMAL(18, 4),
                ""PorcaoVendaQuantidade"" DECIMAL(18, 4),
                ""PorcaoVendaUnidadeMedidaId"" BIGINT,
                ""RendimentoPorcoes"" VARCHAR(200),
                ""TempoPreparo"" INTEGER,
                ""IsAtivo"" BOOLEAN NOT NULL DEFAULT true,
                ""UsuarioCriacao"" VARCHAR(100),
                ""UsuarioAtualizacao"" VARCHAR(100),
                ""DataCriacao"" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc'),
                ""DataAtualizacao"" TIMESTAMP WITH TIME ZONE,
                CONSTRAINT ""FK_FichaTecnica_CategoriaReceita_{schemaName}"" FOREIGN KEY (""CategoriaId"") REFERENCES ""{schemaName}"".""CategoriaReceita""(""Id"") ON DELETE RESTRICT,
                CONSTRAINT ""FK_FichaTecnica_Receita_ReceitaPrincipalId"" FOREIGN KEY (""ReceitaPrincipalId"") REFERENCES ""{schemaName}"".""Receita""(""Id"") ON DELETE RESTRICT
            );
            CREATE INDEX IF NOT EXISTS ""IX_FichaTecnica_ReceitaPrincipalId_{schemaName}"" ON ""{schemaName}"".""FichaTecnica""(""ReceitaPrincipalId"");

            -- Tabela FichaTecnicaItem
            CREATE TABLE IF NOT EXISTS ""{schemaName}"".""FichaTecnicaItem"" (
                ""Id"" BIGSERIAL PRIMARY KEY,
                ""FichaTecnicaId"" BIGINT NOT NULL,
                ""TipoItem"" VARCHAR(20) NOT NULL,
                ""ReceitaId"" BIGINT,
                ""InsumoId"" BIGINT,
                ""Quantidade"" DECIMAL(18, 4) NOT NULL,
                ""UnidadeMedidaId"" BIGINT NOT NULL,
                ""ExibirComoQB"" BOOLEAN NOT NULL DEFAULT false,
                ""Ordem"" INTEGER NOT NULL,
                ""Observacoes"" TEXT,
                ""UsuarioCriacao"" VARCHAR(100),
                ""UsuarioAtualizacao"" VARCHAR(100),
                ""DataCriacao"" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc'),
                ""DataAtualizacao"" TIMESTAMP WITH TIME ZONE,
                CONSTRAINT ""FK_FichaTecnicaItem_FichaTecnica_{schemaName}"" FOREIGN KEY (""FichaTecnicaId"") REFERENCES ""{schemaName}"".""FichaTecnica""(""Id"") ON DELETE CASCADE,
                CONSTRAINT ""FK_FichaTecnicaItem_Receita_{schemaName}"" FOREIGN KEY (""ReceitaId"") REFERENCES ""{schemaName}"".""Receita""(""Id"") ON DELETE RESTRICT,
                CONSTRAINT ""FK_FichaTecnicaItem_Insumo_{schemaName}"" FOREIGN KEY (""InsumoId"") REFERENCES ""{schemaName}"".""Insumo""(""Id"") ON DELETE RESTRICT,
                CONSTRAINT ""FK_FichaTecnicaItem_UnidadeMedida_{schemaName}"" FOREIGN KEY (""UnidadeMedidaId"") REFERENCES ""{schemaName}"".""UnidadeMedida""(""Id"") ON DELETE RESTRICT,
                CONSTRAINT ""CK_FichaTecnicaItem_TipoItem_{schemaName}"" CHECK ((""TipoItem"" = 'Receita' AND ""ReceitaId"" IS NOT NULL AND ""InsumoId"" IS NULL) OR (""TipoItem"" = 'Insumo' AND ""InsumoId"" IS NOT NULL AND ""ReceitaId"" IS NULL))
            );
            CREATE INDEX IF NOT EXISTS ""IX_FichaTecnicaItem_FichaTecnicaId_Ordem_{schemaName}"" ON ""{schemaName}"".""FichaTecnicaItem""(""FichaTecnicaId"", ""Ordem"");

            -- Tabela CanalVenda
            CREATE TABLE IF NOT EXISTS ""{schemaName}"".""CanalVenda"" (
                ""Id"" BIGSERIAL PRIMARY KEY,
                ""Nome"" VARCHAR(100) NOT NULL,
                ""TaxaPercentualPadrao"" DECIMAL(18, 4),
                ""IsAtivo"" BOOLEAN NOT NULL DEFAULT true,
                ""UsuarioCriacao"" VARCHAR(100),
                ""UsuarioAtualizacao"" VARCHAR(100),
                ""DataCriacao"" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc'),
                ""DataAtualizacao"" TIMESTAMP WITH TIME ZONE
            );
            CREATE UNIQUE INDEX IF NOT EXISTS ""IX_CanalVenda_Nome_{schemaName}"" ON ""{schemaName}"".""CanalVenda""(""Nome"");

            -- Tabela FichaTecnicaCanal
            CREATE TABLE IF NOT EXISTS ""{schemaName}"".""FichaTecnicaCanal"" (
                ""Id"" BIGSERIAL PRIMARY KEY,
                ""FichaTecnicaId"" BIGINT NOT NULL,
                ""CanalVendaId"" BIGINT,
                ""Canal"" VARCHAR(50) NOT NULL,
                ""NomeExibicao"" VARCHAR(100),
                ""PrecoVenda"" DECIMAL(18, 4) NOT NULL DEFAULT 0,
                ""TaxaPercentual"" DECIMAL(18, 4),
                ""ComissaoPercentual"" DECIMAL(18, 4),
                ""Multiplicador"" DECIMAL(18, 4),
                ""MargemCalculadaPercentual"" DECIMAL(18, 4),
                ""Observacoes"" TEXT,
                ""IsAtivo"" BOOLEAN NOT NULL DEFAULT true,
                CONSTRAINT ""FK_FichaTecnicaCanal_FichaTecnica_{schemaName}"" FOREIGN KEY (""FichaTecnicaId"") REFERENCES ""{schemaName}"".""FichaTecnica""(""Id"") ON DELETE CASCADE,
                CONSTRAINT ""FK_FichaTecnicaCanal_CanalVenda_{schemaName}"" FOREIGN KEY (""CanalVendaId"") REFERENCES ""{schemaName}"".""CanalVenda""(""Id"") ON DELETE RESTRICT
            );
            CREATE INDEX IF NOT EXISTS ""IX_FichaTecnicaCanal_CanalVendaId_{schemaName}"" ON ""{schemaName}"".""FichaTecnicaCanal""(""CanalVendaId"");
        ";

        await _tenantContext.Database.ExecuteSqlRawAsync(sql);
    }

    private async Task InitializeTenantDataAsync(string schemaName, string? usuarioCriacao)
    {
        _logger.LogInformation("Inicializando dados do tenant no schema {Schema}", schemaName);
        
        var usuarioCriacaoValue = usuarioCriacao ?? "Sistema";
        var dataCriacao = "NOW() AT TIME ZONE 'utc'";
        
        // Usar SQL direto para inserir os dados iniciais
        var sql = $@"
            -- Inserir Perfil ""Administrador"" e perfis padrï¿½ï¿½o
            INSERT INTO ""{schemaName}"".""Perfil"" (""Nome"", ""IsAtivo"", ""UsuarioCriacao"", ""DataCriacao"")
            VALUES ('Administrador', true, '{usuarioCriacaoValue}', {dataCriacao})
            ON CONFLICT DO NOTHING;

            INSERT INTO ""{schemaName}"".""Perfil"" (""Nome"", ""IsAtivo"", ""UsuarioCriacao"", ""DataCriacao"")
            VALUES 
                ('Diretor', true, '{usuarioCriacaoValue}', {dataCriacao}),
                ('Gerï¿½ï¿½ncia', true, '{usuarioCriacaoValue}', {dataCriacao}),
                ('Coordenador de Equipe', true, '{usuarioCriacaoValue}', {dataCriacao}),
                ('Chef on Line', true, '{usuarioCriacaoValue}', {dataCriacao})
            ON CONFLICT DO NOTHING;

            -- Inserir Categorias de Insumo padrão
            INSERT INTO ""{schemaName}"".""CategoriaInsumo"" (""Nome"", ""IsAtivo"", ""UsuarioCriacao"", ""DataCriacao"")
            VALUES 
                ('Hortifruti', true, '{usuarioCriacaoValue}', {dataCriacao}),
                ('Carnes e Aves', true, '{usuarioCriacaoValue}', {dataCriacao}),
                ('Peixes e Frutos do Mar', true, '{usuarioCriacaoValue}', {dataCriacao}),
                ('Laticínios', true, '{usuarioCriacaoValue}', {dataCriacao}),
                ('Grãos e Cereais', true, '{usuarioCriacaoValue}', {dataCriacao}),
                ('Massas e Farinhas', true, '{usuarioCriacaoValue}', {dataCriacao}),
                ('Bebidas', true, '{usuarioCriacaoValue}', {dataCriacao}),
                ('Condimentos e Temperos', true, '{usuarioCriacaoValue}', {dataCriacao}),
                ('Óleos e Gorduras', true, '{usuarioCriacaoValue}', {dataCriacao}),
                ('Limpeza e Higiene', true, '{usuarioCriacaoValue}', {dataCriacao}),
                ('Embalagens', true, '{usuarioCriacaoValue}', {dataCriacao}),
                ('Outros', true, '{usuarioCriacaoValue}', {dataCriacao})
            ON CONFLICT DO NOTHING;

            -- Inserir Unidades de Medida padrão simplificadas
            INSERT INTO ""{schemaName}"".""UnidadeMedida"" (""Nome"", ""Sigla"", ""IsAtivo"", ""UsuarioCriacao"", ""DataCriacao"")
            VALUES 
                ('Unidade', 'UN', true, '{usuarioCriacaoValue}', {dataCriacao}),
                ('Grama', 'GR', true, '{usuarioCriacaoValue}', {dataCriacao}),
                ('Mililitro', 'ML', true, '{usuarioCriacaoValue}', {dataCriacao})
            ON CONFLICT DO NOTHING;

            -- Inserir Categorias de Receita padrÃ£o
            INSERT INTO ""{schemaName}"".""CategoriaReceita"" (""Nome"", ""IsAtivo"", ""UsuarioCriacao"", ""DataCriacao"")
            VALUES 
                ('Entrada', true, '{usuarioCriacaoValue}', {dataCriacao}),
                ('Prato Principal', true, '{usuarioCriacaoValue}', {dataCriacao}),
                ('Sobremesa', true, '{usuarioCriacaoValue}', {dataCriacao}),
                ('Bebida', true, '{usuarioCriacaoValue}', {dataCriacao}),
                ('Acompanhamento', true, '{usuarioCriacaoValue}', {dataCriacao}),
                ('Salada', true, '{usuarioCriacaoValue}', {dataCriacao}),
                ('Sopa', true, '{usuarioCriacaoValue}', {dataCriacao}),
                ('Outros', true, '{usuarioCriacaoValue}', {dataCriacao})
            ON CONFLICT DO NOTHING;

            -- Inserir Canais de Venda padrão
            INSERT INTO ""{schemaName}"".""CanalVenda"" (""Nome"", ""TaxaPercentualPadrao"", ""IsAtivo"", ""UsuarioCriacao"", ""DataCriacao"")
            VALUES 
                ('iFood 1', 13, true, '{usuarioCriacaoValue}', {dataCriacao}),
                ('iFood 2', 25, true, '{usuarioCriacaoValue}', {dataCriacao}),
                ('Balcão', 0, true, '{usuarioCriacaoValue}', {dataCriacao}),
                ('Delivery Próprio', 0, true, '{usuarioCriacaoValue}', {dataCriacao})
            ON CONFLICT (""Nome"") DO NOTHING;
        ";

        await _tenantContext.Database.ExecuteSqlRawAsync(sql);
        _logger.LogInformation("Dados do tenant inicializados com sucesso no schema {Schema}", schemaName);
    }

    private async Task CreateAdminUserAsync(string schemaName, CreateAdminRequest adminRequest, string? usuarioCriacao)
    {
        _logger.LogInformation("Criando usuário administrador no schema {Schema}", schemaName);
        
        // Buscar o perfil Administrador via SQL usando parâmetros
        var perfilIdSql = $@"SELECT ""Id"" FROM ""{schemaName}"".""Perfil"" WHERE ""Nome"" = 'Administrador' LIMIT 1";
        var connection = _tenantContext.Database.GetDbConnection();
        await connection.OpenAsync();
        
        try
        {
            var command = connection.CreateCommand();
            command.CommandText = perfilIdSql;
            var perfilIdResult = await command.ExecuteScalarAsync();
            
            if (perfilIdResult == null || perfilIdResult == DBNull.Value)
            {
                throw new InvalidOperationException("Perfil Administrador não encontrado");
            }

            var perfilIdValue = Convert.ToInt64(perfilIdResult);

            // Verificar se o email já existe usando parâmetros
            var emailCheckSql = $@"SELECT COUNT(*) FROM ""{schemaName}"".""Usuario"" WHERE ""Email"" = @email";
            command.CommandText = emailCheckSql;
            var emailParam = command.CreateParameter();
            emailParam.ParameterName = "@email";
            emailParam.Value = adminRequest.Email;
            command.Parameters.Clear();
            command.Parameters.Add(emailParam);
            var emailCount = Convert.ToInt32(await command.ExecuteScalarAsync() ?? 0);
            
            if (emailCount > 0)
            {
                throw new BusinessException("Email do administrador já está em uso");
            }

            // Inserir usuário administrador usando parâmetros
            var senhaHash = BCrypt.Net.BCrypt.HashPassword(adminRequest.Senha);
            var usuarioCriacaoValue = usuarioCriacao ?? "Sistema";
            var insertSql = $@"
                INSERT INTO ""{schemaName}"".""Usuario"" 
                (""PerfilId"", ""NomeCompleto"", ""Email"", ""SenhaHash"", ""IsAtivo"", ""UsuarioCriacao"", ""DataCriacao"")
                VALUES 
                (@perfilId, @nomeCompleto, @email, @senhaHash, true, @usuarioCriacao, NOW() AT TIME ZONE 'utc')
            ";

            command.CommandText = insertSql;
            command.Parameters.Clear();
            command.Parameters.Add(new Npgsql.NpgsqlParameter("@perfilId", perfilIdValue));
            command.Parameters.Add(new Npgsql.NpgsqlParameter("@nomeCompleto", adminRequest.NomeCompleto));
            command.Parameters.Add(new Npgsql.NpgsqlParameter("@email", adminRequest.Email));
            command.Parameters.Add(new Npgsql.NpgsqlParameter("@senhaHash", senhaHash));
            command.Parameters.Add(new Npgsql.NpgsqlParameter("@usuarioCriacao", usuarioCriacaoValue));
            await command.ExecuteNonQueryAsync();
            
            _logger.LogInformation("Usuário administrador criado com sucesso no schema {Schema}", schemaName);
        }
        finally
        {
            await connection.CloseAsync();
        }
    }
}
