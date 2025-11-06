using Microsoft.Extensions.Logging;
using SGR.Api.Models.DTOs;
using SGR.Api.Services.Interfaces;

namespace SGR.Api.Controllers.Backoffice;

/// <summary>
/// Controller para gerenciamento de perfis
/// </summary>
public class PerfisController : BaseController<IPerfilService, PerfilDto, CreatePerfilRequest, UpdatePerfilRequest>
{
    public PerfisController(IPerfilService service, ILogger<PerfisController> logger) : base(service, logger)
    {
    }
}
