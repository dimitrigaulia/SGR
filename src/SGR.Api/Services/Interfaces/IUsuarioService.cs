using SGR.Api.Models.DTOs;
using SGR.Api.Models.Entities;

namespace SGR.Api.Services.Interfaces;

public interface IUsuarioService : IBaseService<Usuario, UsuarioDto, CreateUsuarioRequest, UpdateUsuarioRequest>,
    IBaseServiceController<UsuarioDto, CreateUsuarioRequest, UpdateUsuarioRequest>
{
    Task<bool> EmailExistsAsync(string email, long? excludeId = null);
}
