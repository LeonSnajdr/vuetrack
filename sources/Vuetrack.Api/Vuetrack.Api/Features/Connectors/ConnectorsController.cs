using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vuetrack.Api.Features.Connectors.Services;
using Vuetrack.Api.Infrastructure.Authentication;

namespace Vuetrack.Api.Features.Connectors;

[ApiController]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize(Roles = "User")]
public class ConnectorsController(IConnectorService connectorService) : ControllerBase
{
    private IConnectorService ConnectorService { get; } = connectorService;

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var connectors = await ConnectorService.GetConnectorsAsync(userId, cancellationToken);

        return Ok(connectors);
    }
}
