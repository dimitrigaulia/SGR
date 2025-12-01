using Microsoft.AspNetCore.Mvc;
using SGR.Api.Controllers.Tenant;
using SGR.Api.Models.Tenant.DTOs;
using SGR.Api.Services.Tenant.Interfaces;

namespace SGR.Api.Controllers.Tenant;

[ApiController]
[Route("api/tenant/insumos")]
public class InsumosController : BaseController<IInsumoService, InsumoDto, CreateInsumoRequest, UpdateInsumoRequest>
{
    public InsumosController(IInsumoService service, ILogger<InsumosController> logger)
        : base(service, logger)
    {
    }
}

