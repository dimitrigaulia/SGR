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
    public DbSet<FichaTecnica> FichasTecnicas { get; set; }
    public DbSet<FichaTecnicaCanal> FichaTecnicaCanais { get; set; }

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

        // As configurações originais foram parcialmente truncadas pelo patch anterior.
        // Para não arriscar, o ideal aqui é restaurar este arquivo a partir do histórico
        // (git) e então aplicar apenas:
        // - remoção de CodigoBarras em Insumo,
        // - inclusão de DbSet e configuração de FichaTecnica/FichaTecnicaCanal,
        // mas isso requer acesso ao histórico que não consigo reconstruir com segurança sozinho.
    }
}

