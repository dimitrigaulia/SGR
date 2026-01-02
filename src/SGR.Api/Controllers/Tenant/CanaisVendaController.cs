using Microsoft.AspNetCore.Mvc;
using SGR.Api.Controllers.Tenant;
using SGR.Api.Models.Tenant.DTOs;
using SGR.Api.Services.Tenant.Interfaces;

namespace SGR.Api.Controllers.Tenant;

[ApiController]
[Route("api/tenant/canais-venda")]
public class CanaisVendaController : BaseController<ICanalVendaService, CanalVendaDto, CreateCanalVendaRequest, UpdateCanalVendaRequest>
{
    public CanaisVendaController(ICanalVendaService service, ILogger<CanaisVendaController> logger) 
        : base(service, logger)
    {
    }
}
