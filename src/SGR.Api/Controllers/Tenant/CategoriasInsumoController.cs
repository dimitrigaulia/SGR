using Microsoft.AspNetCore.Mvc;
using SGR.Api.Controllers.Tenant;
using SGR.Api.Models.Tenant.DTOs;
using SGR.Api.Services.Tenant.Interfaces;

namespace SGR.Api.Controllers.Tenant;

[ApiController]
[Route("api/tenant/categorias-insumo")]
public class CategoriasInsumoController : BaseController<ICategoriaInsumoService, CategoriaInsumoDto, CreateCategoriaInsumoRequest, UpdateCategoriaInsumoRequest>
{
    public CategoriasInsumoController(ICategoriaInsumoService service, ILogger<CategoriasInsumoController> logger) 
        : base(service, logger)
    {
    }
}

