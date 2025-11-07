using Microsoft.EntityFrameworkCore;
using SGR.Api.Models.Entities;

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

    public DbSet<TipoPessoa> TipoPessoas { get; set; }
    public DbSet<Perfil> Perfis { get; set; }
    public DbSet<Usuario> Usuarios { get; set; }

    /// <summary>
    /// Define o schema a ser usado para este contexto
    /// </summary>
    public void SetSchema(string schema)
    {
        _schema = schema;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuração TipoPessoa
        modelBuilder.Entity<TipoPessoa>(entity =>
        {
            entity.ToTable("TipoPessoa");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Nome).HasMaxLength(100).IsRequired();
        });

        // Configuração Perfil
        modelBuilder.Entity<Perfil>(entity =>
        {
            entity.ToTable("Perfil");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Nome).HasMaxLength(100).IsRequired();
            entity.Property(e => e.UsuarioCriacao).HasMaxLength(100);
            entity.Property(e => e.UsuarioAtualizacao).HasMaxLength(100);
        });

        // Configuração Usuario
        modelBuilder.Entity<Usuario>(entity =>
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

    public override int SaveChanges()
    {
        ApplySchemaToEntities();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplySchemaToEntities();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void ApplySchemaToEntities()
    {
        if (string.IsNullOrEmpty(_schema))
            return;

        // Aplicar schema a todas as entidades antes de salvar
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is not null)
            {
                var entityType = entry.Entity.GetType();
                var tableName = Model.FindEntityType(entityType)?.GetTableName();
                if (!string.IsNullOrEmpty(tableName))
                {
                    // O schema será aplicado via SQL direto nas queries
                }
            }
        }
    }
}

