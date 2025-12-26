using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SGR.Api.Models.DTOs;
using SGR.Api.Services.Interfaces;
using System.Linq.Expressions;

namespace SGR.Api.Services.Common;

/// <summary>
/// Service base genérico que funciona com qualquer DbContext
/// </summary>
public abstract class BaseService<TDbContext, TEntity, TDto, TCreateRequest, TUpdateRequest> 
    : IBaseService<TEntity, TDto, TCreateRequest, TUpdateRequest>
    where TDbContext : DbContext
    where TEntity : class
    where TDto : class
    where TCreateRequest : class
    where TUpdateRequest : class
{
    protected readonly TDbContext _context;
    protected readonly DbSet<TEntity> _dbSet;
    protected readonly ILogger _logger;

    protected BaseService(TDbContext context, ILogger logger)
    {
        _context = context;
        _dbSet = context.Set<TEntity>();
        _logger = logger;
    }

    // Métodos virtuais para customização
    protected virtual IQueryable<TEntity> ApplySearch(IQueryable<TEntity> query, string? search) => query;
    protected virtual IQueryable<TEntity> ApplySorting(IQueryable<TEntity> query, string? sort, string? order) => query;
    protected abstract Expression<Func<TEntity, TDto>> MapToDto();
    protected abstract TEntity MapToEntity(TCreateRequest request);
    protected abstract void UpdateEntity(TEntity entity, TUpdateRequest request);
    protected virtual Task BeforeCreateAsync(TEntity entity, TCreateRequest request, string? usuarioCriacao) => Task.CompletedTask;
    protected virtual Task BeforeUpdateAsync(TEntity entity, TUpdateRequest request, string? usuarioAtualizacao) => Task.CompletedTask;
    protected virtual Task BeforeDeleteAsync(TEntity entity) => Task.CompletedTask;

    public virtual async Task<PagedResult<TDto>> GetAllAsync(string? search, int page, int pageSize, string? sort, string? order)
    {
        _logger.LogInformation("Buscando {EntityType} - Página: {Page}, Tamanho: {PageSize}, Busca: {Search}", 
            typeof(TEntity).Name, page, pageSize, search ?? "N/A");

        // Usar AsNoTracking para queries de leitura (melhor performance)
        var query = _dbSet.AsNoTracking().AsQueryable();
        
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

        _logger.LogInformation("Encontrados {Total} registros de {EntityType}", total, typeof(TEntity).Name);

        return new PagedResult<TDto> { Items = items, Total = total };
    }

    public virtual async Task<TDto?> GetByIdAsync(long id)
    {
        _logger.LogInformation("Buscando {EntityType} por ID: {Id}", typeof(TEntity).Name, id);

        // Usar AsNoTracking para queries de leitura (melhor performance)
        // Usar FirstOrDefaultAsync ao invÃ©s de FindAsync para garantir que respeita o schema do tenant
        var entity = await _dbSet.AsNoTracking().FirstOrDefaultAsync(e => EF.Property<long>(e, "Id") == id);
        if (entity == null)
        {
            _logger.LogWarning("{EntityType} com ID {Id} não encontrado", typeof(TEntity).Name, id);
            return null;
        }

        var mapper = MapToDto().Compile();
        return mapper(entity);
    }

    public virtual async Task<TDto> CreateAsync(TCreateRequest request, string? usuarioCriacao)
    {
        _logger.LogInformation("Criando novo {EntityType} - Usuário: {Usuario}", typeof(TEntity).Name, usuarioCriacao ?? "Sistema");

        try
        {
            var entity = MapToEntity(request);
            
            // Setar campos de auditoria se existirem
            SetAuditFieldsOnCreate(entity, request, usuarioCriacao);
            
            await BeforeCreateAsync(entity, request, usuarioCriacao);
            
            _dbSet.Add(entity);
            await _context.SaveChangesAsync();

            var entityId = GetEntityId(entity);
            _logger.LogInformation("{EntityType} criado com sucesso - ID: {Id}", typeof(TEntity).Name, entityId);

            // Usar a entidade já tracked ao invés de fazer uma nova query
            // A entidade já tem o ID gerado após SaveChanges
            var mapper = MapToDto().Compile();
            return mapper(entity);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Erro ao criar {EntityType} - Violação de constraint", typeof(TEntity).Name);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar {EntityType}", typeof(TEntity).Name);
            throw;
        }
    }

    public virtual async Task<TDto?> UpdateAsync(long id, TUpdateRequest request, string? usuarioAtualizacao)
    {
        _logger.LogInformation("Atualizando {EntityType} - ID: {Id}, Usuário: {Usuario}", 
            typeof(TEntity).Name, id, usuarioAtualizacao ?? "Sistema");

        try
        {
            // Usar FirstOrDefaultAsync ao invés de FindAsync para garantir que respeita o schema do tenant
            // Não usar AsNoTracking aqui pois precisamos do tracking para update
            var entity = await _dbSet.FirstOrDefaultAsync(e => EF.Property<long>(e, "Id") == id);
            if (entity == null)
            {
                _logger.LogWarning("{EntityType} com ID {Id} não encontrado para atualização", typeof(TEntity).Name, id);
                return null;
            }

            UpdateEntity(entity, request);
            
            // Setar campos de auditoria se existirem
            SetAuditFieldsOnUpdate(entity, request, usuarioAtualizacao);
            
            await BeforeUpdateAsync(entity, request, usuarioAtualizacao);
            
            // Não é necessário marcar como Modified explicitamente quando a entidade já está tracked
            // O EF Core detecta automaticamente as mudanças
            await _context.SaveChangesAsync();

            _logger.LogInformation("{EntityType} atualizado com sucesso - ID: {Id}", typeof(TEntity).Name, id);

            // Usar a entidade já tracked ao invés de fazer uma nova query
            var mapper = MapToDto().Compile();
            return mapper(entity);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concorrência detectada ao atualizar {EntityType} - ID: {Id}", typeof(TEntity).Name, id);
            throw;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Erro ao atualizar {EntityType} - ID: {Id} - Violação de constraint", typeof(TEntity).Name, id);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar {EntityType} - ID: {Id}", typeof(TEntity).Name, id);
            throw;
        }
    }

    public virtual async Task<bool> DeleteAsync(long id)
    {
        _logger.LogInformation("Excluindo {EntityType} - ID: {Id}", typeof(TEntity).Name, id);

        try
        {
            // Usar FirstOrDefaultAsync ao invés de FindAsync para garantir que respeita o schema do tenant
            var entity = await _dbSet.FirstOrDefaultAsync(e => EF.Property<long>(e, "Id") == id);
            if (entity == null)
            {
                _logger.LogWarning("{EntityType} com ID {Id} não encontrado para exclusão", typeof(TEntity).Name, id);
                return false;
            }

            await BeforeDeleteAsync(entity);
            
            _dbSet.Remove(entity);
            await _context.SaveChangesAsync();

            _logger.LogInformation("{EntityType} excluído com sucesso - ID: {Id}", typeof(TEntity).Name, id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao excluir {EntityType} - ID: {Id}", typeof(TEntity).Name, id);
            throw;
        }
    }

    // Helpers para campos de auditoria usando reflection
    protected virtual void SetAuditFieldsOnCreate(object entity, TCreateRequest request, string? usuarioCriacao)
    {
        var type = entity.GetType();
        
        if (type.GetProperty("UsuarioCriacao") != null)
            type.GetProperty("UsuarioCriacao")?.SetValue(entity, usuarioCriacao);
            
        if (type.GetProperty("DataCriacao") != null)
            type.GetProperty("DataCriacao")?.SetValue(entity, DateTime.UtcNow);
    }

    protected virtual void SetAuditFieldsOnUpdate(object entity, TUpdateRequest request, string? usuarioAtualizacao)
    {
        var type = entity.GetType();
        
        if (type.GetProperty("UsuarioAtualizacao") != null)
            type.GetProperty("UsuarioAtualizacao")?.SetValue(entity, usuarioAtualizacao);
            
        if (type.GetProperty("DataAtualizacao") != null)
            type.GetProperty("DataAtualizacao")?.SetValue(entity, DateTime.UtcNow);
    }

    protected long GetEntityId(object entity)
    {
        var idProperty = entity.GetType().GetProperty("Id");
        if (idProperty != null)
            return Convert.ToInt64(idProperty.GetValue(entity));
        throw new InvalidOperationException("Entidade não possui propriedade Id");
    }
}

