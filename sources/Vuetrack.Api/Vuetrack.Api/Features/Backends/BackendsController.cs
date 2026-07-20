using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vuetrack.Api.Features.Backends.Services;
using Vuetrack.Api.Infrastructure.Authentication;

namespace Vuetrack.Api.Features.Backends;

[ApiController]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize(Roles = "User")]
public class BackendsController(IBackendService backendService) : ControllerBase
{
    private IBackendService BackendService { get; } = backendService;

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var backends = await BackendService.GetBackendsAsync(userId, cancellationToken);

        return Ok(backends);
    }
}
