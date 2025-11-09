using Microsoft.EntityFrameworkCore;
using SGR.Api.Data;
using SGR.Api.Services.Interfaces;
using SGR.Api.Services.Implementations;

namespace SGR.Api.Extensions;

/// <summary>
/// Extension methods para configuração de serviços
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adiciona os serviços da aplicação ao container de DI
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITenantAuthService, TenantAuthService>();
        services.AddScoped<IUsuarioService, UsuarioService>();
        services.AddScoped<IPerfilService, PerfilService>();
        services.AddScoped<ITenantService, TenantService>();
        
        // HTTP Client para validação de CPF/CNPJ
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
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(configConnectionString));

        var tenantsConnectionString = configuration.GetConnectionString("TenantsConnection");
        
        // Adicionar HttpContextAccessor se ainda não estiver registrado
        services.AddHttpContextAccessor();
        
        // Registrar o interceptor como singleton (não precisa de estado, apenas do HttpContextAccessor)
        services.AddSingleton<TenantSchemaInterceptor>();
        
        // Registrar o DbContext com o interceptor
        services.AddDbContext<TenantDbContext>((serviceProvider, options) =>
        {
            options.UseNpgsql(tenantsConnectionString);
            
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

