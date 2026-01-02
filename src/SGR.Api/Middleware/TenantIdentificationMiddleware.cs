using SGR.Api.Data;
using SGR.Api.Services.Interfaces;

namespace SGR.Api.Middleware;

/// <summary>
/// Middleware para identificar o tenant baseado no subdomínio (produção) ou header (desenvolvimento)
/// </summary>
public class TenantIdentificationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantIdentificationMiddleware> _logger;

    public TenantIdentificationMiddleware(RequestDelegate next, ILogger<TenantIdentificationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ApplicationDbContext configContext,
        TenantDbContext tenantContext,
        ITenantService tenantService)
    {
        // Ignorar rotas do backoffice e health checks
        var path = context.Request.Path.Value?.ToLower() ?? "";
        if (path.StartsWith("/api/backoffice") || 
            path.StartsWith("/health") || 
            path.StartsWith("/swagger") ||
            path.StartsWith("/openapi"))
        {
            await _next(context);
            return;
        }

        // Verificar se é uma requisição de impersonação do backoffice
        bool isBackofficeImpersonation = context.Request.Headers.TryGetValue("X-Backoffice-Impersonation", out var impersonationHeader) &&
                                         impersonationHeader.ToString().ToLower() == "true";

        string? subdomain = null;

        // Em produção: extrair subdomínio do header Host
        // Em desenvolvimento: ler do header X-Tenant-Subdomain
        if (context.Request.Headers.TryGetValue("X-Tenant-Subdomain", out var headerSubdomain))
        {
            subdomain = headerSubdomain.ToString().ToLower();
            _logger.LogDebug("Tenant identificado via header X-Tenant-Subdomain: {Subdomain}", subdomain);
        }
        else
        {
            // Tentar extrair do Host (produção)
            var host = context.Request.Host.Host;
            if (!string.IsNullOrEmpty(host))
            {
                // Assumindo formato: subdomain.dominio.com
                var parts = host.Split('.');
                if (parts.Length >= 2)
                {
                    subdomain = parts[0].ToLower();
                    _logger.LogDebug("Tenant identificado via Host: {Subdomain}", subdomain);
                }
            }
        }

        // Para login do tenant, permitir continuar mesmo sem subdomain (o controller tratará o erro)
        // Para outras rotas, bloquear se não tiver subdomain (exceto se for impersonação do backoffice)
        if (string.IsNullOrEmpty(subdomain))
        {
            if (path.StartsWith("/api/tenant/auth/login"))
            {
                // Login do tenant sem subdomain - deixar o controller tratar
                await _next(context);
                return;
            }
            
            // Se for impersonação do backoffice, permitir continuar (o controller validará o token)
            if (isBackofficeImpersonation)
            {
                _logger.LogDebug("Requisição de impersonação do backoffice sem subdomain - será validada no controller");
                await _next(context);
                return;
            }
            
            _logger.LogWarning("Não foi possível identificar o tenant. Path: {Path}", path);
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("Tenant não identificado. Forneça o header X-Tenant-Subdomain ou acesse via subdomínio.");
            return;
        }

        // Buscar tenant no banco sgr_config
        var tenant = await tenantService.GetBySubdomainAsync(subdomain);
        if (tenant == null || !tenant.IsAtivo)
        {
            _logger.LogWarning("Tenant não encontrado ou inativo: {Subdomain}", subdomain);
            context.Response.StatusCode = 404;
            await context.Response.WriteAsync($"Tenant '{subdomain}' não encontrado ou inativo.");
            return;
        }

        // Configurar o TenantDbContext para usar o schema do tenant
        tenantContext.SetSchema(tenant.NomeSchema);
        
        // Armazenar informações do tenant no HttpContext para uso posterior
        context.Items["Tenant"] = tenant;
        context.Items["TenantSchema"] = tenant.NomeSchema;
        
        // Marcar se é impersonação do backoffice
        if (isBackofficeImpersonation)
        {
            context.Items["IsBackofficeImpersonation"] = true;
            _logger.LogInformation("Impersonação do backoffice detectada para tenant: {Subdomain}", subdomain);
        }

        _logger.LogDebug("Tenant configurado: {Subdomain} -> Schema: {Schema}", subdomain, tenant.NomeSchema);

        await _next(context);
    }
}

