using Microsoft.AspNetCore.Mvc;
using SGR.Api.Controllers.Tenant;
using SGR.Api.Models.Tenant.DTOs;
using SGR.Api.Services.Tenant.Interfaces;

namespace SGR.Api.Controllers.Tenant;

[ApiController]
[Route("api/tenant/categorias-receita")]
public class CategoriasReceitaController : BaseController<ICategoriaReceitaService, CategoriaReceitaDto, CreateCategoriaReceitaRequest, UpdateCategoriaReceitaRequest>
{
    public CategoriasReceitaController(ICategoriaReceitaService service, ILogger<CategoriasReceitaController> logger) 
        : base(service, logger)
    {
    }
}

