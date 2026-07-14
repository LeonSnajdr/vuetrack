using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vuetrack.Api.Features.Connectors.Jira.Contracts;
using Vuetrack.Api.Features.Connectors.Jira.Services;
using Vuetrack.Api.Infrastructure.Authentication;
using Vuetrack.Api.Infrastructure.Validation;

namespace Vuetrack.Api.Features.Connectors.Jira;

[ApiController]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/connectors/jira")]
[Authorize(Roles = "User")]
public class JiraConnectionController(IJiraConnectionService connectionService) : ControllerBase
{
    private IJiraConnectionService ConnectionService { get; } = connectionService;

    [HttpGet("authorize")]
    public IActionResult Authorize([FromQuery] string redirectUri)
    {
        if (!Uri.TryCreate(redirectUri, UriKind.Absolute, out _))
        {
            ModelState.AddModelError(nameof(redirectUri), "redirectUri must be a valid absolute URI.");
            return ValidationProblem();
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
    public async Task<IActionResult> Callback([FromBody] JiraConnectCreateContract request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var result = await ConnectionService.ConnectAsync(userId, request, cancellationToken);

        return this.ToActionResult(result);
    }

    [HttpDelete]
    public async Task<IActionResult> Disconnect()
    {
        var userId = User.GetUserId();

        await ConnectionService.DisconnectAsync(userId);

        return NoContent();
    }
}
