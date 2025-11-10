using SGR.Api.Models.Tenant.DTOs;
using SGR.Api.Models.Tenant.Entities;
using SGR.Api.Services.Interfaces;

namespace SGR.Api.Services.Tenant.Interfaces;

public interface IUnidadeMedidaService : IBaseService<UnidadeMedida, UnidadeMedidaDto, CreateUnidadeMedidaRequest, UpdateUnidadeMedidaRequest>, 
    IBaseServiceController<UnidadeMedidaDto, CreateUnidadeMedidaRequest, UpdateUnidadeMedidaRequest>
{
}

