using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using SGR.Api.Data;
using SGR.Api.Models.DTOs;
using SGR.Api.Models.Entities;
using SGR.Api.Services.Interfaces;

namespace SGR.Api.Services.Implementations;

/// <summary>
/// Service para autenticação de tenants
/// Usa o TenantDbContext que já está configurado com o schema do tenant pelo middleware
/// </summary>
public class TenantAuthService : ITenantAuthService
{
    private readonly TenantDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TenantAuthService> _logger;

    public TenantAuthService(TenantDbContext context, IConfiguration configuration, ILogger<TenantAuthService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        _logger.LogInformation("Tentativa de login do tenant para email: {Email}", request.Email);

        // Buscar usuário por email no schema do tenant
        var usuario = await _context.Usuarios
            .Include(u => u.Perfil)
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsAtivo);

        if (usuario == null)
        {
            _logger.LogWarning("Tentativa de login do tenant falhou - Usuário não encontrado ou inativo: {Email}", request.Email);
            return null;
        }

        // Verificar senha
        if (!BCrypt.Net.BCrypt.Verify(request.Senha, usuario.SenhaHash))
        {
            _logger.LogWarning("Tentativa de login do tenant falhou - Senha inválida para: {Email}", request.Email);
            return null;
        }

        // Verificar se o perfil está ativo
        if (!usuario.Perfil.IsAtivo)
        {
            _logger.LogWarning("Tentativa de login do tenant falhou - Perfil inativo para: {Email}", request.Email);
            return null;
        }

        _logger.LogInformation("Login do tenant bem-sucedido para: {Email} (ID: {Id})", request.Email, usuario.Id);

        // Gerar token JWT
        var token = GenerateJwtToken(usuario);

        // Mapear para DTOs
        var usuarioDto = new UsuarioDto
        {
            Id = usuario.Id,
            PerfilId = usuario.PerfilId,
            IsAtivo = usuario.IsAtivo,
            NomeCompleto = usuario.NomeCompleto,
            Email = usuario.Email,
            PathImagem = usuario.PathImagem,
            UsuarioAtualizacao = usuario.UsuarioAtualizacao,
            DataAtualizacao = usuario.DataAtualizacao
        };

        var perfilDto = new PerfilDto
        {
            Id = usuario.Perfil.Id,
            Nome = usuario.Perfil.Nome,
            IsAtivo = usuario.Perfil.IsAtivo,
            UsuarioCriacao = usuario.Perfil.UsuarioCriacao,
            UsuarioAtualizacao = usuario.Perfil.UsuarioAtualizacao,
            DataCriacao = usuario.Perfil.DataCriacao,
            DataAtualizacao = usuario.Perfil.DataAtualizacao
        };

        return new LoginResponse
        {
            Token = token,
            Usuario = usuarioDto,
            Perfil = perfilDto
        };
    }

    private string GenerateJwtToken(Usuario usuario)
    {
        var secretKey = _configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey não configurada");
        var issuer = _configuration["Jwt:Issuer"] ?? "SGR.Api";
        var audience = _configuration["Jwt:Audience"] ?? "SGR.Frontend";
        var expirationMinutes = int.Parse(_configuration["Jwt:ExpirationMinutes"] ?? "60");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Email, usuario.Email),
            new Claim(ClaimTypes.Name, usuario.NomeCompleto),
            new Claim("PerfilId", usuario.PerfilId.ToString()),
            new Claim("PerfilNome", usuario.Perfil.Nome)
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

