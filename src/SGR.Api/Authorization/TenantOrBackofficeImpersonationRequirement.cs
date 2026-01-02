using Microsoft.AspNetCore.Authorization;

namespace SGR.Api.Authorization;

/// <summary>
/// Requirement para permitir acesso às rotas do tenant com tokens do tenant ou tokens do backoffice com impersonação
/// </summary>
public class TenantOrBackofficeImpersonationRequirement : IAuthorizationRequirement
{
}
