using Microsoft.Extensions.Logging;
using SGR.Api.Models.Backoffice.DTOs;
using SGR.Api.Services.Backoffice.Interfaces;

namespace SGR.Api.Controllers.Backoffice;

/// <summary>
/// Controller para gerenciamento de perfis do backoffice
/// </summary>
public class PerfisController : BaseController<IBackofficePerfilService, BackofficePerfilDto, CreateBackofficePerfilRequest, UpdateBackofficePerfilRequest>
{
    public PerfisController(IBackofficePerfilService service, ILogger<PerfisController> logger) : base(service, logger)
    {
    }
}
