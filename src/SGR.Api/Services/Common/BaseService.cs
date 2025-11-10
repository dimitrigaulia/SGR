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

        var query = _dbSet.AsQueryable();
        
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

        var entity = await _dbSet.FindAsync(id);
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
            SetAuditFieldsOnCreate(entity, usuarioCriacao);
            
            await BeforeCreateAsync(entity, request, usuarioCriacao);
            
            _dbSet.Add(entity);
            await _context.SaveChangesAsync();

            var entityId = GetEntityId(entity);
            _logger.LogInformation("{EntityType} criado com sucesso - ID: {Id}", typeof(TEntity).Name, entityId);

            // Buscar novamente para garantir que temos os dados atualizados (incluindo ID gerado)
            var savedEntity = await _dbSet.FindAsync(entityId);
            if (savedEntity == null)
                throw new InvalidOperationException("Erro ao salvar entidade");

            var mapper = MapToDto().Compile();
            return mapper(savedEntity);
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
            var entity = await _dbSet.FindAsync(id);
            if (entity == null)
            {
                _logger.LogWarning("{EntityType} com ID {Id} não encontrado para atualização", typeof(TEntity).Name, id);
                return null;
            }

            UpdateEntity(entity, request);
            
            // Setar campos de auditoria se existirem
            SetAuditFieldsOnUpdate(entity, usuarioAtualizacao);
            
            await BeforeUpdateAsync(entity, request, usuarioAtualizacao);
            
            await _context.SaveChangesAsync();

            _logger.LogInformation("{EntityType} atualizado com sucesso - ID: {Id}", typeof(TEntity).Name, id);

            // Buscar novamente para garantir dados atualizados
            var updatedEntity = await _dbSet.FindAsync(id);
            if (updatedEntity == null) return null;

            var mapper = MapToDto().Compile();
            return mapper(updatedEntity);
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
            var entity = await _dbSet.FindAsync(id);
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
    protected void SetAuditFieldsOnCreate(object entity, string? usuarioCriacao)
    {
        var type = entity.GetType();
        
        if (type.GetProperty("UsuarioCriacao") != null)
            type.GetProperty("UsuarioCriacao")?.SetValue(entity, usuarioCriacao);
            
        if (type.GetProperty("DataCriacao") != null)
            type.GetProperty("DataCriacao")?.SetValue(entity, DateTime.UtcNow);
    }

    protected void SetAuditFieldsOnUpdate(object entity, string? usuarioAtualizacao)
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

