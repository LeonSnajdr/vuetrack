using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vuetrack.Api.Features.Connectors.Jira.Contracts;
using Vuetrack.Api.Features.Connectors.Jira.Services;
using Vuetrack.Api.Infrastructure.Authentication;
using Vuetrack.Connectors.Abstractions;

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
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        return Ok(await ConnectionService.GetStatusAsync(userId));
    }

    [HttpPost("callback")]
    public async Task<IActionResult> Callback([FromBody] JiraConnectCreateContract request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

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
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        await ConnectionService.DisconnectAsync(userId);

        return NoContent();
    }

    /// <summary>
    /// Testing-only: fetches the caller's Jira activity ("recommendations") end-to-end to verify the OAuth connection works.
    /// </summary>
    [HttpGet("recommendations")]
    public async Task<IActionResult> Recommendations([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var toDate = to ?? DateTime.UtcNow;
        var fromDate = from ?? toDate.AddDays(-7);

        var result = await ConnectionService.FetchRecommendationsAsync(userId, fromDate, toDate, cancellationToken);

        return result switch
        {
            ActivityFetchNotConnected => Conflict(new { errors = new[] { "Jira is not connected." } }),
            ActivityFetchSuccess success => Ok(success.Signals),
            ActivityFetchAuthFailed authFailed => Unauthorized(new { errors = new[] { authFailed.Reason } }),
            ActivityFetchRateLimited rateLimited => StatusCode(429, new { retryAfterSeconds = rateLimited.RetryAfter.TotalSeconds }),
            ActivityFetchConnectorError error => StatusCode(502, new { errors = new[] { error.Message } }),
            _ => BadRequest(),
        };
    }
}
