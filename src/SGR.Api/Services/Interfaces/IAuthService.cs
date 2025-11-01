using SGR.Api.Models.DTOs;

namespace SGR.Api.Services.Interfaces;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request);
}

