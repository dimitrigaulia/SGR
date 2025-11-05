using Microsoft.EntityFrameworkCore;
using SGR.Api.Models.Entities;

namespace SGR.Api.Data;

public static class DbInitializer
{
    public static void Initialize(ApplicationDbContext context)
    {
        // Garantir que o banco foi criado
        context.Database.EnsureCreated();

        // Verificar se já existem dados
        if (context.Perfis.Any())
        {
            return; // Banco já foi inicializado
        }

        // Criar perfil Administrador
        var perfilAdmin = new Perfil
        {
            Nome = "Administrador",
            IsAtivo = true,
            UsuarioCriacao = "Sistema",
            DataCriacao = DateTime.UtcNow
        };

        context.Perfis.Add(perfilAdmin);
        context.SaveChanges();

        // Criar usuário padrão
        var senhaHash = BCrypt.Net.BCrypt.HashPassword("Dimi@1997");
        var usuario = new Usuario
        {
            NomeCompleto = "Dimitri Gaulia",
            Email = "dimitrifgaulia@gmail.com",
            SenhaHash = senhaHash,
            PerfilId = perfilAdmin.Id,
            IsAtivo = true,
            UsuarioCriacao = "Sistema",
            DataCriacao = DateTime.UtcNow
        };

        context.Usuarios.Add(usuario);
        context.SaveChanges();
    }
}

