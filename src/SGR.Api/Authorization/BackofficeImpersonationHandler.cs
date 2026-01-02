using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace SGR.Api.Authorization;

/// <summary>
/// Handler que verifica se o usuário autenticado é do backoffice e está fazendo impersonação de um tenant
/// </summary>
public class BackofficeImpersonationHandler : AuthorizationHandler<BackofficeImpersonationRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public BackofficeImpersonationHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        BackofficeImpersonationRequirement requirement)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return Task.CompletedTask;
        }

        // Verificar se o header X-Backoffice-Impersonation está presente
        bool hasImpersonationHeader = httpContext.Request.Headers.TryGetValue("X-Backoffice-Impersonation", out var impersonationHeader) &&
                                      impersonationHeader.ToString().ToLower() == "true";

        if (!hasImpersonationHeader)
        {
            return Task.CompletedTask;
        }

        // Verificar se o token tem a claim "Context" = "backoffice"
        var contextClaim = context.User.FindFirst("Context");
        if (contextClaim == null || contextClaim.Value != "backoffice")
        {
            return Task.CompletedTask;
        }

        // Verificar se o tenant foi identificado
        var tenant = httpContext.Items["Tenant"];
        if (tenant == null)
        {
            return Task.CompletedTask;
        }

        // Todas as condições atendidas - permitir acesso
        context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
