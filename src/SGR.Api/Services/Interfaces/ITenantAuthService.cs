using SGR.Api.Models.DTOs;

namespace SGR.Api.Services.Interfaces;

/// <summary>
/// Service para autenticação de tenants
/// </summary>
public interface ITenantAuthService
{
    /// <summary>
    /// Realiza login do usuário do tenant
    /// </summary>
    Task<LoginResponse?> LoginAsync(LoginRequest request);
}

