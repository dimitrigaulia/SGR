using SGR.Api.Models.Backoffice.DTOs;
using SGR.Api.Models.Backoffice.Entities;
using SGR.Api.Services.Interfaces;

namespace SGR.Api.Services.Backoffice.Interfaces;

public interface IBackofficePerfilService : IBaseService<BackofficePerfil, BackofficePerfilDto, CreateBackofficePerfilRequest, UpdateBackofficePerfilRequest>, 
    IBaseServiceController<BackofficePerfilDto, CreateBackofficePerfilRequest, UpdateBackofficePerfilRequest>
{
}

