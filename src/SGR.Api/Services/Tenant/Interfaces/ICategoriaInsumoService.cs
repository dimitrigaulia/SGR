using SGR.Api.Models.Tenant.DTOs;
using SGR.Api.Models.Tenant.Entities;
using SGR.Api.Services.Interfaces;

namespace SGR.Api.Services.Tenant.Interfaces;

public interface ICategoriaInsumoService : IBaseService<CategoriaInsumo, CategoriaInsumoDto, CreateCategoriaInsumoRequest, UpdateCategoriaInsumoRequest>, 
    IBaseServiceController<CategoriaInsumoDto, CreateCategoriaInsumoRequest, UpdateCategoriaInsumoRequest>
{
}

