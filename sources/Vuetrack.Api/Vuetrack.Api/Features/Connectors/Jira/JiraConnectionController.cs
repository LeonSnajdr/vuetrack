using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vuetrack.Api.Features.Connectors.Jira.Contracts;
using Vuetrack.Api.Features.Connectors.Jira.Services;
using Vuetrack.Api.Infrastructure.Authentication;

namespace Vuetrack.Api.Features.Connectors.Jira;

[ApiController]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/connectors/jira")]
[Authorize]
public class JiraConnectionController(IJiraConnectionService connectionService) : ControllerBase
{
    private IJiraConnectionService ConnectionService { get; } = connectionService;

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
    public async Task<IActionResult> Callback([FromBody] JiraConnectCreateContract request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var result = await ConnectionService.ConnectAsync(userId, request, cancellationToken);

        return result switch
        {
            JiraConnectSuccess success => Ok(new JiraConnectContract(success.SiteUrl)),
            JiraConnectNoSite => Conflict(new { errors = new[] { "No accessible Jira site for this account." } }),
            JiraConnectValidationFailed invalid => BadRequest(new { errors = invalid.Errors }),
            JiraConnectAuthFailed authFailed => Unauthorized(new { errors = new[] { authFailed.Reason } }),
            JiraConnectRateLimited rateLimited => StatusCode(429, new { retryAfterSeconds = rateLimited.RetryAfter.TotalSeconds }),
            JiraConnectError error => StatusCode(502, new { errors = new[] { error.Message } }),
            _ => StatusCode(500),
        };
    }

    [HttpDelete]
    public async Task<IActionResult> Disconnect()
    {
        var userId = User.GetUserId();

        await ConnectionService.DisconnectAsync(userId);

        return NoContent();
    }
}
