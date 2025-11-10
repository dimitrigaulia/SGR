using SGR.Api.Models.Backoffice.DTOs;
using SGR.Api.Models.Backoffice.Entities;
using SGR.Api.Services.Interfaces;

namespace SGR.Api.Services.Backoffice.Interfaces;

public interface IBackofficeUsuarioService : IBaseService<BackofficeUsuario, BackofficeUsuarioDto, CreateBackofficeUsuarioRequest, UpdateBackofficeUsuarioRequest>,
    IBaseServiceController<BackofficeUsuarioDto, CreateBackofficeUsuarioRequest, UpdateBackofficeUsuarioRequest>
{
    Task<bool> EmailExistsAsync(string email, long? excludeId = null);
}

