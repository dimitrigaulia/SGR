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
    public DbSet<CategoriaReceita> CategoriasReceita { get; set; }
    public DbSet<Receita> Receitas { get; set; }
    public DbSet<ReceitaItem> ReceitaItens { get; set; }

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
            entity.Property(e => e.IsAtivo).IsRequired();
            entity.Property(e => e.UsuarioCriacao).HasMaxLength(100);
            entity.Property(e => e.UsuarioAtualizacao).HasMaxLength(100);
            entity.Property(e => e.DataCriacao).IsRequired();
            entity.Property(e => e.DataAtualizacao);
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
            entity.Property(e => e.IsAtivo).IsRequired();
            entity.Property(e => e.UsuarioCriacao).HasMaxLength(100);
            entity.Property(e => e.DataCriacao).IsRequired();
            entity.Property(e => e.UsuarioAtualizacao).HasMaxLength(100);
            entity.Property(e => e.DataAtualizacao);

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
            entity.Property(e => e.IsAtivo).IsRequired();
            entity.Property(e => e.UsuarioCriacao).HasMaxLength(100);
            entity.Property(e => e.UsuarioAtualizacao).HasMaxLength(100);
            entity.Property(e => e.DataCriacao).IsRequired();
            entity.Property(e => e.DataAtualizacao);

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
            entity.Property(e => e.FatorConversaoBase).HasPrecision(18, 6);
            entity.Property(e => e.IsAtivo).IsRequired();
            entity.Property(e => e.UsuarioCriacao).HasMaxLength(100);
            entity.Property(e => e.UsuarioAtualizacao).HasMaxLength(100);
            entity.Property(e => e.DataCriacao).IsRequired();
            entity.Property(e => e.DataAtualizacao);

            // Índices únicos (dentro do schema do tenant)
            entity.HasIndex(e => e.Nome).IsUnique();
            entity.HasIndex(e => e.Sigla).IsUnique();

            // Relacionamento com unidade base (self-referencing)
            entity.HasOne(e => e.UnidadeBase)
                  .WithMany()
                  .HasForeignKey(e => e.UnidadeBaseId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuração Insumo
        modelBuilder.Entity<Insumo>(entity =>
        {
            entity.ToTable("Insumo");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Nome).HasMaxLength(200).IsRequired();
            entity.Property(e => e.QuantidadePorEmbalagem).HasPrecision(18, 4).IsRequired();
            entity.Property(e => e.CustoUnitario).HasPrecision(18, 4).IsRequired();
            entity.Property(e => e.FatorCorrecao).HasPrecision(18, 4).IsRequired().HasDefaultValue(1.0m);
            entity.Property(e => e.Descricao);
            entity.Property(e => e.CodigoBarras).HasMaxLength(50);
            entity.Property(e => e.PathImagem).HasMaxLength(500);
            entity.Property(e => e.IsAtivo).IsRequired();
            entity.Property(e => e.UsuarioCriacao).HasMaxLength(100);
            entity.Property(e => e.UsuarioAtualizacao).HasMaxLength(100);
            entity.Property(e => e.DataCriacao).IsRequired();
            entity.Property(e => e.DataAtualizacao);

            // Índice único para Código de Barras (dentro do schema do tenant, se informado)
            entity.HasIndex(e => e.CodigoBarras).IsUnique().HasFilter("\"CodigoBarras\" IS NOT NULL");

            // Relacionamentos
            entity.HasOne(e => e.Categoria)
                  .WithMany(c => c.Insumos)
                  .HasForeignKey(e => e.CategoriaId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.UnidadeCompra)
                  .WithMany()
                  .HasForeignKey(e => e.UnidadeCompraId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.UnidadeUso)
                  .WithMany(u => u.Insumos)
                  .HasForeignKey(e => e.UnidadeUsoId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuração CategoriaReceita
        modelBuilder.Entity<CategoriaReceita>(entity =>
        {
            entity.ToTable("CategoriaReceita");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Nome).HasMaxLength(100).IsRequired();
            entity.Property(e => e.IsAtivo).IsRequired();
            entity.Property(e => e.UsuarioCriacao).HasMaxLength(100);
            entity.Property(e => e.UsuarioAtualizacao).HasMaxLength(100);
            entity.Property(e => e.DataCriacao).IsRequired();
            entity.Property(e => e.DataAtualizacao);

            // Índice único para Nome (dentro do schema do tenant)
            entity.HasIndex(e => e.Nome).IsUnique();
        });

        // Configuração Receita
        modelBuilder.Entity<Receita>(entity =>
        {
            entity.ToTable("Receita");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Nome).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Descricao).HasMaxLength(5000);
            entity.Property(e => e.InstrucoesEmpratamento).HasMaxLength(2000);
            entity.Property(e => e.Rendimento).HasPrecision(18, 4).IsRequired();
            entity.Property(e => e.PesoPorPorcao).HasPrecision(18, 4);
            entity.Property(e => e.ToleranciaPeso).HasPrecision(18, 4);
            entity.Property(e => e.FatorRendimento).HasPrecision(18, 4).IsRequired().HasDefaultValue(1.0m);
            entity.Property(e => e.TempoPreparo);
            entity.Property(e => e.Versao).HasMaxLength(20).HasDefaultValue("1.0");
            entity.Property(e => e.CustoTotal).HasPrecision(18, 4).IsRequired().HasDefaultValue(0m);
            entity.Property(e => e.CustoPorPorcao).HasPrecision(18, 4).IsRequired().HasDefaultValue(0m);
            entity.Property(e => e.PathImagem).HasMaxLength(500);
            entity.Property(e => e.IsAtivo).IsRequired();
            entity.Property(e => e.UsuarioCriacao).HasMaxLength(100);
            entity.Property(e => e.UsuarioAtualizacao).HasMaxLength(100);
            entity.Property(e => e.DataCriacao).IsRequired();
            entity.Property(e => e.DataAtualizacao);

            // Relacionamento
            entity.HasOne(e => e.Categoria)
                  .WithMany(c => c.Receitas)
                  .HasForeignKey(e => e.CategoriaId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuração ReceitaItem
        modelBuilder.Entity<ReceitaItem>(entity =>
        {
            entity.ToTable("ReceitaItem");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Quantidade).HasPrecision(18, 4).IsRequired();
            entity.Property(e => e.Ordem).IsRequired();
            entity.Property(e => e.Observacoes).HasMaxLength(500);

            // Relacionamentos
            entity.HasOne(e => e.Receita)
                  .WithMany(r => r.Itens)
                  .HasForeignKey(e => e.ReceitaId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Insumo)
                  .WithMany()
                  .HasForeignKey(e => e.InsumoId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Índice composto para garantir ordem única por receita
            entity.HasIndex(e => new { e.ReceitaId, e.Ordem });
        });
    }

}

