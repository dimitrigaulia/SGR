using Microsoft.EntityFrameworkCore;
using SGR.Api.Models.Tenant.Entities;

namespace SGR.Api.Data
{
    /// <summary>
    /// DbContext para o banco sgr_tenants
    /// Cada tenant tem seu prÃ³prio schema dentro deste banco
    /// O schema Ã© definido dinamicamente via SetSchema()
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
        public DbSet<FichaTecnica> FichasTecnicas { get; set; }
        public DbSet<FichaTecnicaItem> FichaTecnicaItens { get; set; }
        public DbSet<FichaTecnicaCanal> FichaTecnicaCanais { get; set; }

        /// <summary>
        /// Define o schema a ser usado para este contexto
        /// </summary>
        public void SetSchema(string schema)
        {
            _schema = schema;
        }

        /// <summary>
        /// ObtÃ©m o schema atual configurado
        /// </summary>
        public string? GetSchema() => _schema;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ConfiguraÃ§Ã£o do schema dinÃ¢mico
            if (!string.IsNullOrEmpty(_schema))
            {
                modelBuilder.HasDefaultSchema(_schema);
            }

            // TenantPerfil
            modelBuilder.Entity<TenantPerfil>(entity =>
            {
                entity.ToTable("Perfil", _schema);
                entity.HasKey(e => e.Id);
                entity.HasMany(e => e.Usuarios)
                      .WithOne(u => u.Perfil)
                      .HasForeignKey(u => u.PerfilId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // TenantUsuario
            modelBuilder.Entity<TenantUsuario>(entity =>
            {
                entity.ToTable("Usuario", _schema);
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
            });

            // CategoriaInsumo
            modelBuilder.Entity<CategoriaInsumo>(entity =>
            {
                entity.ToTable("CategoriaInsumo", _schema);
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Nome).IsUnique();

                // Relacionamento CategoriaInsumo (1) -> (N) Insumos
                entity.HasMany(c => c.Insumos)
                      .WithOne(i => i.Categoria)
                      .HasForeignKey(i => i.CategoriaId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // CategoriaReceita
            modelBuilder.Entity<CategoriaReceita>(entity =>
            {
                entity.ToTable("CategoriaReceita", _schema);
                entity.HasKey(e => e.Id);

                // Relacionamento CategoriaReceita (1) -> (N) Receitas
                entity.HasMany(c => c.Receitas)
                      .WithOne(r => r.Categoria)
                      .HasForeignKey(r => r.CategoriaId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Insumo
            modelBuilder.Entity<Insumo>(entity =>
            {
                entity.ToTable("Insumo", _schema);
                entity.HasKey(e => e.Id);

                entity.Property(e => e.CategoriaId)
                      .IsRequired();

                // Relacionamento com UnidadeCompra
                entity.HasOne(e => e.UnidadeCompra)
                      .WithMany()
                      .HasForeignKey(e => e.UnidadeCompraId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Relacionamento com UnidadeUso
                entity.HasOne(e => e.UnidadeUso)
                      .WithMany()
                      .HasForeignKey(e => e.UnidadeUsoId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // UnidadeMedida
            modelBuilder.Entity<UnidadeMedida>(entity =>
            {
                entity.ToTable("UnidadeMedida", _schema);
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Nome).IsUnique();
                entity.HasIndex(e => e.Sigla).IsUnique();

                // Como Insumo tem duas navegaÃ§Ãµes para UnidadeMedida (UnidadeCompra e UnidadeUso),
                // ignoramos a coleÃ§Ã£o UnidadeMedida.Insumos para evitar ambiguidades de mapeamento.
                entity.Ignore(e => e.Insumos);
            });

            // Receita
            modelBuilder.Entity<Receita>(entity =>
            {
                entity.ToTable("Receita", _schema);
                entity.HasKey(e => e.Id);

                // Relacionamento com Itens (1 -> N)
                entity.HasMany(e => e.Itens)
                      .WithOne(i => i.Receita)
                      .HasForeignKey(i => i.ReceitaId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ReceitaItem
            modelBuilder.Entity<ReceitaItem>(entity =>
            {
                entity.ToTable("ReceitaItem", _schema);
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.Insumo)
                      .WithMany()
                      .HasForeignKey(e => e.InsumoId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.UnidadeMedida)
                      .WithMany()
                      .HasForeignKey(e => e.UnidadeMedidaId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // FichaTecnica
            modelBuilder.Entity<FichaTecnica>(entity =>
            {
                entity.ToTable("FichaTecnica", _schema);
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.Categoria)
                      .WithMany()
                      .HasForeignKey(e => e.CategoriaId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.ReceitaPrincipal)
                      .WithMany()
                      .HasForeignKey(e => e.ReceitaPrincipalId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.PorcaoVendaUnidadeMedida)
                      .WithMany()
                      .HasForeignKey(e => e.PorcaoVendaUnidadeMedidaId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(e => e.Itens)
                      .WithOne(i => i.FichaTecnica)
                      .HasForeignKey(i => i.FichaTecnicaId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(e => e.Canais)
                      .WithOne(c => c.FichaTecnica)
                      .HasForeignKey(c => c.FichaTecnicaId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // FichaTecnicaItem
            modelBuilder.Entity<FichaTecnicaItem>(entity =>
            {
                entity.ToTable("FichaTecnicaItem", _schema);
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.FichaTecnica)
                      .WithMany(f => f.Itens)
                      .HasForeignKey(e => e.FichaTecnicaId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Receita)
                      .WithMany()
                      .HasForeignKey(e => e.ReceitaId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Insumo)
                      .WithMany()
                      .HasForeignKey(e => e.InsumoId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.UnidadeMedida)
                      .WithMany()
                      .HasForeignKey(e => e.UnidadeMedidaId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.ToTable(t => t.HasCheckConstraint(
                    "CK_FichaTecnicaItem_TipoItem",
                    @"(""TipoItem"" = 'Receita' AND ""ReceitaId"" IS NOT NULL AND ""InsumoId"" IS NULL) OR (""TipoItem"" = 'Insumo' AND ""InsumoId"" IS NOT NULL AND ""ReceitaId"" IS NULL)"));
            });

            // FichaTecnicaCanal
            modelBuilder.Entity<FichaTecnicaCanal>(entity =>
            {
                entity.ToTable("FichaTecnicaCanal", _schema);
                entity.HasKey(e => e.Id);
            });
        }
    }
}
