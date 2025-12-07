using Microsoft.EntityFrameworkCore;
using SGR.Api.Models.Backoffice.Entities;
using SGR.Api.Models.Entities;

namespace SGR.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Backoffice entities
    public DbSet<BackofficePerfil> BackofficePerfis { get; set; }
    public DbSet<BackofficeUsuario> BackofficeUsuarios { get; set; }
    
    // Config entities
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<CategoriaTenant> CategoriaTenants { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuração BackofficePerfil
        modelBuilder.Entity<BackofficePerfil>(entity =>
        {
            entity.ToTable("BackofficePerfil");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Nome).HasMaxLength(100).IsRequired();
            entity.Property(e => e.IsAtivo).IsRequired();
            entity.Property(e => e.UsuarioCriacao).HasMaxLength(100);
            entity.Property(e => e.UsuarioAtualizacao).HasMaxLength(100);
            entity.Property(e => e.DataCriacao).IsRequired();
            entity.Property(e => e.DataAtualizacao);
        });

        // Configuração BackofficeUsuario
        modelBuilder.Entity<BackofficeUsuario>(entity =>
        {
            entity.ToTable("BackofficeUsuario");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.NomeCompleto).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(200).IsRequired();
            entity.Property(e => e.SenhaHash).HasMaxLength(500).IsRequired();
            entity.Property(e => e.PathImagem).HasMaxLength(500);
            entity.Property(e => e.IsAtivo).IsRequired();
            entity.Property(e => e.UsuarioCriacao).HasMaxLength(100);
            entity.Property(e => e.DataCriacao).IsRequired();
            entity.Property(e => e.UsuarioAtualizacao).HasMaxLength(100);
            entity.Property(e => e.DataAtualizacao);

            // Índice único para Email
            entity.HasIndex(e => e.Email).IsUnique();

            // Relacionamento
            entity.HasOne(e => e.Perfil)
                  .WithMany(p => p.Usuarios)
                  .HasForeignKey(e => e.PerfilId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuração CategoriaTenant
        modelBuilder.Entity<CategoriaTenant>(entity =>
        {
            entity.ToTable("CategoriaTenant");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Nome).HasMaxLength(100).IsRequired();
            entity.Property(e => e.IsAtivo).IsRequired();
            entity.Property(e => e.UsuarioCriacao).HasMaxLength(100);
            entity.Property(e => e.UsuarioAtualizacao).HasMaxLength(100);
            entity.Property(e => e.DataCriacao).IsRequired();
            entity.Property(e => e.DataAtualizacao);

            // Índice único para Nome
            entity.HasIndex(e => e.Nome).IsUnique();
        });

        // Configuração Tenant
        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.ToTable("Tenant");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.RazaoSocial).HasMaxLength(200).IsRequired();
            entity.Property(e => e.NomeFantasia).HasMaxLength(200).IsRequired();
            entity.Property(e => e.TipoPessoaId).IsRequired();
            entity.Property(e => e.CpfCnpj).HasMaxLength(18).IsRequired();
            entity.Property(e => e.Subdominio).HasMaxLength(50).IsRequired();
            entity.Property(e => e.NomeSchema).HasMaxLength(100).IsRequired();
            entity.Property(e => e.CategoriaId).IsRequired();
            entity.Property(e => e.FatorContabil).HasPrecision(18, 4).IsRequired();
            entity.Property(e => e.IsAtivo).IsRequired();
            entity.Property(e => e.UsuarioCriacao).HasMaxLength(100);
            entity.Property(e => e.UsuarioAtualizacao).HasMaxLength(100);
            entity.Property(e => e.DataCriacao).IsRequired();
            entity.Property(e => e.DataAtualizacao);

            // Índices únicos
            entity.HasIndex(e => e.Subdominio).IsUnique();
            entity.HasIndex(e => e.CpfCnpj).IsUnique();
            entity.HasIndex(e => e.NomeSchema).IsUnique();

            // Relacionamento com CategoriaTenant
            entity.HasOne(e => e.Categoria)
                  .WithMany()
                  .HasForeignKey(e => e.CategoriaId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}




