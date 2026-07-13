using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vuetrack.Api.Features.Backends.Timetracking.Contracts;
using Vuetrack.Api.Features.Backends.Timetracking.Services;
using Vuetrack.Api.Infrastructure.Authentication;

namespace Vuetrack.Api.Features.Backends.Timetracking;

[ApiController]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/backends/timetracking")]
[Authorize]
public class TimetrackingConnectionController(ITimetrackingConnectionService connectionService) : ControllerBase
{
    private ITimetrackingConnectionService ConnectionService { get; } = connectionService;

    [HttpGet("authorize")]
    public IActionResult Authorize([FromQuery] string redirectUri)
    {
        if (!Uri.TryCreate(redirectUri, UriKind.Absolute, out _))
        {
            return BadRequest(new { errors = new[] { "redirectUri must be a valid absolute URI." } });
        }

        return Ok(ConnectionService.BuildAuthorization(redirectUri));
    }

    [HttpGet("status")]
    public async Task<IActionResult> Status()
    {
        var userId = User.GetUserId();

        return Ok(await ConnectionService.GetStatusAsync(userId));
    }

    [HttpPost("callback")]
    public async Task<IActionResult> Callback([FromBody] TimetrackingConnectCreateContract request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var result = await ConnectionService.ConnectAsync(userId, request, cancellationToken);

        return result.ToActionResult();
    }

    [HttpDelete]
    public async Task<IActionResult> Disconnect()
    {
        var userId = User.GetUserId();

        await ConnectionService.DisconnectAsync(userId);

        return NoContent();
    }
}
