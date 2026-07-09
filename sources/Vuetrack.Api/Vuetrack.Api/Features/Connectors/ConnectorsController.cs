using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vuetrack.Connectors.Abstractions;

namespace Vuetrack.Api.Features.Connectors;

[ApiController]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class ConnectorsController(IConnectorRegistry registry) : ControllerBase
{
    private IConnectorRegistry Registry { get; } = registry;

    [HttpGet]
    public IActionResult Get()
    {
        return Ok(Registry.Descriptors);
    }
}
