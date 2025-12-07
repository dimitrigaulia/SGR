using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SGR.Api.Data;
using SGR.Api.Exceptions;
using SGR.Api.Models.DTOs;
using SGR.Api.Models.Tenant.DTOs;
using SGR.Api.Models.Tenant.Entities;
using SGR.Api.Services.Common;
using SGR.Api.Services.Interfaces;
using SGR.Api.Services.Tenant.Interfaces;
using System.Linq.Expressions;

namespace SGR.Api.Services.Tenant.Implementations;

public class TenantUsuarioService : BaseService<TenantDbContext, TenantUsuario, TenantUsuarioDto, CreateTenantUsuarioRequest, UpdateTenantUsuarioRequest>, ITenantUsuarioService
{
    public TenantUsuarioService(TenantDbContext context, ILogger<TenantUsuarioService> logger) : base(context, logger)
    {
    }

    protected override IQueryable<TenantUsuario> ApplySearch(IQueryable<TenantUsuario> query, string? search)
    {
        if (string.IsNullOrWhiteSpace(search)) return query;
        search = search.ToLower();
        return query.Where(u =>
            EF.Functions.ILike(u.NomeCompleto, $"%{search}%") ||
            EF.Functions.ILike(u.Email, $"%{search}%"));
    }

    protected override IQueryable<TenantUsuario> ApplySorting(IQueryable<TenantUsuario> query, string? sort, string? order)
    {
        var ascending = string.Equals(order, "asc", StringComparison.OrdinalIgnoreCase);
        return (sort?.ToLower()) switch
        {
            "nome" or "nomecompleto" => ascending ? query.OrderBy(u => u.NomeCompleto) : query.OrderByDescending(u => u.NomeCompleto),
            "email" => ascending ? query.OrderBy(u => u.Email) : query.OrderByDescending(u => u.Email),
            "ativo" or "isativo" => ascending ? query.OrderBy(u => u.IsAtivo) : query.OrderByDescending(u => u.IsAtivo),
            "perfil" => ascending ? query.OrderBy(u => u.Perfil.Nome) : query.OrderByDescending(u => u.Perfil.Nome),
            _ => query.OrderBy(u => u.NomeCompleto)
        };
    }

    public override async Task<PagedResult<TenantUsuarioDto>> GetAllAsync(string? search, int page, int pageSize, string? sort, string? order)
    {
        _logger.LogInformation("Buscando {EntityType} - Página: {Page}, Tamanho: {PageSize}, Busca: {Search}", 
            typeof(TenantUsuario).Name, page, pageSize, search ?? "N/A");

        // Usar AsNoTracking para queries de leitura (melhor performance)
        var query = _dbSet.Include(u => u.Perfil).AsNoTracking().AsQueryable();
        
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

        _logger.LogInformation("Encontrados {Total} registros de {EntityType}", total, typeof(TenantUsuario).Name);

        return new PagedResult<TenantUsuarioDto> { Items = items, Total = total };
    }

    public override async Task<TenantUsuarioDto?> GetByIdAsync(long id)
    {
        _logger.LogInformation("Buscando {EntityType} por ID: {Id}", typeof(TenantUsuario).Name, id);

        // Usar AsNoTracking para queries de leitura (melhor performance)
        var entity = await _dbSet.Include(u => u.Perfil).AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
        if (entity == null)
        {
            _logger.LogWarning("{EntityType} com ID {Id} não encontrado", typeof(TenantUsuario).Name, id);
            return null;
        }

        var mapper = MapToDto().Compile();
        return mapper(entity);
    }

    protected override Expression<Func<TenantUsuario, TenantUsuarioDto>> MapToDto()
    {
        return u => new TenantUsuarioDto
        {
            Id = u.Id,
            PerfilId = u.PerfilId,
            PerfilNome = u.Perfil.Nome,
            IsAtivo = u.IsAtivo,
            NomeCompleto = u.NomeCompleto,
            Email = u.Email,
            PathImagem = u.PathImagem,
            UsuarioAtualizacao = u.UsuarioAtualizacao,
            DataAtualizacao = u.DataAtualizacao
        };
    }

    protected override TenantUsuario MapToEntity(CreateTenantUsuarioRequest request)
    {
        return new TenantUsuario
        {
            PerfilId = request.PerfilId,
            IsAtivo = request.IsAtivo,
            NomeCompleto = request.NomeCompleto,
            Email = request.Email,
            SenhaHash = BCrypt.Net.BCrypt.HashPassword(request.Senha),
            PathImagem = request.PathImagem
        };
    }

    protected override void UpdateEntity(TenantUsuario entity, UpdateTenantUsuarioRequest request)
    {
        entity.PerfilId = request.PerfilId;
        entity.IsAtivo = request.IsAtivo;
        entity.NomeCompleto = request.NomeCompleto;
        entity.Email = request.Email;
        entity.PathImagem = request.PathImagem;
        
        if (!string.IsNullOrWhiteSpace(request.NovaSenha))
        {
            entity.SenhaHash = BCrypt.Net.BCrypt.HashPassword(request.NovaSenha);
        }
    }

