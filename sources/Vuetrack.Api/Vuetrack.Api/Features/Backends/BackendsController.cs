using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vuetrack.Backends.Abstractions;

namespace Vuetrack.Api.Features.Backends;

[ApiController]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize(Roles = "User")]
public class BackendsController(IBackendRegistry registry) : ControllerBase
{
    private IBackendRegistry Registry { get; } = registry;

    [HttpGet]
    public IActionResult Get()
    {
        return Ok(Registry.Descriptors);
    }
}
