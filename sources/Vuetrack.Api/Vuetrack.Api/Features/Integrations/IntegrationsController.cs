using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vuetrack.Api.Features.Integrations;
using Vuetrack.Api.Features.Integrations.Connections;
using Vuetrack.Api.Features.Integrations.Contracts;
using Vuetrack.Api.Infrastructure.Authentication;
using Vuetrack.Api.Infrastructure.Validation;

namespace Vuetrack.Api.Features.Integrations;

[ApiController]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/integrations")]
[Authorize(Roles = "User")]
public class IntegrationsController(IIntegrationService integrationService) : ControllerBase
{
    private IIntegrationService IntegrationService { get; } = integrationService;

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var integrations = await IntegrationService.ListAsync(userId, cancellationToken);

        return Ok(integrations);
    }

    [HttpGet("{key}/authorize")]
    public IActionResult Authorize(IntegrationKey key, [FromQuery] string redirectUri)
    {
        if (!Uri.TryCreate(redirectUri, UriKind.Absolute, out _))
        {
            ModelState.AddModelError(nameof(redirectUri), "redirectUri must be a valid absolute URI.");
            return ValidationProblem();
        }

        var connectionService = IntegrationService.ResolveConnectionService(key);
        if (connectionService is null)
        {
            return NotFound();
        }

        return Ok(connectionService.BuildAuthorization(redirectUri));
    }

    [HttpGet("{key}/status")]
    public async Task<IActionResult> Status(IntegrationKey key, CancellationToken cancellationToken)
    {
        var connectionService = IntegrationService.ResolveConnectionService(key);
        if (connectionService is null)
        {
            return NotFound();
        }

        var userId = User.GetUserId();

        var status = await connectionService.GetStatusAsync(userId, cancellationToken);

        return Ok(status);
    }

    [HttpPost("{key}/callback")]
    public async Task<IActionResult> Callback(IntegrationKey key, [FromBody] OAuthConnectCreateContract request, CancellationToken cancellationToken)
    {
        var connectionService = IntegrationService.ResolveConnectionService(key);
        if (connectionService is null)
        {
            return NotFound();
        }

        var userId = User.GetUserId();

        var result = await connectionService.ConnectAsync(userId, request, cancellationToken);

        return this.ToActionResult(result);
    }

    [HttpDelete("{key}")]
    public async Task<IActionResult> Disconnect(IntegrationKey key, CancellationToken cancellationToken)
    {
        var connectionService = IntegrationService.ResolveConnectionService(key);
        if (connectionService is null)
        {
            return NotFound();
        }

        var userId = User.GetUserId();

        await connectionService.DisconnectAsync(userId, cancellationToken);

        return NoContent();
    }
}
