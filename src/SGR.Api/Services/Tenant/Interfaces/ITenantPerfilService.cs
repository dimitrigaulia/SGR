using SGR.Api.Models.Tenant.DTOs;
using SGR.Api.Models.Tenant.Entities;
using SGR.Api.Services.Interfaces;

namespace SGR.Api.Services.Tenant.Interfaces;

public interface ITenantPerfilService : IBaseService<TenantPerfil, TenantPerfilDto, CreateTenantPerfilRequest, UpdateTenantPerfilRequest>, 
    IBaseServiceController<TenantPerfilDto, CreateTenantPerfilRequest, UpdateTenantPerfilRequest>
{
}

