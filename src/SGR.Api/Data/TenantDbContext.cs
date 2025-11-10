using Microsoft.EntityFrameworkCore;
using SGR.Api.Models.Tenant.Entities;

namespace SGR.Api.Data;

/// <summary>
/// DbContext para o banco sgr_tenants
/// Cada tenant tem seu próprio schema dentro deste banco
/// O schema é definido dinamicamente via SetSchema()
/// </summary>
public class TenantDbContext : DbContext
{
    private string? _schema;

    public TenantDbContext(DbContextOptions<TenantDbContext> options)
        : base(options)
    {
    }

    public DbSet<TenantPerfil> TenantPerfis { get; set; }
    public DbSet<TenantUsuario> TenantUsuarios { get; set; }

    /// <summary>
    /// Define o schema a ser usado para este contexto
    /// </summary>
    public void SetSchema(string schema)
    {
        _schema = schema;
    }

    /// <summary>
    /// Obtém o schema atual configurado
    /// </summary>
    public string? GetSchema() => _schema;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuração TenantPerfil
        modelBuilder.Entity<TenantPerfil>(entity =>
        {
            entity.ToTable("Perfil");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Nome).HasMaxLength(100).IsRequired();
            entity.Property(e => e.UsuarioCriacao).HasMaxLength(100);
            entity.Property(e => e.UsuarioAtualizacao).HasMaxLength(100);
            entity.Property(e => e.DataCriacao).IsRequired();
        });

        // Configuração TenantUsuario
        modelBuilder.Entity<TenantUsuario>(entity =>
        {
            entity.ToTable("Usuario");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.NomeCompleto).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(200).IsRequired();
            entity.Property(e => e.SenhaHash).HasMaxLength(500).IsRequired();
            entity.Property(e => e.PathImagem).HasMaxLength(500);
            entity.Property(e => e.UsuarioCriacao).HasMaxLength(100);
            entity.Property(e => e.DataCriacao).IsRequired();
            entity.Property(e => e.UsuarioAtualizacao).HasMaxLength(100);

            // Índice único para Email (dentro do schema do tenant)
            entity.HasIndex(e => e.Email).IsUnique();

            // Relacionamento
            entity.HasOne(e => e.Perfil)
                  .WithMany(p => p.Usuarios)
                  .HasForeignKey(e => e.PerfilId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }

}

