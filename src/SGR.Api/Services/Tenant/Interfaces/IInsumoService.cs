using SGR.Api.Models.Tenant.DTOs;
using SGR.Api.Models.Tenant.Entities;
using SGR.Api.Services.Interfaces;

namespace SGR.Api.Services.Tenant.Interfaces;

public interface IInsumoService : IBaseService<Insumo, InsumoDto, CreateInsumoRequest, UpdateInsumoRequest>,
    IBaseServiceController<InsumoDto, CreateInsumoRequest, UpdateInsumoRequest>
{
}

