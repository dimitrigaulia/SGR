using Microsoft.AspNetCore.Authorization;

namespace SGR.Api.Authorization;

/// <summary>
/// Requirement para permitir que usuários do backoffice acessem rotas do tenant através de impersonação
/// </summary>
public class BackofficeImpersonationRequirement : IAuthorizationRequirement
{
}
