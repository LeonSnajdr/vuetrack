using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vuetrack.Api.Features.Integrations;
using Vuetrack.Api.Features.Integrations.Abstractions;
using Vuetrack.Api.Infrastructure.Authentication;
using Vuetrack.Api.Infrastructure.Validation;

namespace Vuetrack.Api.Features.Project;

[ApiController]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/project")]
[Authorize(Roles = "User")]
public class ProjectController(IBackend store) : ControllerBase
{
    private IBackend Store { get; } = store;

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var result = await Store.GetProjectsAsync(userId, cancellationToken);

        return this.ToActionResult(result);
    }

    [HttpGet("{projectId}/activity")]
    public async Task<IActionResult> Activities(string projectId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var result = await Store.GetActivitiesAsync(userId, projectId, cancellationToken);

        return this.ToActionResult(result);
    }

    [HttpGet("findByTaskId")]
    public async Task<IActionResult> FindByTaskId([FromQuery] string taskId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var result = await Store.FindProjectIdByTaskIdAsync(userId, taskId, cancellationToken);

        return this.ToActionResult(result);
    }
}
