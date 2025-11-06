using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SGR.Api.Data;
using SGR.Api.Exceptions;
using SGR.Api.Models.DTOs;
using SGR.Api.Models.Entities;
using SGR.Api.Services.Interfaces;
using System.Linq.Expressions;

namespace SGR.Api.Services.Implementations;

public class UsuarioService : BaseService<Usuario, UsuarioDto, CreateUsuarioRequest, UpdateUsuarioRequest>, IUsuarioService
{
    public UsuarioService(ApplicationDbContext context, ILogger<UsuarioService> logger) : base(context, logger)
    {
    }

    protected override IQueryable<Usuario> ApplySearch(IQueryable<Usuario> query, string? search)
    {
        if (string.IsNullOrWhiteSpace(search)) return query;
        search = search.ToLower();
        return query.Where(u =>
            EF.Functions.ILike(u.NomeCompleto, $"%{search}%") ||
            EF.Functions.ILike(u.Email, $"%{search}%"));
    }

    protected override IQueryable<Usuario> ApplySorting(IQueryable<Usuario> query, string? sort, string? order)
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

    public override async Task<PagedResult<UsuarioDto>> GetAllAsync(string? search, int page, int pageSize, string? sort, string? order)
    {
        _logger.LogInformation("Buscando {EntityType} - Página: {Page}, Tamanho: {PageSize}, Busca: {Search}", 
            typeof(Usuario).Name, page, pageSize, search ?? "N/A");

        var query = _dbSet.Include(u => u.Perfil).AsQueryable();
        
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

        _logger.LogInformation("Encontrados {Total} registros de {EntityType}", total, typeof(Usuario).Name);

        return new PagedResult<UsuarioDto> { Items = items, Total = total };
    }

    public override async Task<UsuarioDto?> GetByIdAsync(long id)
    {
        _logger.LogInformation("Buscando {EntityType} por ID: {Id}", typeof(Usuario).Name, id);

        var entity = await _dbSet.Include(u => u.Perfil).FirstOrDefaultAsync(u => u.Id == id);
        if (entity == null)
        {
            _logger.LogWarning("{EntityType} com ID {Id} não encontrado", typeof(Usuario).Name, id);
            return null;
        }

        var mapper = MapToDto().Compile();
        return mapper(entity);
    }

    protected override Expression<Func<Usuario, UsuarioDto>> MapToDto()
    {
        return u => new UsuarioDto
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

    protected override Usuario MapToEntity(CreateUsuarioRequest request)
    {
        return new Usuario
        {
            PerfilId = request.PerfilId,
            IsAtivo = request.IsAtivo,
            NomeCompleto = request.NomeCompleto,
            Email = request.Email,
            SenhaHash = BCrypt.Net.BCrypt.HashPassword(request.Senha),
            PathImagem = request.PathImagem
        };
    }

    protected override void UpdateEntity(Usuario entity, UpdateUsuarioRequest request)
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

    protected override async Task BeforeCreateAsync(Usuario entity, CreateUsuarioRequest request, string? usuarioCriacao)
    {
        // Validação de email único
        var exists = await _context.Usuarios.AnyAsync(x => x.Email == request.Email);
        if (exists)
        {
            _logger.LogWarning("Tentativa de criar usuário com email já existente: {Email}", request.Email);
            throw new BusinessException("E-mail já cadastrado");
        }

        // Validação de perfil
        var perfilExists = await _context.Perfis.AnyAsync(p => p.Id == request.PerfilId && p.IsAtivo);
        if (!perfilExists)
        {
            _logger.LogWarning("Tentativa de criar usuário com perfil inválido ou inativo: {PerfilId}", request.PerfilId);
            throw new BusinessException("Perfil inválido ou inativo");
        }
    }

    protected override async Task BeforeUpdateAsync(Usuario entity, UpdateUsuarioRequest request, string? usuarioAtualizacao)
    {
        // Validação de email único (exceto o próprio)
        var emailTaken = await _context.Usuarios.AnyAsync(x => x.Email == request.Email && x.Id != entity.Id);
        if (emailTaken)
        {
            _logger.LogWarning("Tentativa de atualizar usuário {Id} com email já existente: {Email}", entity.Id, request.Email);
            throw new BusinessException("E-mail já cadastrado");
        }

        // Validação de perfil
        var perfilExists = await _context.Perfis.AnyAsync(p => p.Id == request.PerfilId);
        if (!perfilExists)
        {
            _logger.LogWarning("Tentativa de atualizar usuário {Id} com perfil inválido: {PerfilId}", entity.Id, request.PerfilId);
            throw new BusinessException("Perfil inválido");
        }
    }

    public override async Task<UsuarioDto> CreateAsync(CreateUsuarioRequest request, string? usuarioCriacao)
    {
        _logger.LogInformation("Criando novo {EntityType} - Usuário: {Usuario}", typeof(Usuario).Name, usuarioCriacao ?? "Sistema");

        try
        {
            var entity = MapToEntity(request);
            
            // Setar campos de auditoria se existirem
            SetAuditFieldsOnCreate(entity, usuarioCriacao);
            
            await BeforeCreateAsync(entity, request, usuarioCriacao);
            
            _dbSet.Add(entity);
            await _context.SaveChangesAsync();

            var entityId = GetEntityId(entity);
            _logger.LogInformation("{EntityType} criado com sucesso - ID: {Id}", typeof(Usuario).Name, entityId);

            // Buscar novamente com Include para garantir que temos o Perfil
            var savedEntity = await _dbSet.Include(u => u.Perfil).FirstOrDefaultAsync(u => u.Id == entityId);
            if (savedEntity == null)
                throw new InvalidOperationException("Erro ao salvar entidade");

            var mapper = MapToDto().Compile();
            return mapper(savedEntity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar {EntityType}", typeof(Usuario).Name);
            throw;
        }
    }

    public override async Task<UsuarioDto?> UpdateAsync(long id, UpdateUsuarioRequest request, string? usuarioAtualizacao)
    {
        _logger.LogInformation("Atualizando {EntityType} - ID: {Id}, Usuário: {Usuario}", 
            typeof(Usuario).Name, id, usuarioAtualizacao ?? "Sistema");

        try
        {
            var entity = await _dbSet.FindAsync(id);
            if (entity == null)
            {
                _logger.LogWarning("{EntityType} com ID {Id} não encontrado para atualização", typeof(Usuario).Name, id);
                return null;
            }

            UpdateEntity(entity, request);
            
            // Setar campos de auditoria se existirem
            SetAuditFieldsOnUpdate(entity, usuarioAtualizacao);
            
            await BeforeUpdateAsync(entity, request, usuarioAtualizacao);
            
            await _context.SaveChangesAsync();

            _logger.LogInformation("{EntityType} atualizado com sucesso - ID: {Id}", typeof(Usuario).Name, id);

            // Buscar novamente com Include para garantir que temos o Perfil
            var updatedEntity = await _dbSet.Include(u => u.Perfil).FirstOrDefaultAsync(u => u.Id == id);
            if (updatedEntity == null) return null;

            var mapper = MapToDto().Compile();
            return mapper(updatedEntity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar {EntityType} - ID: {Id}", typeof(Usuario).Name, id);
            throw;
        }
    }

    // Método específico que não está na interface base
    public async Task<bool> EmailExistsAsync(string email, long? excludeId = null)
    {
        var q = _context.Usuarios.AsQueryable().Where(x => x.Email == email);
        if (excludeId.HasValue)
            q = q.Where(x => x.Id != excludeId.Value);
        return await q.AnyAsync();
    }
}
