using System.Linq;
using Microsoft.EntityFrameworkCore;
using SGR.Api.Models.Backoffice.Entities;
using SGR.Api.Models.Entities;

namespace SGR.Api.Data;

public static class DbInitializer
{
    /// <summary>
    /// Inicializa os dados padrão do banco de dados (backoffice)
    /// Deve ser chamado APÓS as migrations serem aplicadas
    /// </summary>
    public static void Initialize(ApplicationDbContext context)
    {
        // Verificar se o banco está acessível e as migrations foram aplicadas
        if (!context.Database.CanConnect())
        {
            return; // Banco não está acessível ou migrations não foram aplicadas
        }

        // Verificar se a tabela BackofficePerfil existe (migrations aplicadas)
        try
        {
            if (!context.Database.GetPendingMigrations().Any())
            {
                // Migrations aplicadas, verificar se já existem dados
                if (context.BackofficePerfis.Any())
                {
                    return; // Banco já foi inicializado
                }
            }
            else
            {
                // Ainda há migrations pendentes, não inicializar dados
                return;
            }
        }
        catch
        {
            // Se houver erro ao verificar (tabela não existe), não inicializar
            return;
        }

        // Criar categorias padrão
        var categorias = new[]
        {
            new CategoriaTenant { Nome = "Alimentos", IsAtivo = true, UsuarioCriacao = "Sistema", DataCriacao = DateTime.UtcNow },
            new CategoriaTenant { Nome = "Bebidas", IsAtivo = true, UsuarioCriacao = "Sistema", DataCriacao = DateTime.UtcNow },
            new CategoriaTenant { Nome = "Outros", IsAtivo = true, UsuarioCriacao = "Sistema", DataCriacao = DateTime.UtcNow }
        };

        foreach (var categoria in categorias)
        {
            if (!context.CategoriaTenants.Any(c => c.Nome == categoria.Nome))
            {
                context.CategoriaTenants.Add(categoria);
            }
        }
        context.SaveChanges();

        // Criar perfil Administrador do backoffice
        var perfilAdmin = new BackofficePerfil
        {
            Nome = "Administrador",
            IsAtivo = true,
            UsuarioCriacao = "Sistema",
            DataCriacao = DateTime.UtcNow
        };

        context.BackofficePerfis.Add(perfilAdmin);
        context.SaveChanges();

        // Criar usuário padrão do backoffice
        var senhaHash = BCrypt.Net.BCrypt.HashPassword("Dimi@1997");
        var usuario = new BackofficeUsuario
        {
            NomeCompleto = "Dimitri Gaulia",
            Email = "dimitrifgaulia@gmail.com",
            SenhaHash = senhaHash,
            PerfilId = perfilAdmin.Id,
            IsAtivo = true,
            UsuarioCriacao = "Sistema",
            DataCriacao = DateTime.UtcNow
        };

        context.BackofficeUsuarios.Add(usuario);
        context.SaveChanges();
    }
}

