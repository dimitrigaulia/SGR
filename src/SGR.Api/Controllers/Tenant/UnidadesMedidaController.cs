using Microsoft.AspNetCore.Mvc;
using SGR.Api.Controllers.Tenant;
using SGR.Api.Models.Tenant.DTOs;
using SGR.Api.Services.Tenant.Interfaces;

namespace SGR.Api.Controllers.Tenant;

[ApiController]
[Route("api/tenant/unidades-medida")]
public class UnidadesMedidaController : BaseController<IUnidadeMedidaService, UnidadeMedidaDto, CreateUnidadeMedidaRequest, UpdateUnidadeMedidaRequest>
{
    public UnidadesMedidaController(IUnidadeMedidaService service, ILogger<UnidadesMedidaController> logger) 
        : base(service, logger)
    {
    }
}

