using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace SGR.Api.Authorization;

/// <summary>
/// Handler que verifica se o usuário autenticado é do tenant OU do backoffice fazendo impersonação
/// </summary>
public class TenantOrBackofficeImpersonationHandler : AuthorizationHandler<TenantOrBackofficeImpersonationRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantOrBackofficeImpersonationHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TenantOrBackofficeImpersonationRequirement requirement)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return Task.CompletedTask;
        }

        // Verificar se o token tem a claim "Context"
        var contextClaim = context.User.FindFirst("Context");
        
        // Se for token do tenant, permitir acesso
        if (contextClaim != null && contextClaim.Value == "tenant")
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Se for token do backoffice, verificar se está fazendo impersonação
        if (contextClaim != null && contextClaim.Value == "backoffice")
        {
            // Verificar se o header X-Backoffice-Impersonation está presente
            bool hasImpersonationHeader = httpContext.Request.Headers.TryGetValue("X-Backoffice-Impersonation", out var impersonationHeader) &&
                                        impersonationHeader.ToString().ToLower() == "true";

            if (hasImpersonationHeader)
            {
                // Verificar se o tenant foi identificado
                var tenant = httpContext.Items["Tenant"];
                if (tenant != null)
                {
                    context.Succeed(requirement);
                    return Task.CompletedTask;
                }
            }
        }

        return Task.CompletedTask;
    }
}
