using Microsoft.Extensions.Logging;
using SGR.Api.Models.Tenant.DTOs;
using SGR.Api.Services.Tenant.Interfaces;

namespace SGR.Api.Controllers.Tenant;

/// <summary>
/// Controller para gerenciamento de perfis do tenant
/// </summary>
public class PerfisController : BaseController<ITenantPerfilService, TenantPerfilDto, CreateTenantPerfilRequest, UpdateTenantPerfilRequest>
{
    public PerfisController(ITenantPerfilService service, ILogger<PerfisController> logger) : base(service, logger)
    {
    }
}

