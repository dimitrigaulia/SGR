using SGR.Api.Models.DTOs;
using SGR.Api.Models.Entities;

namespace SGR.Api.Services.Interfaces;

public interface IPerfilService : IBaseService<Perfil, PerfilDto, CreatePerfilRequest, UpdatePerfilRequest>, 
    IBaseServiceController<PerfilDto, CreatePerfilRequest, UpdatePerfilRequest>
{
}
