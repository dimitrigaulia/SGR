using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SGR.Api.Data;
using SGR.Api.Models.DTOs;
using SGR.Api.Models.Entities;
using SGR.Api.Services.Interfaces;

namespace SGR.Api.Services.Implementations;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthService(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        // Buscar usuário por email
        var usuario = await _context.Usuarios
            .Include(u => u.Perfil)
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsAtivo);

        if (usuario == null)
        {
            return null; // Usuário não encontrado ou inativo
        }

        // Verificar senha
        if (!BCrypt.Net.BCrypt.Verify(request.Senha, usuario.SenhaHash))
        {
            return null; // Senha inválida
        }

        // Verificar se o perfil está ativo
        if (!usuario.Perfil.IsAtivo)
        {
            return null; // Perfil inativo
        }

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

