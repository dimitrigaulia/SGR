using Microsoft.EntityFrameworkCore;
using SGR.Api.Data;
using SGR.Api.Services.Backoffice.Implementations;
using SGR.Api.Services.Backoffice.Interfaces;
using SGR.Api.Services.Common;
using SGR.Api.Services.Implementations;
using SGR.Api.Services.Interfaces;
using SGR.Api.Services.Tenant.Implementations;
using SGR.Api.Services.Tenant.Interfaces;

namespace SGR.Api.Extensions;

/// <summary>
/// Extension methods para configuraÃ§Ã£o de serviÃ§os
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adiciona os serviÃ§os da aplicaÃ§Ã£o ao container de DI
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Auth Services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITenantAuthService, TenantAuthService>();
        
        // Backoffice Services
        services.AddScoped<IBackofficeUsuarioService, BackofficeUsuarioService>();
        services.AddScoped<IBackofficePerfilService, BackofficePerfilService>();
        
        // Tenant Services
        services.AddScoped<ITenantUsuarioService, TenantUsuarioService>();
        services.AddScoped<ITenantPerfilService, TenantPerfilService>();
        services.AddScoped<ICategoriaInsumoService, CategoriaInsumoService>();
        services.AddScoped<IUnidadeMedidaService, UnidadeMedidaService>();
        services.AddScoped<IInsumoService, InsumoService>();
        services.AddScoped<ICategoriaReceitaService, CategoriaReceitaService>();
        services.AddScoped<IReceitaService, ReceitaService>();
        services.AddScoped<IFichaTecnicaService, FichaTecnicaService>();
        
        // Common Services
        services.AddScoped<ITenantService, TenantService>();
        services.AddScoped<PdfService>();
        
        // HTTP Client para validaÃ§Ã£o de CPF/CNPJ
        services.AddHttpClient<ICpfCnpjValidationService, CpfCnpjValidationService>();
        
        // HTTP Client para busca de dados do CNPJ
        services.AddHttpClient<ICnpjDataService, CnpjDataService>();

        return services;
    }

    /// <summary>
    /// Configura o Entity Framework Core
    /// </summary>
    public static IServiceCollection AddApplicationDbContext(this IServiceCollection services, IConfiguration configuration)
    {
        var configConnectionString = configuration.GetConnectionString("ConfigConnection");
        var isDevelopment = configuration.GetValue<bool>("ASPNETCORE_ENVIRONMENT") == "Development" 
            || Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
        
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(configConnectionString);
            
            // ConfiguraÃ§Ãµes recomendadas da Microsoft
            if (isDevelopment)
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
            
            // Manter tracking como padrÃ£o para operaÃ§Ãµes de escrita
            // Usar AsNoTracking() explicitamente em queries de leitura
        });

        var tenantsConnectionString = configuration.GetConnectionString("TenantsConnection");
        
        // Adicionar HttpContextAccessor se ainda nÃ£o estiver registrado
        services.AddHttpContextAccessor();
        
        // Registrar o interceptor como singleton (nÃ£o precisa de estado, apenas do HttpContextAccessor)
        services.AddSingleton<TenantSchemaInterceptor>();
        
        // Registrar o DbContext com o interceptor
        services.AddDbContext<TenantDbContext>((serviceProvider, options) =>
        {
            options.UseNpgsql(tenantsConnectionString);
            
            // ConfiguraÃ§Ãµes recomendadas da Microsoft
            if (isDevelopment)
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
            
            // Obter o interceptor do service provider
            var interceptor = serviceProvider.GetRequiredService<TenantSchemaInterceptor>();
            options.AddInterceptors(interceptor);
        });

        return services;
    }

    /// <summary>
    /// Configura CORS
    /// </summary>
    public static IServiceCollection AddApplicationCors(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
            ?? new[] { "http://localhost:4200", "https://localhost:4200" };

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            });
        });

        return services;
    }

    /// <summary>
    /// Adiciona health checks
    /// </summary>
    public static IServiceCollection AddApplicationHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ConfigConnection");
        
        services.AddHealthChecks()
            .AddDbContextCheck<ApplicationDbContext>(name: "database");

        return services;
    }
}

