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
    public DbSet<CategoriaInsumo> CategoriaInsumos { get; set; }
    public DbSet<UnidadeMedida> UnidadesMedida { get; set; }
    public DbSet<Insumo> Insumos { get; set; }

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

        // Configuração CategoriaInsumo
        modelBuilder.Entity<CategoriaInsumo>(entity =>
        {
            entity.ToTable("CategoriaInsumo");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Nome).HasMaxLength(100).IsRequired();
            entity.Property(e => e.UsuarioCriacao).HasMaxLength(100);
            entity.Property(e => e.UsuarioAtualizacao).HasMaxLength(100);
            entity.Property(e => e.DataCriacao).IsRequired();

            // Índice único para Nome (dentro do schema do tenant)
            entity.HasIndex(e => e.Nome).IsUnique();
        });

        // Configuração UnidadeMedida
        modelBuilder.Entity<UnidadeMedida>(entity =>
        {
            entity.ToTable("UnidadeMedida");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Nome).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Sigla).HasMaxLength(10).IsRequired();
            entity.Property(e => e.Tipo).HasMaxLength(20);
            entity.Property(e => e.UsuarioCriacao).HasMaxLength(100);
            entity.Property(e => e.UsuarioAtualizacao).HasMaxLength(100);
            entity.Property(e => e.DataCriacao).IsRequired();

            // Índices únicos (dentro do schema do tenant)
            entity.HasIndex(e => e.Nome).IsUnique();
            entity.HasIndex(e => e.Sigla).IsUnique();
        });

        // Configuração Insumo
        modelBuilder.Entity<Insumo>(entity =>
        {
            entity.ToTable("Insumo");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Nome).HasMaxLength(200).IsRequired();
            entity.Property(e => e.CustoUnitario).HasPrecision(18, 4).IsRequired();
            entity.Property(e => e.EstoqueMinimo).HasPrecision(18, 4);
            entity.Property(e => e.Descricao);
            entity.Property(e => e.CodigoBarras).HasMaxLength(50);
            entity.Property(e => e.PathImagem).HasMaxLength(500);
            entity.Property(e => e.UsuarioCriacao).HasMaxLength(100);
            entity.Property(e => e.UsuarioAtualizacao).HasMaxLength(100);
            entity.Property(e => e.DataCriacao).IsRequired();

            // Índice único para Código de Barras (dentro do schema do tenant, se informado)
            entity.HasIndex(e => e.CodigoBarras).IsUnique().HasFilter("\"CodigoBarras\" IS NOT NULL");

            // Relacionamentos
            entity.HasOne(e => e.Categoria)
                  .WithMany(c => c.Insumos)
                  .HasForeignKey(e => e.CategoriaId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.UnidadeMedida)
                  .WithMany(u => u.Insumos)
                  .HasForeignKey(e => e.UnidadeMedidaId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }

}