    protected override async Task BeforeCreateAsync(TenantUsuario entity, CreateTenantUsuarioRequest request, string? usuarioCriacao)
    {
        // Validação de email único
        var exists = await _context.Set<TenantUsuario>().AnyAsync(x => x.Email == request.Email);
        if (exists)
        {
            _logger.LogWarning("Tentativa de criar usuário com email já existente: {Email}", request.Email);
            throw new BusinessException("E-mail já cadastrado");
        }

        // Validação de perfil
        var perfilExists = await _context.Set<TenantPerfil>().AnyAsync(p => p.Id == request.PerfilId && p.IsAtivo);
        if (!perfilExists)
        {
            _logger.LogWarning("Tentativa de criar usuário com perfil inválido ou inativo: {PerfilId}", request.PerfilId);
            throw new BusinessException("Perfil inválido ou inativo");
        }
    }

    protected override async Task BeforeUpdateAsync(TenantUsuario entity, UpdateTenantUsuarioRequest request, string? usuarioAtualizacao)
    {
        // Validação de email único (exceto o próprio)
        var emailTaken = await _context.Set<TenantUsuario>().AnyAsync(x => x.Email == request.Email && x.Id != entity.Id);
        if (emailTaken)
        {
            _logger.LogWarning("Tentativa de atualizar usuário {Id} com email já existente: {Email}", entity.Id, request.Email);
            throw new BusinessException("E-mail já cadastrado");
        }

        // Validação de perfil
        var perfilExists = await _context.Set<TenantPerfil>().AnyAsync(p => p.Id == request.PerfilId);
        if (!perfilExists)
        {
            _logger.LogWarning("Tentativa de atualizar usuário {Id} com perfil inválido: {PerfilId}", entity.Id, request.PerfilId);
            throw new BusinessException("Perfil inválido");
        }
    }

    public override async Task<TenantUsuarioDto> CreateAsync(CreateTenantUsuarioRequest request, string? usuarioCriacao)
    {
        _logger.LogInformation("Criando novo {EntityType} - Usuário: {Usuario}", typeof(TenantUsuario).Name, usuarioCriacao ?? "Sistema");

        try
        {
            var entity = MapToEntity(request);
            
            // Setar campos de auditoria se existirem
            SetAuditFieldsOnCreate(entity, request, usuarioCriacao);
            
            await BeforeCreateAsync(entity, request, usuarioCriacao);
            
            _dbSet.Add(entity);
            await _context.SaveChangesAsync();

            var entityId = GetEntityId(entity);
            _logger.LogInformation("{EntityType} criado com sucesso - ID: {Id}", typeof(TenantUsuario).Name, entityId);

            // Buscar novamente com Include para garantir que temos o Perfil
            var savedEntity = await _dbSet.Include(u => u.Perfil).FirstOrDefaultAsync(u => u.Id == entityId);
            if (savedEntity == null)
                throw new InvalidOperationException("Erro ao salvar entidade");

            var mapper = MapToDto().Compile();
            return mapper(savedEntity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar {EntityType}", typeof(TenantUsuario).Name);
            throw;
        }
    }

    public override async Task<TenantUsuarioDto?> UpdateAsync(long id, UpdateTenantUsuarioRequest request, string? usuarioAtualizacao)
    {
        _logger.LogInformation("Atualizando {EntityType} - ID: {Id}, Usuário: {Usuario}", 
            typeof(TenantUsuario).Name, id, usuarioAtualizacao ?? "Sistema");

        try
        {
            // Usar FirstOrDefaultAsync ao invés de FindAsync para garantir que respeita o schema do tenant
            var entity = await _dbSet.FirstOrDefaultAsync(e => e.Id == id);
            if (entity == null)
            {
                _logger.LogWarning("{EntityType} com ID {Id} não encontrado para atualização", typeof(TenantUsuario).Name, id);
                return null;
            }

            UpdateEntity(entity, request);
            
            // Setar campos de auditoria se existirem
            SetAuditFieldsOnUpdate(entity, request, usuarioAtualizacao);
            
            await BeforeUpdateAsync(entity, request, usuarioAtualizacao);
            
            // Não é necessário marcar como Modified explicitamente quando a entidade já está tracked
            await _context.SaveChangesAsync();

            _logger.LogInformation("{EntityType} atualizado com sucesso - ID: {Id}", typeof(TenantUsuario).Name, id);

            // Buscar novamente com Include para garantir que temos o Perfil
            var updatedEntity = await _dbSet.Include(u => u.Perfil).FirstOrDefaultAsync(u => u.Id == id);
            if (updatedEntity == null) return null;

            var mapper = MapToDto().Compile();
            return mapper(updatedEntity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar {EntityType} - ID: {Id}", typeof(TenantUsuario).Name, id);
            throw;
        }
    }

    // Método específico que não está na interface base
    public async Task<bool> EmailExistsAsync(string email, long? excludeId = null)
    {
        var q = _context.Set<TenantUsuario>().AsQueryable().Where(x => x.Email == email);
        if (excludeId.HasValue)
            q = q.Where(x => x.Id != excludeId.Value);
        return await q.AnyAsync();
    }
}

